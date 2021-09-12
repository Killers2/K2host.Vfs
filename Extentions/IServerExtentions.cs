/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.VisualBasic;

using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;

using K2host.Core;
using K2host.Vfs.Enums;
using K2host.Vfs.Classes;
using K2host.Vfs.Interface;

using gl = K2host.Core.OHelpers;

namespace K2host.Vfs.Extentions
{

    public static class IServerExtentions
    {

        public const string VDiskFileExtention = ".vddf";
        public const string HDiskFileExtention = ".hddf";

        #region Server Header and Disk Mounting

        /// <summary>
        /// Creates a new V-Disk with the server.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <param name="rootAlias"></param>
        /// <param name="requirements"></param>
        /// <param name="systemkey"></param>
        /// <param name="certificatePath"></param>
        /// <param name="certificatePassword"></param>
        /// <returns></returns>
        public static IServer CreateNew(this IServer e, string path, string rootAlias, IUserRequirements requirements, string systemkey, string certificatePath = "", string certificatePassword = "")
        {
            try
            {
                e.OnCreateNewStatusEvent?.Invoke(e, "Creating file system header, please wait..");

                MemoryStream m = new OHeader(requirements, systemkey, rootAlias, certificatePath, certificatePassword)
                { 
                    Version = e.Version 
                }
                .ToBytes()
                .ToMemoryStream();

                byte[] b = gl.CompressData(m).ToArray();

                e.OnCreateNewStatusEvent?.Invoke(e, "Writing file system header, please wait..");

                e.VDiskFileStream = new(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
                e.VDiskFileStream?.Seek(0, SeekOrigin.Begin);
                e.VDiskFileStream?.Write(b);
                e.VDiskFileStream?.Write(BitConverter.GetBytes(b.Length));
                e.VDiskFileStream?.Close();
                e.VDiskFileStream?.Dispose();

                m?.Close();
                m?.Dispose();

                e.OnCreateNewCompleteEvent?.Invoke(e);

                return e;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        /// <summary>
        /// Mounts and opens a vdisk, loads the v-disk file steam.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source">The VDisk File</param>
        /// <returns></returns>
        public static IServer Mount(this IServer e, string source)
        {
            try
            {
                e.VDisk = source;

                if (Path.GetExtension(e.VDisk) != VDiskFileExtention)
                    throw new Exception("incorrect file format used.");

                e.VDiskFileStream = new(e.VDisk, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                byte[] a = new byte[4]; //int is 4 bytes in length (32bit / 64bit)

                e.VDiskFileStream.Position = (int)e.VDiskFileStream.Length - a.Length;

                int readlength = e.VDiskFileStream.Read(a, 0, a.Length);
              
                try {
                    a = new byte[BitConverter.ToInt32(a)]; // resize the buffer to the header length
                } catch { }

                int tempFileLength = ((int)e.VDiskFileStream.Length - readlength) - a.Length;

                //if this potiner is less than the length of the disk then this is an error with the header
                if (tempFileLength >= 0)
                { 
                    e.VDiskFileStream.Position = tempFileLength;
                    _ = e.VDiskFileStream.Read(a, 0, a.Length);
                }

                //Decompress the header stream and deserialize json
                e.Header = gl.DecompressData(a.ToMemoryStream()).GetHeader();

                string headerFilePath = e.VDisk.Replace(VDiskFileExtention, HDiskFileExtention);

                //If there is no header assume a failed dismount on the last open.
                if (e.Header == null && File.Exists(headerFilePath))
                    e.Header = gl.DecompressData(File.ReadAllBytes(headerFilePath).ToMemoryStream()).GetHeader();

                //If this fails then throw error
                if(e.Header == null)
                    throw new Exception("incorrect file format used.");

                e.SystemKey = e.Header.SystemKey;

                //We only do this if the header was not a file, remove / truncate the disk and remove the header from the data, (partition)
                if (!File.Exists(headerFilePath))
                {
                    e.VDiskFileStream.SetLength(tempFileLength);
                    e.VDiskFileStream.Flush();
                }

                //If there is a certificate then create the cert file from the header data.
                if (!string.IsNullOrEmpty(e.Header.Certificate))
                {
                   
                    e.Certificate = new(
                        Convert.FromBase64String(e.Header.Certificate),
                        gl.DecryptAes(
                            e.Header.CertificatePassword,
                            e.SystemKey,
                            Encoding.UTF8.GetBytes(e.SystemKey)
                        )
                    );

                    e.CertificatePublicKey  = e.Certificate.GetRSAPublicKey();
                    e.CertificatePrivateKey = e.Certificate.GetRSAPrivateKey();

                }

                //Reset the position of the pointer
                e.VDiskFileStream.Position = 0;

                if (e.Header.Version != e.Version)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The version of this device ( " + e.Header.Version + " ) dosen't match the version of this engine ( " + e.Version + " )."));
                    e.Header.Dispose();
                    e.Dismount();
                    return e;
                }

                if (!File.Exists(headerFilePath))
                {

                    FileStream headerFile = new(headerFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    headerFile.Write(a);
                    headerFile.SetLength(a.Length);
                    headerFile.Flush();
                    headerFile.Close();
                    headerFile.Dispose();

                    File.SetAttributes(headerFilePath, File.GetAttributes(headerFilePath) | FileAttributes.Hidden);

                }

                a.Dispose();

                e.IsMounted = true;

                e.OnMountedEvent?.Invoke(e);

                return e;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        /// <summary>
        /// This dismounts the v-disk and writes the header to the v-disk
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IServer Dismount(this IServer e)
        {
            try
            {

                e.HeaderChange = 0;

                e.ApplyChanges();
                
                byte[] b = File.ReadAllBytes(e.VDisk.Replace(VDiskFileExtention, HDiskFileExtention));

                File.Delete(e.VDisk.Replace(VDiskFileExtention, HDiskFileExtention));

                e.VDiskFileStream.Position = e.VDiskFileStream.Length;
                e.VDiskFileStream?.Seek(0, SeekOrigin.End);

                e.VDiskFileStream?.Write(b);
                e.VDiskFileStream?.Write(BitConverter.GetBytes(b.Length)); // header length at the end of the stream.
                e.VDiskFileStream?.Flush();
                e.VDiskFileStream?.Close();
                e.VDiskFileStream?.Dispose();

                e.IsMounted = false;

                e.OnDismountedEvent?.Invoke(e);

                return e;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        /// <summary>
        /// Writes any changes to the temp header file while the server has a mounted device
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IServer ApplyChanges(this IServer e)
        {
            try
            {

                byte[] b = gl.CompressData(e.Header.ToBytes().ToMemoryStream()).ToArray();

                FileStream hf = new(e.VDisk.Replace(VDiskFileExtention, HDiskFileExtention), FileMode.Truncate, FileAccess.ReadWrite, FileShare.None);
                hf?.Write(b, 0, b.Length);
                hf?.Flush();
                hf?.Close();
                hf?.Dispose();

                b.Dispose();

                e.OnDeviceUpdateEvent?.Invoke(e);

                return e;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        /// <summary>
        /// Backs up your v-disk file currently mounted
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IServer Backup(this IServer e)
        {
            try
            {
                e.OnBackupStatusEvent?.Invoke(e, "Dismounting V-Disk..");

                e.Dismount();

                e.OnBackupStatusEvent?.Invoke(e, "Backup started..");

                string backupPath   = e.VDisk.Remove(e.VDisk.LastIndexOf(@"\")) + @"\V-Disk Backup";

                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                string name = e.VDisk.Remove(0, e.VDisk.LastIndexOf(@"\") + 1);

                e.VDiskFileStream = new(e.VDisk, FileMode.Open, FileAccess.Read, FileShare.Read);

                FileStream zipFile = new(backupPath + "\\" + gl.UniqueIdent() + ".zip", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

                IWriter writer = WriterFactory.Open(zipFile, ArchiveType.Zip, new WriterOptions(CompressionType.BZip2) { LeaveStreamOpen = false });
                
                writer.Write(name, e.VDiskFileStream);
               
                writer.Dispose();

                e.VDiskFileStream?.Close();
                e.VDiskFileStream?.Dispose();

                zipFile?.Close();
                zipFile?.Dispose();

                e.OnBackupStatusEvent?.Invoke(e, "Remounting Please wait..");

                e.Mount(e.VDisk);

                e.OnBackupCompleteEvent?.Invoke(e);

                return e;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        /// <summary>
        /// Restores a zip back up of the v-disk file.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IServer Restore(this IServer e, string source)
        {
            try
            {

                e.OnRestoreStatusEvent?.Invoke(e, "Checking Please wait..");

                if (!File.Exists(source))
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("There are no back up files to restore!"));
                    return e;
                }

                e.Dismount();

                e.OnRestoreStatusEvent?.Invoke(e, "Restoring Please wait..");

                FileStream  vdisk   = new(e.VDisk, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream  zipFile = new(source, FileMode.Open, FileAccess.Read, FileShare.None);
                IReader     reader  = ReaderFactory.Open(zipFile);
                while (reader.MoveToNextEntry())
                    if (!reader.Entry.IsDirectory)
                        reader.WriteEntryTo(vdisk);

                reader.Dispose();
                vdisk.Close();
                vdisk.Dispose();

                e.OnRestoreStatusEvent?.Invoke(e, "Remounting Please wait..");

                e.Mount(e.VDisk);

                e.OnRestoreCompleteEvent?.Invoke(e);

                return e;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        /// <summary>
        /// Returns a true value if the system needs to cancel an operation.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool CheckInterrupts(this IServer e)
        {
            
            if (e.Cancel)
            {
                e.Cancel = false;
                return true;
            }

            return false;

        }

        #endregion

        #region Files and Directories Tools

        /// <summary>
        /// Changes the paths in all files based on directory path change.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="rootpath"></param>
        /// <param name="source"></param>
        public static void ChangePathsRecursively(this IServer e, string rootpath, IFolder source)
        {

            source.FullPath = rootpath + @"\" + source.Name;

            source.Path = source.FullPath.Remove(source.FullPath.LastIndexOf(@"\"));

            source.Directories.ForEach(di => {
                e.ChangePathsRecursively(source.FullPath, di);
            });

            source.Files.ForEach(fi => {
                fi.FullPath = (source.FullPath + @"\" + fi.Name);
            });

        }

        /// <summary>
        /// Validate the directory name.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public static bool DirectoryNameValidation(this IServer e, string name)
        {

            List<char> inValid = new()
            {
                (char)92,
                (char)47,
                (char)58,
                (char)42,
                (char)63,
                (char)34,
                (char)60,
                (char)62,
                (char)46,
                (char)124
            };

            if (name.ToCharArray().Any(x => inValid.Any(y => y == x)))
            {
                e.OnErrorEvent?.Invoke(e, new Exception(@"The name cannot contain any of these chars  \ / : * ? " + ((char)34).ToString() + " < > | ."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate the file name.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public static bool FileNameValidation(this IServer e, string name)
        {

            List<char> inValid = new()
            {
                (char)92,
                (char)47,
                (char)58,
                (char)42,
                (char)63,
                (char)34,
                (char)60,
                (char)62,
                (char)124
            };

            if (name.ToCharArray().Any(x => inValid.Any(y => y == x)))
            {
                e.OnErrorEvent?.Invoke(e, new Exception(@"The name cannot contain any of these chars  \ / : * ? " + ((char)34).ToString() + " < > |"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks a string source path for char46 (.) 
        /// </summary>
        /// <param name="_"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsPathFile(this IServer _, string path)
        {
            return path.ToCharArray().Contains((char)46);
        }

        /// <summary>
        /// Recursivly sets the deleted flag from an <see cref="IFolder"/> object.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        /// <param name="del"></param>
        public static void SetDeletedFlag(this IServer e, IFolder source, bool delete)
        {
            try
            {

                source.Directories.ForEach(di => { 
                    e.SetDeletedFlag(di, delete); 
                });

                if (delete)
                {
                    source.Properties = source.Properties.SetFlags(ODiskFlags.DELETED);
                    e.Header.RecycleBin = e.Header.RecycleBin.Append(source.FullPath).ToArray();
                }
                else
                {
                    source.Properties = source.Properties.ClearFlags(ODiskFlags.DELETED);
                    e.Header.RecycleBin = e.Header.RecycleBin.Filter(s => s != source.FullPath);
                }

                source.Files.ForEach(fi => {
                    if (delete)
                    {
                        fi.Properties = fi.Properties.SetFlags(ODiskFlags.DELETED);
                        e.Header.RecycleBin = e.Header.RecycleBin.Append(fi.FullPath).ToArray();
                        e.OnDirectoryDeletingEvent?.Invoke(e, fi.FullPath);
                    }
                    else
                    {
                        fi.Properties = fi.Properties.ClearFlags(ODiskFlags.DELETED);
                        e.Header.RecycleBin = e.Header.RecycleBin.Filter(s => s != fi.FullPath);
                        e.OnDirectoryRestoringEvent?.Invoke(e, fi.FullPath);
                    }
                });

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
            }
        }

        /// <summary>
        /// This updates all folder with the current root path and admin user of the current system.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="di"></param>
        /// <param name="adminUser"></param>
        public static void MergeFolderUsersAndPathRecursively(this IServer e, IFolder di, IUserRequirements adminUser)
        {

            di.Directories.ForEach(dir => {
                e.MergeFolderUsersAndPathRecursively(dir, adminUser);
            });

            di.UserIndenties.Dispose(out _);
            di.UserIndenties = Array.Empty<long>();
            di.UserIndenties = di.UserIndenties.Append(adminUser.UserId).ToArray();

            if (di.FullPath.Contains(@"\"))
            {
                di.FullPath = di.FullPath.Remove(0, di.FullPath.IndexOf(@"\"));
                di.FullPath = e.Header.RootName + di.FullPath;
            }
            else
                di.FullPath = e.Header.RootName;

            if (di.Path.Contains(@"\"))
            {
                di.Path = di.Path.Remove(0, di.Path.IndexOf(@"\"));
                di.Path = e.Header.RootName + di.Path;
            }
            else
                di.Path = e.Header.RootName;

        }

        #endregion

        #region Directories

        /// <summary>
        /// Returns the <see cref="IContainer"/> object based on the path given. 
        /// The <see cref="IContainer"/> can be either a folder or disk header
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IContainer DirectoryGetDirectory(this IServer e, string path)
        {
            try
            {
                string[] temp = path.Fracture(@"\");

                if (temp[0] != e.Header.RootName)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The root (" + temp[0] + ") of this path dosen't match the root on this device."));
                    return default;
                }

                if (temp.Length == 1)
                    return e.Header;

                if (!e.Header.Directories.Where(d => d.Name == temp[1]).Any())
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The directory (" + temp[1] + ") dosen't exist."));
                    return default;
                }

                IFolder tf = e.Header.Directories.Where(d => d.Name == temp[1]).FirstOrDefault();

                if (temp.Length == 2)
                    return tf;

                for (var x = 2; x < temp.Length; x++) {
                    var t = tf.Directories.Where(d => d.Name == temp[x]).FirstOrDefault();
                    if (t == null)
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("This directory (" + temp[x] + ") dosen't exist!."));
                        return default;
                    }
                    tf = t;
                }

                return tf;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return default;
            }
        }

        /// <summary>
        /// Returns the <see cref="IFolder"/> list based on the path given. 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IFolder[] DirectoryGetDirectories(this IServer e, string path)
        {
            try
            {
                string[] temp = path.Fracture(@"\");

                if (temp[0] != e.Header.RootName)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The root (" + temp[0] + ") of this path dosen't match the root on this device."));
                    return default;
                }

                if (temp.Length == 1)
                    return e.Header.Directories;

                if (!e.Header.Directories.Where(d => d.Name == temp[1]).Any())
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The directory (" + temp[1] + ") dosen't exist."));
                    return default;
                }

                IFolder tf = e.Header.Directories.Where(d => d.Name == temp[1]).FirstOrDefault();

                if (temp.Length == 2)
                    return tf.Directories;

                for (var x = 2; x < temp.Length; x++)
                {
                    var t = tf.Directories.Where(d => d.Name == temp[x]).FirstOrDefault();
                    if (t == null)
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("This directory (" + temp[x] + ") dosen't exist!."));
                        return default;
                    }
                    tf = t;
                }

                return tf.Directories;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return default;
            }
        }

        /// <summary>
        /// Returns a <see cref="IFile"/> from the last directory in the path.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IFile DirectoryGetFile(this IServer e, string path)
        {
            try
            {

                string[] temp = path.Fracture(@"\");

                if (temp[0] != e.Header.RootName)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The root (" + temp[0] + ") of this path dosen't match the root on this device."));
                    return default;
                }

                if (temp.Length == 1)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("You must have a path greater then the inital root of this device."));
                    return default;
                }

                if (temp.Length == 2)
                    if (!e.Header.Files.Where(f => f.Name == temp[1]).Any())
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("The file (" + temp[1] + ")  dosen't exist!."));
                        return default;
                    }
                    else
                        return e.Header.Files.Where(f => f.Name == temp[1]).FirstOrDefault();


                IFolder tf = e.Header.Directories.Where(d => d.Name == temp[1]).FirstOrDefault();

                for (var x = 2; x < (temp.Length - 1); x++)
                {
                    var t = tf.Directories.Where(d => d.Name == temp[x]).FirstOrDefault();
                    if (t == null)
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("This directory (" + temp[x] + ") dosen't exist!."));
                        return default;
                    }
                    tf = t;
                }

                if (!tf.Files.Where(f => f.Name == temp[^1]).Any())
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The file (" + temp[^1] + ")  dosen't exist!."));
                    return default;
                }

                return tf.Files.Where(f => f.Name == temp[^1]).FirstOrDefault();

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return default;
            }
        }

        /// <summary>
        /// Returns a list of <see cref="IFile"/>'s from the last directory in the path
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IFile[] DirectoryGetFiles(this IServer e, string path)
        {
            try
            {
                string[] temp = path.Fracture(@"\");

                if (temp[0] != e.Header.RootName)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The root (" + temp[0] + ") of this path dosen't match the root on this device."));
                    return default;
                }

                if (temp.Length == 1)
                    return e.Header.Files;

                if (!e.Header.Directories.Where(d => d.Name == temp[1]).Any())
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The directory (" + temp[1] + ") dosen't exist."));
                    return default;
                }

                IFolder tf = e.Header.Directories.Where(d => d.Name == temp[1]).FirstOrDefault();

                if (temp.Length == 2)
                    return tf.Files;

                for (var x = 2; x < temp.Length; x++)
                {
                    var t = tf.Directories.Where(d => d.Name == temp[x]).FirstOrDefault();
                    if (t == null)
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("This directory (" + temp[x] + ") dosen't exist!."));
                        return default;
                    }
                    tf = t;
                }

                return tf.Files;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return default;
            }
        }
        
        /// <summary>
        /// Returns all files recursively from a given path
        /// </summary>
        /// <param name="e"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IFile[] DirectoryGetFilesRecursively(this IServer e, IContainer container)
        {

            List<IFile> output = new();

            output.AddRange(container.Files);

            container.Directories.ForEach(di => {
                output.AddRange(e.DirectoryGetFilesRecursively(di));
            });

            return output.ToArray();
        }

        /// <summary>
        /// Creates a new directory from the path given
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static IFolder DirectoryCreateDirectory(this IServer e, string path, IUserRequirements owner)
        {
            try
            {
                string newDirectory     = path.Remove(0, path.LastIndexOf(@"\") + 1);
                string pathToDirectory  = path.Remove(path.LastIndexOf(@"\"));

                if (!e.DirectoryNameValidation(newDirectory))
                    return default;

                IContainer dl = e.DirectoryGetDirectory(pathToDirectory);

                if (dl == null)
                    return default;

                if (dl.Directories.Where(d => d.Name == newDirectory).Any())
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("This directory (" + newDirectory + ") already exists!."));
                    return default;
                }

                IFolder di = new OFolder(path) { Properties = ODiskFlags.DIRECTORY | ODiskFlags.READWRITE };

                di.UserIndenties    = di.UserIndenties.Append(owner.UserId).ToArray();
                dl.Directories      = dl.Directories.Append(di).ToArray();

                e.ApplyChanges();

                e.OnDirectoryAddedEvent?.Invoke(e, path);

                return di;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return default;
            }
        }

        /// <summary>
        /// Renames a directory in the path given.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool DirectoryRename(this IServer e, string path, string name)
        {
            try
            {
                if (!e.DirectoryNameValidation(name))
                    return false;

                string directoryToRename    = path.Remove(0, path.LastIndexOf(@"\") + 1);
                string pathToDirectory      = path.Remove(path.LastIndexOf(@"\"));

                IFolder dl = (IFolder)e.DirectoryGetDirectory(pathToDirectory);

                if (dl == null)
                    return default;

                dl.Name             = name;
                dl.DateTimeModified = gl.DateTime2UnixTime(DateTime.Now);
                dl.FullPath         = dl.FullPath.Replace(@"\" + directoryToRename, @"\" + dl.Name);

                e.ApplyChanges();

                return true;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// Moves a directory or a file to another directory.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source">The directory or file path source</param>
        /// <param name="desination">The directory path desination</param>
        /// <returns></returns>
        public static bool DirectoryMove(this IServer e, string source, string desination)
        {
            try
            {
                IFolder desinationDirectory = (IFolder)e.DirectoryGetDirectory(desination);
                if (desinationDirectory == null)
                    return false;

                string sourcePath = source.Remove(0, source.LastIndexOf(@"\") + 1);

                if (e.IsPathFile(sourcePath))
                {
                    IFile sourceItem = e.DirectoryGetFile(source);
                    if (desinationDirectory.Files.Where(f => f.Name == sourceItem.Name).Any())
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("Sorry, " + sourcePath + " file already exsits."));
                        return false;
                    }
                    else
                    {
                        IFolder root = (IFolder)e.DirectoryGetDirectory(sourceItem.FullPath.Remove(sourceItem.FullPath.LastIndexOf(@"\")));
                        root.Files = root.Files.Filter(f => f != sourceItem);
                        sourceItem.FullPath = desinationDirectory.FullPath + @"\" + sourceItem.Name;
                        desinationDirectory.Files = desinationDirectory.Files.Append(sourceItem).ToArray();
                    }
                }
                else
                {
                    IFolder sourceItem = (IFolder)e.DirectoryGetDirectory(source);
                    if (desinationDirectory.Directories.Where(d => d.Name == sourceItem.Name).Any())
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("Sorry, " + sourcePath + " directory already exsits."));
                        return false;
                    }
                    else
                    {
                        IFolder root = (IFolder)e.DirectoryGetDirectory(sourceItem.FullPath.Remove(sourceItem.FullPath.LastIndexOf(@"\")));
                        root.Directories = root.Directories.Filter(d => d != sourceItem);
                        e.ChangePathsRecursively(desinationDirectory.FullPath, sourceItem);
                        desinationDirectory.Directories = desinationDirectory.Directories.Append(sourceItem).ToArray();
                    }
                }
                e.ApplyChanges();
                return true;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a directory with its contents.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path">The full directory path</param>
        /// <returns></returns>
        public static bool DirectoryDelete(this IServer e, string path)
        {
            try
            {

                string directoryToDelete    = path.Remove(0, path.LastIndexOf(@"\") + 1);
                string pathToDirectory      = path.Remove(path.LastIndexOf(@"\"));

                IFolder dl = (IFolder)e.DirectoryGetDirectory(pathToDirectory);

                if (dl == null)
                    return false;

                IFolder di = dl.Directories.Where(d => d.Name == directoryToDelete).FirstOrDefault();

                if (di == null)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("This directory (" + directoryToDelete + ") dosen't exist."));
                    return false;
                }

                try
                {

                    e.SetDeletedFlag(di, true);

                    e.OnDirectoryDeletedEvent?.Invoke(e, di.Name);

                    e.ApplyChanges();

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }

                return true;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// Restores a directory with its contents
        /// </summary>
        /// <param name="e"></param>
        /// <param name="di"></param>
        public static IServer DirectoryRestore(this IServer e, IFolder di)
        {
            try
            {
                e.SetDeletedFlag(di, false);

                e.ApplyChanges();

                e.OnDirectoryRestoredEvent?.Invoke(e, di.Name);
                return e;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        #endregion

        #region Files

        /// <summary>
        /// This adds a new file to the data partition and the path selected.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <param name="file"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static bool FileAdd(this IServer e, string path, FileStream file, IUserRequirements owner)
        {
            try
            {
                // there is a cert on this device we will encrypt and bypass the standard.
                if (e.Certificate != null)
                    return e.FileAddEncrypt(path, file, owner);

                string  fileName    = Path.GetFileName(file.Name);
                IFolder di          = (IFolder)e.DirectoryGetDirectory(path);

                //Check user allowed limit
                if (owner.SpaceLimit > -1)
                    if ((owner.SpaceUsed + file.Length) > owner.SpaceLimit)
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("You do not have enough free space left for (" + fileName + ")."));
                        return false;
                    }

                //Check to see if the file name already exists
                if (di.Files.Where(f => f.Name == fileName).Any())
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("Sorry, this file (" + fileName + ") already exists."));
                    return false;
                }

                //Set the user space used
                owner.SpaceUsed += file.Length;

                //Update the header
                e.ApplyChanges();

                //Lets create the new file object
                IFile ret = new OFile(path + @"\" + fileName) {
                    Length = file.Length
                };

                //Add the owner user id to the file access
                ret.UserIndenties = ret.UserIndenties.Append(owner.UserId).ToArray();

                //Add all users from the directory to the file access.
                di.UserIndenties.ForEach(u => {
                    if (!ret.UserIndenties.Contains(u))
                        ret.UserIndenties = ret.UserIndenties.Append(u).ToArray();
                });

                //No lets find the space on the data partition to store the data.
                if (e.Header.EmptyClusters.Any()) {
                    ICluster cluster = e.Header.EmptyClusters.Where(c => c.Length >= ret.Length).FirstOrDefault();
                    if (cluster != null) {
                        ret.ExDataIndex = cluster.StartIndex;
                        if (cluster.Length == ret.Length)
                            e.Header.EmptyClusters = e.Header.EmptyClusters.Filter(c => c != cluster);
                        else
                        {
                            cluster.StartIndex  += ret.Length;
                            cluster.Length      -= ret.Length;
                        }
                    }
                    else
                    {
                        ret.ExDataIndex = e.VDiskFileStream.Length;
                        e.VDiskFileStream.SetLength(e.VDiskFileStream.Length + ret.Length);
                        e.VDiskFileStream.Position = 0;
                        e.VDiskFileStream.Seek(0, SeekOrigin.Begin);
                    }
                }
                else
                {
                    ret.ExDataIndex = e.VDiskFileStream.Length;
                    e.VDiskFileStream.SetLength(e.VDiskFileStream.Length + ret.Length);
                    e.VDiskFileStream.Position = 0;
                    e.VDiskFileStream.Seek(0, SeekOrigin.Begin);
                }

                //Now lets import the data to the data partition.
                e.DataImport(file, ret);

                return true;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// This adds a new file to the data partition and the path selected encrypted.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="path"></param>
        /// <param name="file"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        private static bool FileAddEncrypt(this IServer e, string path, FileStream file, IUserRequirements owner)
        {
            try 
            {

                //Grab the details for the file.
                string  fileName    = Path.GetFileName(file.Name);
                IFolder di          = (IFolder)e.DirectoryGetDirectory(path);

                //Check user allowed limit
                if (owner.SpaceLimit > -1)
                    if ((owner.SpaceUsed + file.Length) > owner.SpaceLimit)
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("You do not have enough free space left for (" + fileName + ")."));
                        return false;
                    }

                //Set the user space used
                owner.SpaceUsed += file.Length;

                //Lets create the new file object
                IFile ret = new OFile(path + @"\" + fileName);

                //Add the owner user id to the file access
                ret.UserIndenties = ret.UserIndenties.Append(owner.UserId).ToArray();

                //Add all users from the directory to the file access.
                di.UserIndenties.ForEach(u => {
                    if (!ret.UserIndenties.Contains(u))
                        ret.UserIndenties = ret.UserIndenties.Append(u).ToArray();
                });

                //Lets setup the data partition, with encrypted data we will allways add to the end
                ret.ExDataIndex             = e.VDiskFileStream.Length;
                e.VDiskFileStream.Position  = e.VDiskFileStream.Length;

                //Setup the encryption system and create a transform.
                AesManaged aesManaged       = new();
                aesManaged.KeySize          = 256;
                aesManaged.BlockSize        = 128;
                aesManaged.Mode             = CipherMode.CBC;
           
                ICryptoTransform transform  = aesManaged.CreateEncryptor();

                byte[] keyEncrypted         = e.CertificatePublicKey.Encrypt(aesManaged.Key, RSAEncryptionPadding.OaepSHA1);
                byte[] LenK                 = BitConverter.GetBytes(keyEncrypted.Length);   // 4 bytes
                byte[] LenIV                = BitConverter.GetBytes(aesManaged.IV.Length);  // 4 bytes

                //Lets write the encryption file header first.
                byte[] section = gl.Combine(LenK, LenIV, keyEncrypted, aesManaged.IV);

                e.VDiskFileStream.SetLength(e.VDiskFileStream.Length + section.Length);
                e.VDiskFileStream.Write(section, 0, section.Length);

                // Now write the cipher using a CryptoStream for encrypting.
                using (CryptoStream outStreamEncrypted = new(e.VDiskFileStream, transform, CryptoStreamMode.Write, true))
                {

                    byte[]  buffer      = new byte[e.SpeedBuffer];
                    long    position    = 0;
                    int     length      = 0;

                    while (position < file.Length)
                    {
                        length = file.Read(buffer, 0, buffer.Length);
                        outStreamEncrypted.Write(buffer, 0, length);
                        e.OnFileAddingEvent?.Invoke(e, ret.FullPath, length);
                        position += length;
                        Thread.Sleep(1);
                    }

                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();

                }

                transform.Dispose();
                aesManaged.Dispose();

                //Set the file length after encrytion as the length is not the same as the un-encrypted file
                ret.Length = e.VDiskFileStream.Position - ret.ExDataIndex;
                
                //Reset the data partition.
                e.VDiskFileStream.Position = 0;
                e.VDiskFileStream.Seek(0, SeekOrigin.Begin);

                //Update the file object and the header.
                ret.MD5     = gl.EncryptMd5(file, true);
                di          = (IFolder)e.DirectoryGetDirectory(ret.FullPath.Remove(ret.FullPath.LastIndexOf(@"\")));
                di.Files    = di.Files.Append(ret).ToArray();

                e.ApplyChanges();
               
                e.OnFileAddedEvent?.Invoke(e, ret.FullPath);

                file.Close();
                file.Dispose();

                return true;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }

        }

        /// <summary>
        /// This is used by the FileAdd method that imports the file data to the data partition after amending the header.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="stream"></param>
        /// <param name="file"></param>
        public static void DataImport(this IServer e, Stream stream, IFile file)
        {
            try
            {
                byte[]  buffer      = new byte[e.SpeedBuffer];
                long    position    = 0;
                int     length      = 0;

                BinaryWriter writer = new(e.VDiskFileStream);

                e.VDiskFileStream.Position = file.ExDataIndex;

                if (e.CheckInterrupts())
                {
                    try 
                    {
                        stream.Close();
                        stream.Dispose(); 
                    } catch { }
                    return;
                }

                while (position < stream.Length)
                {
                    length = stream.Read(buffer, 0, buffer.Length);
                    e.OnFileAddingEvent?.Invoke(e, file.FullPath, length);
                    writer.Write(buffer, 0, length);
                    position += length;
                    Thread.Sleep(1);
                }

                writer.Flush();

                e.VDiskFileStream.Position = 0;
                
                file.MD5 = gl.EncryptMd5(stream, true);

                stream.Close();
                stream.Dispose();
                
                IContainer di   = e.DirectoryGetDirectory(file.FullPath.Remove(file.FullPath.LastIndexOf(@"\")));
                di.Files        = di.Files.Append(file).ToArray();

                e.ApplyChanges();
                e.OnFileAddedEvent?.Invoke(e, file.FullPath);

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
            }
        }

        /// <summary>
        /// This is used to import file data to the data partition encrypted.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="stream"></param>
        /// <param name="file"></param>
        public static void DataImportEncrypt(this IServer e, Stream stream, IFile file)
        {
            try
            {

                //Lets setup the data partition, with encrypted data we will allways add to the end
                file.ExDataIndex            = e.VDiskFileStream.Length;
                e.VDiskFileStream.Position  = file.ExDataIndex;

                //Setup the encryption system and create a transform.
                AesManaged aesManaged       = new();
                aesManaged.KeySize          = 256;
                aesManaged.BlockSize        = 128;
                aesManaged.Mode             = CipherMode.CBC;
           
                ICryptoTransform transform  = aesManaged.CreateEncryptor();

                byte[] keyEncrypted         = e.CertificatePublicKey.Encrypt(aesManaged.Key, RSAEncryptionPadding.OaepSHA1);
                byte[] LenK                 = BitConverter.GetBytes(keyEncrypted.Length);   // 4 bytes
                byte[] LenIV                = BitConverter.GetBytes(aesManaged.IV.Length);  // 4 bytes

                //Lets write the encryption file header first.
                byte[] section = gl.Combine(LenK, LenIV, keyEncrypted, aesManaged.IV);

                e.VDiskFileStream.SetLength(e.VDiskFileStream.Length + section.Length);
                e.VDiskFileStream.Write(section, 0, section.Length);

                // Now write the cipher using a CryptoStream for encrypting.
                using (CryptoStream outStreamEncrypted = new(e.VDiskFileStream, transform, CryptoStreamMode.Write, true))
                {
                    
                    byte[]  buffer      = new byte[e.SpeedBuffer];
                    long    position    = 0;
                    int     length      = 0;

                    while (position < stream.Length)
                    {
                        length = stream.Read(buffer, 0, buffer.Length);
                        outStreamEncrypted.Write(buffer, 0, length);
                        e.OnFileAddingEvent?.Invoke(e, file.FullPath, length);
                        position += length;
                        Thread.Sleep(1);
                    }

                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();

                }

                transform.Dispose();
                aesManaged.Dispose();

                //Set the file length after encrytion as the length is not the same as the un-encrypted file
                file.Length = e.VDiskFileStream.Position - file.ExDataIndex;

                //Reset the data partition.
                e.VDiskFileStream.Position = 0;
                e.VDiskFileStream.Seek(0, SeekOrigin.Begin);

                //Update the file object and the header.
                file.MD5    = gl.EncryptMd5(stream, true);
                var di      = e.DirectoryGetDirectory(file.FullPath.Remove(file.FullPath.LastIndexOf(@"\")));
                di.Files    = di.Files.Append(file).ToArray();

                e.ApplyChanges();

                e.OnFileAddedEvent?.Invoke(e, file.FullPath);

                stream.Close();
                stream.Dispose();

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
            }
        }

        /// <summary>
        /// This will rename a file in the structure
        /// </summary>
        /// <param name="e"></param>
        /// <param name="fullpath"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool FileRename(this IServer e, string fullpath, string name)
        {
            try
            {
                if (e.FileNameValidation(name))
                    return false;

                string  oldfilename     = fullpath.Remove(0, fullpath.LastIndexOf(@"\") + 1);
                string  directorypath   = fullpath.Remove(fullpath.LastIndexOf(@"\"));
                IFile   fi              = e.DirectoryGetFile(fullpath);

                if (fi == null)
                    return false;

                fi.DateTimeModified = gl.DateTime2UnixTime(DateTime.Now);
                fi.FullPath         = fi.FullPath.Replace(@"\" + oldfilename, @"\" + name);
                fi.Extention        = name.Remove(0, name.IndexOf("."));
                fi.Name             = name;

                e.ApplyChanges();

                return true;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// This will mark a file as deleted and add it to the recycle bin.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool FileDelete(this IServer e, string fullpath)
        {
            try
            {
                e.OnFileDeletingEvent?.Invoke(e, fullpath);

                string fileToDelete = fullpath.Remove(0, fullpath.LastIndexOf(@"\") + 1);
                string pathToFile   = fullpath.Remove(fullpath.LastIndexOf(@"\"));

                IFolder dl = (IFolder)e.DirectoryGetDirectory(pathToFile);

                if (dl == null)
                    return false;

                IFile fi = dl.Files.Where(f => f.Name == fileToDelete).FirstOrDefault();

                if (fi == null)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The file (" + fileToDelete + ") dosen't exist."));
                    return false;
                }

                fi.Properties = fi.Properties.SetFlags(ODiskFlags.DELETED);

                e.Header.RecycleBin = e.Header.RecycleBin.Append(fi.FullPath).ToArray();

                e.ApplyChanges();

                e.OnFileDeletedEvent?.Invoke(e, fi.FullPath);

                return true;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// This extracts a file from the data partition and saves to the destination file stream.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="fullpath"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static bool FileExtract(this IServer e, string fullpath, Stream destination)
        {
            try
            {

                // there is a cert on this device we will encrypt and bypass the standard.
                if (e.Certificate != null)
                    return e.FileExtractDecrypt(fullpath, destination);

                string      fn  = Path.GetFileName(fullpath);
                IContainer  di  = e.DirectoryGetDirectory(fullpath.Remove(fullpath.LastIndexOf(@"\")));
                IFile       fi  = di.Files.Where(f => f.Name == fn).FirstOrDefault();

                if (fi == null)
                {
                    e.OnErrorEvent?.Invoke(e, new Exception("The file (" + fn + ") dosen't exist."));
                    return false;
                }

                byte[]  buffer      = new byte[e.SpeedBuffer];
                long    position    = 0;
                int     length      = 0;

                e.VDiskFileStream.Position = fi.ExDataIndex;

                if (e.CheckInterrupts())
                {
                    try { destination.Close(); destination.Dispose(); } catch { }
                    return false;
                }

                while (position < fi.Length)
                {
                    if ((fi.Length - position) < e.SpeedBuffer)
                        buffer = new byte[fi.Length - position];
                    length = e.VDiskFileStream.Read(buffer, 0, buffer.Length);
                    e.OnFileExportingEvent?.Invoke(e, fi.FullPath, length);
                    destination.Write(buffer, 0, length);
                    position += length;
                    Thread.Sleep(1);
                }

                e.VDiskFileStream.Position = 0;
               
                string MD5chk = gl.EncryptMd5(destination, true);

                if (!fi.MD5.Equals(MD5chk))
                    e.OnErrorEvent?.Invoke(e, new Exception("The file has become corrupted, the md5 and sha1 checks are invalid!"));

                destination.Close();
                destination.Dispose();

                e.OnFileExportedEvent?.Invoke(e, fi.FullPath);

                return true;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// This extracts an encrypted file from the data partition and saves to the destination file stream.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="fullpath"></param>
        /// <param name="destination"></param>
        private static bool FileExtractDecrypt(this IServer e, string fullpath, Stream destination)
        {
            try
            {

                AesManaged aesManaged   = new();
                aesManaged.KeySize      = 256;
                aesManaged.BlockSize    = 128;
                aesManaged.Mode         = CipherMode.CBC;
                byte[] LenK             = new byte[4];
                byte[] LenIV            = new byte[4];

                string      fn = Path.GetFileName(fullpath);
                IContainer  di = e.DirectoryGetDirectory(fullpath.Remove(fullpath.LastIndexOf(@"\")));
                IFile       fi = di.Files.Where(f => f.Name == fn).FirstOrDefault();

                e.VDiskFileStream.Position = fi.ExDataIndex;
                e.VDiskFileStream.Read(LenK, 0, 4);
                e.VDiskFileStream.Read(LenIV, 0, 4);

                // Convert the lengths to integer values.
                byte[] KeyEncrypted = new byte[BitConverter.ToInt32(LenK, 0)];
                byte[] IV           = new byte[BitConverter.ToInt32(LenIV, 0)];

                //The header for the encrytion length
                int EncryptionHeaderLength = LenK.Length + LenIV.Length + KeyEncrypted.Length  + IV.Length;

                e.VDiskFileStream.Read(KeyEncrypted, 0, KeyEncrypted.Length);
                e.VDiskFileStream.Read(IV, 0, IV.Length);

                // Use CertificatePrivateKey to decrypt the AesManaged key, Decrypt the key.
                ICryptoTransform transform = aesManaged.CreateDecryptor(e.CertificatePrivateKey.Decrypt(KeyEncrypted, RSAEncryptionPadding.OaepSHA1), IV);
                
                // Decrypt the cipher text from from the FileSteam of the encrypted file (inFs) into the FileStream for the decrypted file (outFs).
                byte[]  buffer      = new byte[e.SpeedBuffer];
                int     length      = 0;
                int     position    = 0;
              
                if (e.CheckInterrupts())
                {
                    try { destination.Close(); destination.Dispose(); } catch { }
                    return false;
                }

                using (CryptoStream outStreamDecrypted = new(destination, transform, CryptoStreamMode.Write, true))
                {
                    long realLength = fi.Length - EncryptionHeaderLength;
                    while (position < realLength)
                    {
                        if ((realLength - position) < e.SpeedBuffer)
                            buffer = new byte[realLength - position];
                        length = e.VDiskFileStream.Read(buffer, 0, buffer.Length);
                        e.OnFileExportingEvent?.Invoke(e, fi.FullPath, length);
                        outStreamDecrypted.Write(buffer, 0, length);
                        position += length;
                        Thread.Sleep(1);
                    }
                    outStreamDecrypted.FlushFinalBlock();
                    outStreamDecrypted.Close();
                }

                e.VDiskFileStream.Position = 0;

                transform.Dispose();

                string MD5chk = gl.EncryptMd5(destination, true);
                
                destination.Close();
                destination.Dispose();

                if (!fi.MD5.Equals(MD5chk))
                    e.OnErrorEvent?.Invoke(e, new Exception("The file has become corrupted, the md5 and sha1 checks are invalid!"));

                e.OnFileExportedEvent?.Invoke(e, fi.FullPath);

                return true;

            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// Creates an internal vfs file stream from the file object.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static IFileStream CreateFileStream(this IServer e, IFile file)
        {

            IFileStream output = new OFileStream(e, file) {
                Position = 0
            };

            if (output.IsEncrypted)
            {
                
                AesManaged aesManaged   = new();
                aesManaged.KeySize      = 256;
                aesManaged.BlockSize    = 128;
                aesManaged.Mode         = CipherMode.CBC;
                byte[] LenK             = new byte[4];
                byte[] LenIV            = new byte[4];

                output.DataPartition.Read(LenK, 0, 4);
                output.DataPartition.Read(LenIV, 0, 4);

                // Convert the lengths to integer values.
                byte[] KeyEncrypted = new byte[BitConverter.ToInt32(LenK, 0)];
                byte[] IV           = new byte[BitConverter.ToInt32(LenIV, 0)];

                int headerLength = (LenK.Length + LenIV.Length + KeyEncrypted.Length + IV.Length);

                //The header for the encrytion length
                output.SetProtectedField("XHeaderLength", headerLength);

                output.DataPartition.Read(KeyEncrypted, 0, KeyEncrypted.Length);
                output.DataPartition.Read(IV, 0, IV.Length);

                // Use CertificatePrivateKey to decrypt the AesManaged key, Decrypt the key.
                var ms = new MemoryStream();
                output.SetProtectedField("XInnerStream",    ms);
                output.SetProtectedField("XStream",         new CryptoStream(ms, aesManaged.CreateDecryptor(e.CertificatePrivateKey.Decrypt(KeyEncrypted, RSAEncryptionPadding.OaepSHA1), IV), CryptoStreamMode.Write, true));                
                output.Position     = (long)headerLength;

            }

            return output;

        }

        /// <summary>
        /// Creates an internal vfs file stream to write to and builds the IFile item.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IFileStream CreateNewFileStream(this IServer e, string fullPath)
        {

            string      fileName    = Path.GetFileName(fullPath);
            IContainer  directory   = e.DirectoryGetDirectory(fullPath.Remove(fullPath.LastIndexOf(@"\")));

            if (directory.Files.Where(f => f.Name == fileName).Any())
                throw new Exception("The file (" + fileName + " ) already exists.");

            IFileStream output = new OFileStream(e) {
                File = new OFile(fullPath)
            };

            //Add the owner user id to the file access
            output.File.UserIndenties = output.File.UserIndenties.Append(e.Header.Requirements[0].UserId).ToArray();

            //Add all users from the directory to the file access.
            ((IFolder)directory).UserIndenties.ForEach(u => {
                if (!output.File.UserIndenties.Contains(u))
                    output.File.UserIndenties = output.File.UserIndenties.Append(u).ToArray();
            });


            if (output.IsEncrypted)
            {

                //Lets setup the data partition, with encrypted data we will allways add to the end
                output.File.ExDataIndex         = output.DataPartition.Length;
                output.DataPartition.Position   = output.DataPartition.Length;

                //Setup the encryption system and create a transform.
                AesManaged aesManaged       = new();
                aesManaged.KeySize          = 256;
                aesManaged.BlockSize        = 128;
                aesManaged.Mode             = CipherMode.CBC;
           
                byte[] keyEncrypted         = e.CertificatePublicKey.Encrypt(aesManaged.Key, RSAEncryptionPadding.OaepSHA1);
                byte[] LenK                 = BitConverter.GetBytes(keyEncrypted.Length);   // 4 bytes
                byte[] LenIV                = BitConverter.GetBytes(aesManaged.IV.Length);  // 4 bytes

                //Lets write the encryption file header first.
                byte[] section              = gl.Combine(LenK, LenIV, keyEncrypted, aesManaged.IV);

                output.DataPartition.SetLength(output.DataPartition.Length + section.Length);
                output.DataPartition.Write(section, 0, section.Length);

                output.File.Length  += section.Length;
                output.Length       += section.Length;

                // Use CertificatePrivateKey to decrypt the AesManaged key, Decrypt the key.
                output.SetProtectedField("XHeaderLength", section.Length);
                output.SetProtectedField("XInnerStream",  null);
                output.SetProtectedField("XStream",       new CryptoStream(output.DataPartition, aesManaged.CreateEncryptor(), CryptoStreamMode.Write, true));
                
                output.Position = section.Length;

            }

            return output;

        }

        /// <summary>
        /// Creates an internal vfs file stream from the file object.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Stream CreateSystemFileStream(this IServer e, IFile file)
        {

            IFileStream output = new OSystemFileStream(e, file) {
                Position = 0
            };

            if (output.IsEncrypted)
            {
                
                AesManaged aesManaged   = new();
                aesManaged.KeySize      = 256;
                aesManaged.BlockSize    = 128;
                aesManaged.Mode         = CipherMode.CBC;
                byte[] LenK             = new byte[4];
                byte[] LenIV            = new byte[4];

                output.DataPartition.Read(LenK, 0, 4);
                output.DataPartition.Read(LenIV, 0, 4);

                // Convert the lengths to integer values.
                byte[] KeyEncrypted = new byte[BitConverter.ToInt32(LenK, 0)];
                byte[] IV           = new byte[BitConverter.ToInt32(LenIV, 0)];

                int headerLength = (LenK.Length + LenIV.Length + KeyEncrypted.Length + IV.Length);

                //The header for the encrytion length
                output.SetProtectedField("XHeaderLength", headerLength);

                output.DataPartition.Read(KeyEncrypted, 0, KeyEncrypted.Length);
                output.DataPartition.Read(IV, 0, IV.Length);

                // Use CertificatePrivateKey to decrypt the AesManaged key, Decrypt the key.
                var ms = new MemoryStream();
                output.SetProtectedField("XInnerStream",    ms);
                output.SetProtectedField("XStream",         new CryptoStream(ms, aesManaged.CreateDecryptor(e.CertificatePrivateKey.Decrypt(KeyEncrypted, RSAEncryptionPadding.OaepSHA1), IV), CryptoStreamMode.Write, true));                
                output.Position     = (long)headerLength;

            }

            return (Stream)output;

        }

        /// <summary>
        /// Creates an internal vfs file stream to write to and builds the IFile item.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Stream CreateNewSystemFileStream(this IServer e, string fullPath)
        {

            string      fileName    = Path.GetFileName(fullPath);
            IContainer  directory   = e.DirectoryGetDirectory(fullPath.Remove(fullPath.LastIndexOf(@"\")));

            if (directory.Files.Where(f => f.Name == fileName).Any())
                throw new Exception("The file (" + fileName + " ) already exists.");

            IFileStream output = new OSystemFileStream(e) {
                File = new OFile(fullPath)
            };

            //Add the owner user id to the file access
            output.File.UserIndenties = output.File.UserIndenties.Append(e.Header.Requirements[0].UserId).ToArray();

            //Add all users from the directory to the file access.
            ((IFolder)directory).UserIndenties.ForEach(u => {
                if (!output.File.UserIndenties.Contains(u))
                    output.File.UserIndenties = output.File.UserIndenties.Append(u).ToArray();
            });


            if (output.IsEncrypted)
            {

                //Lets setup the data partition, with encrypted data we will allways add to the end
                output.File.ExDataIndex         = output.DataPartition.Length;
                output.DataPartition.Position   = output.DataPartition.Length;

                //Setup the encryption system and create a transform.
                AesManaged aesManaged       = new();
                aesManaged.KeySize          = 256;
                aesManaged.BlockSize        = 128;
                aesManaged.Mode             = CipherMode.CBC;
           
                byte[] keyEncrypted         = e.CertificatePublicKey.Encrypt(aesManaged.Key, RSAEncryptionPadding.OaepSHA1);
                byte[] LenK                 = BitConverter.GetBytes(keyEncrypted.Length);   // 4 bytes
                byte[] LenIV                = BitConverter.GetBytes(aesManaged.IV.Length);  // 4 bytes

                //Lets write the encryption file header first.
                byte[] section              = gl.Combine(LenK, LenIV, keyEncrypted, aesManaged.IV);

                output.DataPartition.SetLength(output.DataPartition.Length + section.Length);
                output.DataPartition.Write(section, 0, section.Length);

                output.File.Length  += section.Length;
                output.Length       += section.Length;

                // Use CertificatePrivateKey to decrypt the AesManaged key, Decrypt the key.
                output.SetProtectedField("XHeaderLength", section.Length);
                output.SetProtectedField("XInnerStream",  null);
                output.SetProtectedField("XStream",       new CryptoStream(output.DataPartition, aesManaged.CreateEncryptor(), CryptoStreamMode.Write, true));
                
                output.Position = section.Length;

            }

            return (Stream)output;

        }

        /// <summary>
        /// Creates a new file watcher with the directory container object.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static IFileWatcher CreateNewFileWatch(this IServer e, IContainer directory)
        {

            var output = new OFileWatcher()
            {
                Server      = e, 
                Directory   = directory, 
                Files       = directory.Files.Select(v => new { Key = v.FullPath, Value = v }).ToDictionary(o => o.Key, o => o.Value)
            };

            return output;
        
        }

        /// <summary>
        /// Creates a new file watcher with the directory path.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static IFileWatcher CreateNewFileWatch(this IServer e, string fullPath)
        {

            var di = e.DirectoryGetDirectory(fullPath);

            var output = new OFileWatcher()
            {
                Server      = e,
                Directory   = di,
                Files       = di.Files.Select(v => new { Key = v.FullPath, Value = v }).ToDictionary(o => o.Key, o => o.Value)
            };

            return output;

        }

        #endregion

        #region Recycle Bin

        /// <summary>
        /// Restores files or folders from source path supplied.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        public static bool RecycleBinRestore(this IServer e, string source)
        {
            try
            {

                string obj      = source.Remove(0, source.LastIndexOf(@"\") + 1);
                string path2obj = source.Remove(source.LastIndexOf(@"\"));

                IFolder dl      = (IFolder)e.DirectoryGetDirectory(path2obj);

                if (dl == null)
                    return false;

                if (e.IsPathFile(obj))
                {

                    if (!dl.Files.Where(f => f.Name == obj).Any())
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("The file (" + obj + ") dosen't exist."));
                        return false;
                    }

                    e.OnFileRestoringEvent?.Invoke(e, source);

                    IFile fi = e.DirectoryGetFile(source);

                    if (fi == null)
                        return false;

                    fi.Properties = fi.Properties.ClearFlags(ODiskFlags.DELETED);

                    if (dl.Properties.IsFlagSet(ODiskFlags.DELETED))
                    {
                        dl.Files        = dl.Files.Filter(f => f.Name != obj);
                        fi.FullPath     = e.Header.RootName + @"\" + fi.Name;
                        e.Header.Files  = e.Header.Files.Append(fi).ToArray();
                    }

                    e.Header.RecycleBin = e.Header.RecycleBin.Filter(f => f != fi.FullPath);

                    e.ApplyChanges();
                    e.OnFileRestoredEvent?.Invoke(e, fi.Name);
                }
                else
                {
                    IFolder di = dl.Directories.Where(d => d.Name == obj).FirstOrDefault();
                   
                    if (di == null)
                    {
                        e.OnErrorEvent?.Invoke(e, new Exception("The directory (" + obj + ") dosen't exist."));
                        return false;
                    }

                    e.DirectoryRestore(di);
                }

                return true;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return false;
            }
        }

        /// <summary>
        /// This will delete the containing structure, the return the file object to delete the data.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="filesToDelete">The file objects to remove from the data partition</param>
        /// <returns></returns>
        public static IServer RecycleBinEmpty(this IServer e, out IFile[] filesToDelete)
        {
           
            filesToDelete = Array.Empty<IFile>();

            try
            {
                e.OnEmptyRecycleBinPreperationEvent?.Invoke(e);

                List<IFile> prep = new();

                e.Header.RecycleBin.ForEach(o => {

                    string obj      = o.Remove(0, o.LastIndexOf(@"\") + 1);
                    string path2obj = o.Remove(o.LastIndexOf(@"\"));

                    if (e.IsPathFile(obj))
                    {
                        IFile fi = e.DirectoryGetFile(o);
                        if (!prep.Contains(fi))
                            prep.Add(fi);
                    }
                    else
                        prep.AddRange(e.DirectoryGetFilesRecursively(e.DirectoryGetDirectory(o)));

                });

                if (e.CheckInterrupts())
                    return e;

                filesToDelete = prep.ToArray();

                prep.Clear();

                return e;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        #endregion

        #region System

        /// <summary>
        /// Removes file data from the data partition of the disk.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="filesToDelete"></param>
        public static IServer RemoveDiskData(this IServer e, IFile[] filesToDelete)
        {
            try
            {
                e.OnEmptyRecycleBinStartEvent?.Invoke(e, e.Header.RecycleBin.Length);

                byte[]              buffer      = new byte[e.SpeedBuffer];
                long                position    = 0;
                IContainer          di          = default;
                IUserRequirements   owner       = default;

                //Set up the data partition.
                e.VDiskFileStream.Position = 0;
                e.VDiskFileStream.Seek(0, SeekOrigin.Begin);

                //Check for a canceled operation.
                if (e.CheckInterrupts())
                {
                    filesToDelete.Dispose(out _);
                    return e;
                }

                //For each file delete from partition
                foreach (IFile fi in filesToDelete)
                {
                    //Check for a canceled operation.
                    if (e.CheckInterrupts())
                    {
                        filesToDelete.Dispose(out _);
                        e.Cancel = false;
                        break;
                    }

                    //Find the owner of the file.
                    owner = e.Header.Requirements
                        .Where(r => r.UserId == fi.UserIndenties.FirstOrDefault())
                        .FirstOrDefault();

                    //Set the owners space used
                    owner.SpaceUsed -= fi.Length;

                    //Set the data partition position
                    e.VDiskFileStream.Position = fi.ExDataIndex;

                    //Write the empty data in the block used by the file, which wipes the data.
                    while (position < fi.Length)
                    {
                        if ((fi.Length - position) < e.SpeedBuffer)
                            buffer = new byte[fi.Length - position];
                        e.VDiskFileStream.Write(buffer, 0, buffer.Length);
                        position += buffer.Length;
                        Thread.Sleep(1);
                    }

                    position = 0;

                    ICluster clusterAfter = default;
                    ICluster clusterBefore = default;

                    //Get the clusters before or after to merge if need be
                    e.Header.EmptyClusters.ForEach(cluster => {

                        if (cluster.StartIndex == (fi.ExDataIndex + fi.Length))
                            clusterAfter = cluster;

                        if ((cluster.StartIndex + cluster.Length) == fi.ExDataIndex)
                            clusterBefore = cluster;

                    });

                    //Merge or create clusters to header of the system.
                    if (clusterAfter != null && clusterBefore != null)
                    {
                        clusterAfter.StartIndex = fi.ExDataIndex;
                        clusterAfter.Length += fi.Length;
                        clusterAfter.StartIndex = clusterBefore.StartIndex;
                        clusterAfter.Length += clusterBefore.Length;
                        e.Header.EmptyClusters = e.Header.EmptyClusters.Filter(c => c != clusterBefore);
                    }
                    else if (clusterAfter != null && clusterBefore == null)
                    {
                        clusterAfter.StartIndex = fi.ExDataIndex;
                        clusterAfter.Length += fi.Length;
                    }
                    else if (clusterAfter == null && clusterBefore != null)
                        clusterBefore.Length += fi.Length;
                    else if (clusterAfter == null && clusterBefore == null)
                        e.Header.EmptyClusters = e.Header.EmptyClusters.Append(new OCluster(fi.ExDataIndex, fi.Length)).ToArray();

                    //Remove the path from the recycle bin.
                    if (e.Header.RecycleBin.Contains(fi.FullPath))
                        e.Header.RecycleBin = e.Header.RecycleBin.Filter(f => f != fi.FullPath);

                    //Get the directory that contains the file object.
                    di = e.DirectoryGetDirectory(fi.FullPath.Remove(fi.FullPath.LastIndexOf(@"\")));
                    
                    //Remove the file object from the directory.
                    di.Files = di.Files.Filter(f => f.Name != fi.Name);

                    e.OnEmptyRecycleBinItemDeletedEvent?.Invoke(e);
                }

                //Remove the buffer from memory
                buffer.Dispose();

                //Reset the data partition
                e.VDiskFileStream.Position = 0;
                e.VDiskFileStream.Seek(0, SeekOrigin.Begin);

                //Remove the array from memory
                filesToDelete.Dispose(out _);

                //Check for a canceled operation.
                if (e.CheckInterrupts())
                    return e;

                //Remove any directories that need removing.
                e.Header.RecycleBin.ForEach(dir => {

                    string d_name   = dir.Remove(0, dir.LastIndexOf(@"\") + 1);
                    string d_parent = dir.Remove(dir.LastIndexOf(@"\"));

                    di = e.DirectoryGetDirectory(d_parent);

                    if (di.Directories.Where(d => d.Name == d_name).Any())
                        di.Directories = di.Directories.Filter(d => d.Name != d_name);

                    e.OnEmptyRecycleBinItemDeletedEvent?.Invoke(e);

                });

                //Clear the recycle bin array and reset it.
                e.Header.RecycleBin.Dispose(out _);
                e.Header.RecycleBin = Array.Empty<string>();

                //Apply all the changes to the header.
                e.ApplyChanges();

                e.OnEmptyRecycleBinCompleteEvent?.Invoke(e);
                return e;
            }
            catch (Exception ex)
            {
                e.OnErrorEvent?.Invoke(e, ex);
                return e;
            }
        }

        /// <summary>
        /// Defrags the data partition of the device and removes clusters.
        /// </summary>
        /// <param name="e"></param>
        public static IServer Defrag(this IServer e)
        {
            try
            {
                e.OnDefragePreperationEvent?.Invoke(e);

                //Lets create a temp path for storing data while the process is running.
                string temppath = e.VDisk.Remove(e.VDisk.LastIndexOf(@"\") + 1) + "system_defrag";
                if (!Directory.Exists(temppath))
                    Directory.CreateDirectory(temppath);

                Thread.Sleep(100);

                e.OnDefrageStatusEvent?.Invoke(e, "Creating defragmentation files list...");

                //Create a list of alll files in all folders in the system.
                IFile[] lf = e.DirectoryGetFilesRecursively(e.Header);

                Thread.Sleep(100);

                e.OnDefrageStatusEvent?.Invoke(e, "Starting defragmentation...");

                //For internal looping the process untill there are no clusters.
            Begining:

                //Setup a file stream to read the in and out the file data.
                FileStream m_expfile = null;

                //Set up the data partition of the vdisk
                e.VDiskFileStream.Position = 0;
                e.VDiskFileStream.Seek(0, SeekOrigin.Begin);

                //look in each cluster and move data.
                if (e.Header.EmptyClusters.Any())
                {
                    //If no files then reset clusters and add one the sise of the data partition.
                    if (lf.Length <= 0)
                    {
                        e.Header.EmptyClusters.Dispose(out _);
                        e.Header.EmptyClusters = Array.Empty<ICluster>();
                        e.Header.EmptyClusters = e.Header.EmptyClusters.Append(new OCluster(0, e.VDiskFileStream.Length)).ToArray();
                    }

                    //Each file save outside, merge clusters and rewrite to disk
                    foreach (IFile fi in lf)
                    {
                        e.OnDefragProgressResetEvent?.Invoke(e, 6);
                        e.OnDefrageStatusEvent?.Invoke(e, "Defragmenting ( " + fi.Name + " ), Please wait...");
                        Thread.Sleep(20);

                        bool gotonext = true;

                        foreach (ICluster cluster in e.Header.EmptyClusters)
                            if ((cluster.StartIndex + cluster.Length) == fi.ExDataIndex)
                                gotonext = false;

                        if (gotonext)
                            goto Next;

                        //Export file
                        byte[]  buffer      = new byte[e.SpeedBuffer];
                        long    position    = 0;
                        int     length      = 0;
                        m_expfile           = new(temppath + @"\" + fi.Name, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);

                        e.VDiskFileStream.Position = fi.ExDataIndex;

                        while (position < fi.Length)
                        {
                            if ((fi.Length - position) < e.SpeedBuffer)
                                buffer = new byte[fi.Length - position];
                            length = e.VDiskFileStream.Read(buffer, 0, buffer.Length);
                            m_expfile.Write(buffer, 0, length);
                            position += length;
                            Thread.Sleep(5);
                        }

                        position    = 0;
                        length      = 0;

                        buffer.Dispose();
                        m_expfile.Close();
                        m_expfile.Dispose();

                        e.OnDefragProgressNextEvent?.Invoke(e, 1);
                        Thread.Sleep(50);

                        //Remove file data
                        buffer = new byte[e.SpeedBuffer];
                        e.VDiskFileStream.Position = fi.ExDataIndex;
                        while (position < fi.Length)
                        {
                            if ((fi.Length - position) < e.SpeedBuffer)
                                buffer = new byte[fi.Length - position];
                            e.VDiskFileStream.Write(buffer, 0, buffer.Length);
                            position += buffer.Length;
                            Thread.Sleep(5);
                        }
                        position = 0;
                        length = 0;
                        buffer.Dispose();
                        e.OnDefragProgressNextEvent?.Invoke(e, 2);
                        Thread.Sleep(50);

                        //Fix Clusters
                        ICluster clusterAfter = default;
                        ICluster clusterBefore = default;
                        ICluster selectedCluster = default;
                       
                        foreach (ICluster cluster in e.Header.EmptyClusters)
                        {
                            if (cluster.StartIndex == (fi.ExDataIndex + fi.Length))
                                clusterAfter = cluster;
                            if ((cluster.StartIndex + cluster.Length) == fi.ExDataIndex)
                                clusterBefore = cluster;
                            Thread.Sleep(5);
                        }

                        if (clusterAfter != null && clusterBefore != null)
                        {
                            clusterAfter.StartIndex = fi.ExDataIndex;
                            clusterAfter.Length += fi.Length;
                            clusterAfter.StartIndex = clusterBefore.StartIndex;
                            clusterAfter.Length += clusterBefore.Length;
                            e.Header.EmptyClusters = e.Header.EmptyClusters.Filter(c => c != clusterBefore);
                            selectedCluster = clusterAfter;
                        }
                        else if (clusterAfter != null && clusterBefore == null)
                        {
                            clusterAfter.StartIndex = fi.ExDataIndex;
                            clusterAfter.Length += fi.Length;
                            selectedCluster = clusterAfter;
                        }
                        else if (clusterAfter == null && clusterBefore != null)
                        {
                            clusterBefore.Length += fi.Length;
                            selectedCluster = clusterBefore;
                        }

                        e.OnDefragProgressNextEvent?.Invoke(e, 3);
                        Thread.Sleep(50);

                        //Reset file entrie's starting index from selected cluster
                        fi.ExDataIndex = selectedCluster.StartIndex;
                        if (selectedCluster.Length == fi.Length)
                            e.Header.EmptyClusters = e.Header.EmptyClusters.Filter(c => c != selectedCluster);
                        else
                        {
                            selectedCluster.StartIndex += fi.Length;
                            selectedCluster.Length -= fi.Length;
                        }
                        e.OnDefragProgressNextEvent?.Invoke(e, 4);
                        Thread.Sleep(50);

                        //Import the file data
                        m_expfile = new FileStream(temppath + @"\" + fi.Name, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        
                        e.VDiskFileStream.Position = fi.ExDataIndex;
                        buffer = new byte[e.SpeedBuffer];
                        
                        while (position < fi.Length)
                        {
                            if ((fi.Length - position) < e.SpeedBuffer)
                                buffer = new byte[fi.Length - position];
                            length = m_expfile.Read(buffer, 0, buffer.Length);
                            e.VDiskFileStream.Write(buffer, 0, length);
                            position += length;
                            Thread.Sleep(5);
                        }

                        position    = 0;
                        length      = 0;

                        buffer.Dispose();
                        m_expfile.Close();
                        m_expfile.Dispose();

                        e.OnDefragProgressNextEvent?.Invoke(e, 5);
                        Thread.Sleep(50);

                        //Reset the file object with in the header
                        e.VDiskFileStream.Position = 0;
                        if (File.Exists(temppath + @"\" + fi.Name))
                            File.Delete(temppath + @"\" + fi.Name);
                       
                        e.OnDefragProgressNextEvent?.Invoke(e, 6);

                    Next:
                        Thread.Sleep(50);
                    }

                    //More than one cluster then start the process again.
                    if (e.Header.EmptyClusters.Length > 1)
                        goto Begining;

                    //If there is one left and the length is not the length of the data partition start the process again
                    if (e.Header.EmptyClusters.Length == 1)
                    {
                        long length = (e.Header.EmptyClusters[0].StartIndex + e.Header.EmptyClusters[0].Length);
                        if (e.VDiskFileStream.Length != length)
                            goto Begining;
                    }

                    //At this point we set the length of the data partition to the start on the last cluster.
                    e.VDiskFileStream.SetLength(e.Header.EmptyClusters[0].StartIndex);

                }

                //Reset the clusters in the header.
                e.Header.EmptyClusters.Dispose(out _);
                e.Header.EmptyClusters = Array.Empty<ICluster>();

                //Apply all the changes to the header.
                e.ApplyChanges();

                //remove the tempory data path for the  file data
                if (Directory.Exists(temppath))
                    Directory.Delete(temppath, true);

                Thread.Sleep(50);

                e.OnDefrageCompleteEvent?.Invoke(e);

                return e;

            }
            catch (Exception)
            {
                e.OnDefrageErrorEvent?.Invoke(e);
                return e;
            }
        }

        /// <summary>
        /// Merges another v disk with this one.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        public static IServer Merge(this IServer e, string source)
        {

            try
            {
                e.OnMergeStatusEvent?.Invoke(e, "Reading Disk Header...");

                IUserRequirements   adminUser       = e.Header.Requirements.FirstOrDefault();
                IHeader             sourceHeader    = default;
                FileStream          sourceStream    = default;

                //Get the header of the source device
                sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);

                byte[] a = new byte[4]; //int is 4 bytes in length (32bit / 64bit)

                sourceStream.Position = (int)sourceStream.Length - a.Length;

                int readlength = sourceStream.Read(a, 0, a.Length);

                a = new byte[BitConverter.ToInt32(a)]; // resize the buffer to the header length

                int tempFileLength = ((int)sourceStream.Length - readlength) - a.Length;

                sourceStream.Position = tempFileLength;

                _ = sourceStream.Read(a, 0, a.Length);

                //remove / truncate the disk and remove the header from the data, (partition)
                sourceStream.SetLength(tempFileLength);
                sourceStream.Flush();

                //Decompress the header stream and deserialize json
                sourceHeader = gl.DecompressData(a.ToMemoryStream()).GetHeader();

                e.OnMergeStatusEvent?.Invoke(e, "Starting Merge Process...");

                //Create a tempory server object to get the files from the source disk.
                IServer temp            = (IServer)Activator.CreateInstance(e.GetType());
                temp.VDisk              = source;
                temp.VDiskFileStream    = sourceStream;
                temp.Header             = sourceHeader;
                temp.Version            = sourceHeader.Version;

                IFile[] lsf = temp.DirectoryGetFilesRecursively(temp.Header);

                //Set up current datafile
                e.VDiskFileStream.Position = e.VDiskFileStream.Length;
                e.OnMergeStatusEvent?.Invoke(e, "Importing Files...");

                //Import data from soruce to current datafile
                lsf.ForEach(fi =>
                {

                    e.OnMergeProgressResetEvent?.Invoke(e, 3);
                    e.OnMergeStatusEvent?.Invoke(e, "Importing ( " + fi.Name + " ), Please wait...");

                    byte[]  buffer      = new byte[e.SpeedBuffer];
                    long    position    = 0;
                    int     length      = 0;

                    //Set the position in the temp data partition to read the file data
                    temp.VDiskFileStream.Position = fi.ExDataIndex;
                    e.OnMergeProgressNextEvent?.Invoke(e, 1);

                    //Write the data from the source disk to the current disk
                    while (position < fi.Length)
                    {
                        if ((fi.Length - position) < e.SpeedBuffer)
                            buffer = new byte[fi.Length - position];
                        length = temp.VDiskFileStream.Read(buffer, 0, buffer.Length);
                        e.VDiskFileStream.Write(buffer, 0, length);
                        position += length;
                        Thread.Sleep(1);
                    }

                    e.OnMergeProgressNextEvent?.Invoke(e, 2);

                    position    = 0;
                    length      = 0;
                    buffer.Dispose();
                    
                    //Update the new data position to the file object and root path
                    fi.ExDataIndex      = (e.VDiskFileStream.Position - fi.Length);
                    fi.FullPath         = fi.FullPath.Remove(0, fi.FullPath.IndexOf(@"\"));
                    fi.FullPath         = e.Header.RootName + fi.FullPath;
                    fi.UserIndenties.Dispose(out _);
                    fi.UserIndenties    = Array.Empty<long>();
                    fi.UserIndenties    = fi.UserIndenties.Append(adminUser.UserId).ToArray();

                    e.OnMergeProgressNextEvent?.Invoke(e, 3);

                    Thread.Sleep(50);

                });

                //close the source data partition stream.
                temp.VDiskFileStream.Close();
                temp.VDiskFileStream.Dispose();
                
                e.OnMergeStatusEvent?.Invoke(e, "Importing Header, Please wait...");

                //Import the source header to the current header.
                temp.Header.Files.ForEach(fi => {
                    e.Header.Files = e.Header.Files.Append(fi).ToArray();
                });

                temp.Header.Directories.ForEach(di => {
                    e.MergeFolderUsersAndPathRecursively(di, adminUser);
                    e.Header.Directories = e.Header.Directories.Append(di).ToArray();
                });

                e.ApplyChanges();

                temp.Dispose();

                e.OnMergeCompletedEvent?.Invoke(e);

                return e;

            }
            catch (Exception)
            {
                e.OnMergeErrorEvent?.Invoke(e);
                return e;
            }

        }

        /// <summary>
        /// Dumps a file as a raw data file to a stream.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="file"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static IServer DumpData(this IServer e, IFile file, Stream destination)
        {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string          DumpLine    = string.Empty;
            string          DataLine    = string.Empty;
            StreamWriter    RetVal      = new(destination);
            int             Address     = 0;
            byte            Pos         = 0;

            string Ch;
            byte[] Data         = new byte[1];

            for (long i = file.ExDataIndex; i <= (file.ExDataIndex + file.Length); i++)
            {

                e.VDiskFileStream.Position = i;
                e.VDiskFileStream.Read(Data, 0, Data.Length);

                Pos += 1;

                if (Pos > 16)
                {
                    Pos         = 1;
                    RetVal.Write(Strings.Format(Address, "0000") + "   " + DumpLine.Trim() + "   " + DataLine + ControlChars.CrLf);
                    DumpLine    = string.Empty;
                    DataLine    = string.Empty;
                    Address     += 1;
                }

                Ch = Conversion.Hex(Data[0]);

                if (Strings.Len(Ch) < 2)
                    Ch = "0" + Ch;

                DumpLine += Ch + (Pos == 8 ? "  " : " ");

                if ((Data[0] > 31 & Data[0] < 127) | (Data[0] > 127))
                    DataLine += Strings.Chr(Data[0]);
                else
                    DataLine += ".";

            }
            
            DumpLine += Strings.Space(((16 - Pos) * 3) + (Pos < 8 ? 1 : 0));
            
            RetVal.Write(Strings.Format(Address + 1, "0000") + "   " + DumpLine + "  " + DataLine);

            RetVal.Flush();

            return e;
        }

        /// <summary>
        /// Dumps the v-disk as a raw data file to a stream
        /// </summary>
        /// <param name="e"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static IServer DumpDisk(this IServer e, Stream destination)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string          DumpLine    = string.Empty;
            string          DataLine    = string.Empty;
            StreamWriter    RetVal      = new(destination);
            int             Address     = 0;
            byte            Pos         = 0;
            byte[]          Data        = new byte[1];
            string          Ch;
            long            Read        = 1;

            while (Read > 0) 
            {
                Read = e.VDiskFileStream.Read(Data, 0, Data.Length);

                Pos += 1;

                if (Pos > 16)
                {
                    Pos         = 1;
                    RetVal.Write(Strings.Format(Address, "0000") + "   " + DumpLine.Trim() + "   " + DataLine + ControlChars.CrLf);
                    DumpLine    = string.Empty;
                    DataLine    = string.Empty;
                    Address     += 1;
                }

                Ch = Conversion.Hex(Data[0]);
               
                if (Strings.Len(Ch) < 2)
                    Ch = "0" + Ch;

                DumpLine += Ch + (Pos == 8 ? "  " : " ");

                if ((Data[0] > 31 & Data[0] < 127) | (Data[0] > 127))
                    DataLine += Strings.Chr(Data[0]);
                else
                    DataLine += ".";

            }

            DumpLine += Strings.Space(((16 - Pos) * 3) + (Pos < 8 ? 1 : 0));

            RetVal.Write(Strings.Format(Address + 1, "0000") + "   " + DumpLine + "  " + DataLine);
            
            RetVal.Flush();

            return e;

        }


        #endregion

    }

}
