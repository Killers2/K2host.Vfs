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
using System.Reflection;
using System.Security.Cryptography;
using K2host.Core;
using K2host.Vfs.Classes;
using K2host.Vfs.Interface;

using gl = K2host.Core.OHelpers;

namespace K2host.Vfs.Extentions
{
    
    public static class IFileStreamExtentions
    {

        /// <summary>
        /// Read data from the current stream with a buffer.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns>Total read byte length</returns>
        public static long Read(this IFileStream e, byte[] buffer, int offset, int length)
        {

            if (!e.CanRead)
                return 0;

            if (e.File != null)
                if ((e.Position + length) > e.File.Length)
                {
                    length = (int)(e.File.Length - e.Position);
                    e.Position = e.File.Length - length;
                }

            long read = Convert.ToInt64(e.DataPartition.Read(buffer, offset, length));

            e.Position += length;

            MemoryStream XInnerStream = e.GetProtectedField<MemoryStream>("XInnerStream");
            
            //Only for reading from the v-disk
            if (e.IsEncrypted && XInnerStream != null) 
            {

                CryptoStream XStream = e.GetProtectedField<CryptoStream>("XStream");

                long originalPosition = XInnerStream.Position;

                XStream.Write(buffer, 0, (int)read);

                if (e.Position == e.File.Length)
                    XStream.FlushFinalBlock();

                XInnerStream.Position = originalPosition;

                read = XInnerStream.Read(buffer, 0, buffer.Length);

            }

            return read;

        }

        /// <summary>
        /// Write data to the current stream with a buffer.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns>Total writen byte length</returns>
        public static long Write(this IFileStream e, byte[] buffer, int offset, int length) 
        {
            
            if (!e.CanWrite)
                return 0;

            //Lets start hashing the incomming data for an md5 hash file compare.           
            byte[] tempHash = new byte[length];
            e.GetProtectedField<HashAlgorithm>("XMd5Algorithm").TransformBlock(buffer, 0, length, tempHash, 0);
            tempHash.Dispose(out _);

            if (e.IsEncrypted)
            {
                //This will expand the disk automatically
                e.GetProtectedField<CryptoStream>("XStream").Write(buffer, 0, length);

                //Set the file length after encrytion as the length is not the same as the un-encrypted file
                e.File.Length = e.DataPartition.Position - e.File.ExDataIndex;

                //Plus the read to the current length.
                e.Length = e.File.Length;

            }
            else
            {
                //Set up teh disk for writing.
                e.DataPartition.Position = e.DataPartition.Length;
                e.DataPartition.SetLength(e.DataPartition.Length + length);
                try
                {
                    e.DataPartition.Write(buffer, offset, length);
                }
                catch (Exception)
                {
                    return 0;
                }

                //Plus the read to the current length.
                e.Length += length;

                //if we are writing then the file object should be new or null
                if (e.File != null && string.IsNullOrEmpty(e.File.MD5))
                    e.File.Length = e.Length;

            }

            //Set the position with out a seek
            e.SetProtectedField("mPosition", e.Length);

            //Set the incomming buffer the last written buffer to the protected field for the hashing squence
            byte[] lastBuffer = new byte[length];
            Buffer.BlockCopy(buffer, 0, lastBuffer, 0, lastBuffer.Length);
            e.SetProtectedField("mLastWriteBuffer", lastBuffer);

            return length;

        }

        /// <summary>
        /// Set the position of the potiner in the stream using SeekOrigin methods
        /// </summary>
        /// <param name="e"></param>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static long Seek(this IFileStream e, long offset, SeekOrigin origin)
        {

            long currentPosition = e.Position;

            if (origin == SeekOrigin.Begin)
            {
                //read refers to the file in the data partition.
                if (e.File != null && e.File.MD5 != string.Empty && offset <= e.File.Length)
                    e.DataPartition.Position = (e.File.ExDataIndex + offset);

                //write the pos refers to the data partition directly
                if ((e.File == null && e.DataPartition.Position >= e.Length) || (e.File != null && e.File.MD5 == string.Empty && e.DataPartition.Position >= e.Length))
                    e.DataPartition.Position = (e.DataPartition.Length - e.Length) + offset;

            }

            if (origin == SeekOrigin.Current)
            {
                //read refers to the file in the data partition.
                if (e.File != null && (e.Position + offset) <= e.File.Length)
                {
                    e.DataPartition.Position = (e.File.ExDataIndex + (e.Position + offset));
                    offset += currentPosition;
                }

                //write the pos refers to the data partition directly
                if (e.File == null && (e.DataPartition.Position + offset) <= e.DataPartition.Length)
                    e.DataPartition.Position = (e.DataPartition.Position + offset);

            }

            if (origin == SeekOrigin.End)
            {
                //read refers to the file in the data partition.
                if (e.File != null && offset > 0)
                {
                    e.DataPartition.Position = ((e.File.ExDataIndex + e.File.Length) - offset);
                    offset = (e.File.Length - offset);
                }

                //write the pos refers to the data partition directly
                if (e.File == null && (e.DataPartition.Length - offset) > 0)
                {
                    e.DataPartition.Position = (e.DataPartition.Length - offset);
                    offset = (e.Length - offset);
                }

            }

            e.GetType()
                .GetField("mPosition", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(e, offset);
            
            return e.Position;

        }

        /// <summary>
        /// Flushes any data to the output stream and updates and finializes the file object if creating from new..
        /// </summary>
        /// <param name="e"></param>
        public static void Flush(this IFileStream e) 
        { 
        
            //if we are a new file object then there will be no md5 hash
            if (e.File != null && string.IsNullOrEmpty(e.File.MD5))
            {

                //Get the last written incomming buffer for the hashing squence
                byte[] lastWriteBuffer = e.GetProtectedField<byte[]>("mLastWriteBuffer");

                //Use the last written incomming buffer for the final block.
                e.GetProtectedField<HashAlgorithm>("XMd5Algorithm").TransformFinalBlock(lastWriteBuffer, 0, lastWriteBuffer.Length);

                //Dispose of all resourses used for encryption
                if (e.IsEncrypted)
                {

                    if (e.CanWrite)
                    {
                        //Write the padding to the stream.
                        e.GetProtectedField<CryptoStream>("XStream").FlushFinalBlock();
                        e.File.Length   = e.DataPartition.Position - e.File.ExDataIndex;
                        e.Length        = e.File.Length;
                    }
                    e.GetProtectedField<CryptoStream>("XStream")?.Close();
                    e.GetProtectedField<MemoryStream>("XInnerStream")?.Close();
                    e.GetProtectedField<CryptoStream>("XStream")?.Dispose();
                    e.GetProtectedField<MemoryStream>("XInnerStream")?.Dispose();
                    e.SetProtectedField("XHeaderLength", 0);
                }
                else
                {
                    //Complete the file object
                    e.Seek(0, SeekOrigin.Begin);
                    e.File.ExDataIndex  = e.DataPartition.Position;
                }

                //Write the MD5 Hash of the original file before import
                e.File.MD5 = string.Join(string.Empty, e.GetProtectedField<HashAlgorithm>("XMd5Algorithm").Hash.Select(b => b.ToString("x2"))).ToUpper();

                e.GetProtectedField<HashAlgorithm>("XMd5Algorithm")?.Clear();
                e.GetProtectedField<HashAlgorithm>("XMd5Algorithm")?.Dispose();

                //Add the file to the directory
                IContainer directory = e.Server.DirectoryGetDirectory(e.File.FullPath.Remove(e.File.FullPath.LastIndexOf(@"\")));
                directory.Files = directory.Files.Append(e.File).ToArray();
                e.Server.ApplyChanges();
                e.Server.OnFileAddedEvent?.Invoke(e.Server, e.File.FullPath);
            }

        }

        /// <summary>
        /// Closes and releases the inner stream to the disk as a shared stream. Also finializes the file object if creating from new.
        /// If flush is used prior to this then close will only close the inner stream.
        /// </summary>
        /// <param name="e"></param>
        public static void Close(this IFileStream e)
        {

            //if we are a new file object then there will be no md5 hash
            if (e.File != null && string.IsNullOrEmpty(e.File.MD5))
            {

                //Get the last written incomming buffer for the hashing squence
                byte[] lastWriteBuffer = e.GetProtectedField<byte[]>("mLastWriteBuffer");

                //Use the last written incomming buffer for the final block.
                e.GetProtectedField<HashAlgorithm>("XMd5Algorithm").TransformFinalBlock(lastWriteBuffer, 0, lastWriteBuffer.Length);

                //Dispose of all resourses used for encryption
                if (e.IsEncrypted)
                {

                    if (e.CanWrite)
                    {
                        //Write the padding to the stream.
                        e.GetProtectedField<CryptoStream>("XStream").FlushFinalBlock();
                        e.File.Length   = e.DataPartition.Position - e.File.ExDataIndex;
                        e.Length        = e.File.Length;
                    }
                    e.GetProtectedField<CryptoStream>("XStream")?.Close();
                    e.GetProtectedField<MemoryStream>("XInnerStream")?.Close();
                    e.GetProtectedField<CryptoStream>("XStream")?.Dispose();
                    e.GetProtectedField<MemoryStream>("XInnerStream")?.Dispose();
                    e.SetProtectedField("XHeaderLength", 0);
                }
                else
                {
                    //Complete the file object
                    e.Seek(0, SeekOrigin.Begin);
                    e.File.ExDataIndex  = e.DataPartition.Position;
                }

                //Write the MD5 Hash of the original file before import
                e.File.MD5 = string.Join(string.Empty, e.GetProtectedField<HashAlgorithm>("XMd5Algorithm").Hash.Select(b => b.ToString("x2"))).ToUpper();

                e.GetProtectedField<HashAlgorithm>("XMd5Algorithm")?.Clear();
                e.GetProtectedField<HashAlgorithm>("XMd5Algorithm")?.Dispose();

                //Add the file to the directory
                IContainer directory = e.Server.DirectoryGetDirectory(e.File.FullPath.Remove(e.File.FullPath.LastIndexOf(@"\")));
                directory.Files = directory.Files.Append(e.File).ToArray();
                e.Server.ApplyChanges();
                e.Server.OnFileAddedEvent?.Invoke(e.Server, e.File.FullPath);
            }

            e.DataPartition?.Close();
            e.DataPartition?.Dispose();

        }

        /// <summary>
        /// Reads all the data into a non-expandable memory stream.
        /// This is the raw data, so if the system is encrypted then the data returned is encrypted.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static MemoryStream ToMemoryStream(this IFileStream e)
        {

            e.Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[e.Length];

            e.Read(buffer, 0, buffer.Length);

            var output = new MemoryStream(buffer);

            buffer.Dispose();

            e.Seek(0, SeekOrigin.Begin);

            return output;

        }

        /// <summary>
        /// Reads all the data into an array of bytes.
        /// This is the raw data, so if the system is encrypted then the data returned is encrypted.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static byte[] ToArray(this IFileStream e)
        {

            e.Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[e.Length];

            e.Read(buffer, 0, buffer.Length);

            e.Seek(0, SeekOrigin.Begin);

            return buffer;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static IFile CreateFile(this IFileStream e, string fullPath, IUserRequirements owner)
        {
            
            e.Seek(0, SeekOrigin.Begin);

            string      fileName    = Path.GetFileName(fullPath);
            IContainer  directory   = e.Server.DirectoryGetDirectory(fullPath.Remove(fullPath.LastIndexOf(@"\")));

            //Lets create the new file object
            IFile ret = new OFile(fullPath)
            {
                ExDataIndex = e.DataPartition.Position,
                Length      = e.Length,
                MD5         = gl.EncryptMd5(e.ToMemoryStream(), true)
            };

            //Add the owner user id to the file access
            ret.UserIndenties = ret.UserIndenties.Append(owner.UserId).ToArray();

            //Add all users from the directory to the file access.
            ((IFolder)directory).UserIndenties.ForEach(u => {
                if (!ret.UserIndenties.Contains(u))
                    ret.UserIndenties = ret.UserIndenties.Append(u).ToArray();
            });

            directory.Files = directory.Files.Append(ret).ToArray();

            e.Server.ApplyChanges();
            e.Server.OnFileAddedEvent?.Invoke(e.Server, ret.FullPath);

            return ret;

        }

    }

}
