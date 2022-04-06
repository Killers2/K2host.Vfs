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
using System.Security.Cryptography;
using K2host.Core;
using K2host.Vfs.Extentions;
using K2host.Vfs.Interface;

using gl = K2host.Core.OHelpers;

namespace K2host.Vfs.Classes
{

    /// <summary>
    /// This FileStream handles internal reading and writing of normal and encrypted files from a mounted device.
    /// This also implements the <see cref="IFileStream"/> for accessing extentions during a normal stream.
    /// </summary>
    public class OSystemFileStream : Stream, IFileStream
    {

        /// <summary>
        /// Used for the hasher to write the last block on close.
        /// </summary>
        protected byte[] mLastWriteBuffer = Array.Empty<byte>();
        
        /// <summary>
        /// The position stored here for property that triggers a seek
        /// </summary>
        protected long mPosition = 0;

        /// <summary>
        /// The length stored here.
        /// </summary>
        protected long mLength = 0;

        /// <summary>
        /// For Encrypted files
        /// </summary>
        protected int XHeaderLength = 0;

        /// <summary>
        /// For Encrypted files
        /// </summary>
        protected CryptoStream XStream = null;

        /// <summary>
        /// For Encrypted files
        /// </summary>
        protected MemoryStream XInnerStream = null;

        /// <summary>
        /// Used to build the MD5 File hash from an incomming stream.
        /// </summary>
        protected HashAlgorithm XMd5Algorithm = null;

        /// <summary>
        /// The encrypted status based on the server.
        /// </summary>
        public bool IsEncrypted { get; }

        /// <summary>
        /// The <see cref="IFile"/> object to read.
        /// </summary>
        public IFile File { get; set; }

        /// <summary>
        /// The data partition to read from
        /// </summary>
        public FileStream DataPartition { get; }

        /// <summary>
        /// The server with the mounted partition.
        /// </summary>
        public IServer Server { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public override bool CanRead { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public override bool CanSeek { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public override bool CanWrite { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public override long Length { get { return mLength; } }
        
        /// <summary>
        /// 
        /// </summary>
        public override long Position
        {
            get { return mPosition; }
            set { mPosition = this.Seek(value, SeekOrigin.Begin); }
        }

        /// <summary>
        /// 
        /// </summary>
        long IFileStream.Length { get { return mLength; } set { mLength = value; } }

        /// <summary>
        /// The constructor read only
        /// </summary>
        public OSystemFileStream(IServer server, IFile file)
        {
            IsEncrypted     = server.Certificate != null;
            Server          = server;
            DataPartition   = new(Server.VDisk, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            File            = file;
            CanWrite        = false;
            CanRead         = true;
            CanSeek         = true;
            XMd5Algorithm   = null;
            SetLength(file.Length);
        }

        /// <summary>
        /// The constructor read and write
        /// </summary>
        /// <param name="server"></param>
        public OSystemFileStream(IServer server)
        {
            IsEncrypted     = server.Certificate != null;
            Server          = server;
            DataPartition   = new(Server.VDisk, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            File            = null;
            CanWrite        = true;
            CanRead         = true; 
            CanSeek         = true;
            Position        = 0;
            XMd5Algorithm   = MD5.Create();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            mLength = value;
        }
       
        /// <summary>
        /// 
        /// </summary>
        public override void Flush()
        {

            //if we are a new file object then there will be no md5 hash
            if (File != null && string.IsNullOrEmpty(File.MD5))
            {

                //Use the last written incomming buffer for the final block.
                XMd5Algorithm.TransformFinalBlock(mLastWriteBuffer, 0, mLastWriteBuffer.Length);

                //Dispose of all resourses used for encryption
                if (IsEncrypted)
                {

                    if (CanWrite)
                    {
                        //Write the padding to the stream.
                        XStream.FlushFinalBlock();
                        File.Length = DataPartition.Position - File.ExDataIndex;
                        SetLength(File.Length);
                    }
                    XStream?.Close();
                    XInnerStream?.Close();
                    XStream?.Dispose();
                    XInnerStream?.Dispose();
                    XHeaderLength = 0;
                }
                else
                {
                    //Complete the file object
                    Seek(0, SeekOrigin.Begin);
                    File.ExDataIndex = DataPartition.Position;
                }

                //Write the MD5 Hash of the original file before import
                File.MD5 = string.Join(string.Empty, XMd5Algorithm.Hash.Select(b => b.ToString("x2"))).ToUpper();

                XMd5Algorithm?.Clear();
                XMd5Algorithm?.Dispose();

                //Add the file to the directory
                IContainer directory = Server.DirectoryGetDirectory(File.FullPath.Remove(File.FullPath.LastIndexOf(@"\")));
                directory.Files = directory.Files.Append(File).ToArray();
                Server.ApplyChanges();
                Server.OnFileAddedEvent?.Invoke(Server, File.FullPath);
            }

            DataPartition?.Close();
            DataPartition?.Dispose();

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int length)
        {

            if (!CanRead)
                return 0;

            if (File != null)
                if ((Position + length) > File.Length)
                {
                    length = (int)(File.Length - Position);
                    Position = File.Length - length;
                }

            int read = DataPartition.Read(buffer, offset, length);

            Position += length;

            //Only for reading from the v-disk
            if (IsEncrypted && XInnerStream != null)
            {

                long originalPosition = XInnerStream.Position;

                XStream.Write(buffer, 0, (int)read);

                if (Position == File.Length)
                    XStream.FlushFinalBlock();

                XInnerStream.Position = originalPosition;

                read = XInnerStream.Read(buffer, 0, buffer.Length);

            }

            return read;

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public override void Write(byte[] buffer, int offset, int length)
        {

            if (!CanWrite)
                return;

            //Lets start hashing the incomming data for an md5 hash file compare.           
            byte[] tempHash = new byte[length];
            XMd5Algorithm.TransformBlock(buffer, 0, length, tempHash, 0);
            tempHash.Dispose(out _);

            if (IsEncrypted)
            {
                //This will expand the disk automatically
                XStream.Write(buffer, 0, length);

                //Set the file length after encrytion as the length is not the same as the un-encrypted file
                File.Length = DataPartition.Position - File.ExDataIndex;

                //Plus the read to the current length.
                SetLength(File.Length);

            }
            else
            {
                //Set up teh disk for writing.
                DataPartition.Position = DataPartition.Length;
                DataPartition.SetLength(DataPartition.Length + length);
                try
                {
                    DataPartition.Write(buffer, offset, length);
                }
                catch (Exception)
                {
                    return;
                }

                //Plus the read to the current length.
                SetLength(Length + length);

                //if we are writing then the file object should be new or null
                if (File != null && string.IsNullOrEmpty(File.MD5))
                    File.Length = Length;

            }

            //Set the position with out a seek
            mPosition = Length;

            //Set the incomming buffer the last written buffer to the protected field for the hashing squence
            byte[] lastBuffer = new byte[length];
            Buffer.BlockCopy(buffer, 0, lastBuffer, 0, lastBuffer.Length);
            mLastWriteBuffer = lastBuffer;

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {

            long currentPosition = Position;

            if (origin == SeekOrigin.Begin)
            {
                //read refers to the file in the data partition.
                if (File != null && File.MD5 != string.Empty && offset <= File.Length)
                    DataPartition.Position = (File.ExDataIndex + offset);

                //write the pos refers to the data partition directly
                if ((File == null && DataPartition.Position >= Length) || (File != null && File.MD5 == string.Empty && DataPartition.Position >= Length))
                    DataPartition.Position = (DataPartition.Length - Length) + offset;

            }

            if (origin == SeekOrigin.Current)
            {
                //read refers to the file in the data partition.
                if (File != null && (Position + offset) <= File.Length)
                {
                    DataPartition.Position = (File.ExDataIndex + (Position + offset));
                    offset += currentPosition;
                }

                //write the pos refers to the data partition directly
                if (File == null && (DataPartition.Position + offset) <= DataPartition.Length)
                    DataPartition.Position = DataPartition.Position + offset;

            }

            if (origin == SeekOrigin.End)
            {
                //read refers to the file in the data partition.
                if (File != null && offset > 0)
                {
                    DataPartition.Position = ((File.ExDataIndex + File.Length) - offset);
                    offset = File.Length - offset;
                }

                //write the pos refers to the data partition directly
                if (File == null && (DataPartition.Length - offset) > 0)
                {
                    DataPartition.Position = (DataPartition.Length - offset);
                    offset = Length - offset;
                }

            }

            mPosition = offset;

            return Position;

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MemoryStream ToMemoryStream()
        {

            Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[Length];

            Read(buffer, 0, buffer.Length);

            var output = new MemoryStream(buffer);

            buffer.Dispose();

            Seek(0, SeekOrigin.Begin);

            return output;

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {

            Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[Length];

            Read(buffer, 0, buffer.Length);

            Seek(0, SeekOrigin.Begin);

            return buffer;

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public IFile CreateFile(string fullPath, IUserRequirements owner)
        {

            Seek(0, SeekOrigin.Begin);

            string fileName = Path.GetFileName(fullPath);
            IContainer directory = Server.DirectoryGetDirectory(fullPath.Remove(fullPath.LastIndexOf(@"\")));

            //Lets create the new file object
            IFile ret = new OFile(fullPath)
            {
                ExDataIndex = DataPartition.Position,
                Length = Length,
                MD5 = gl.EncryptMd5(ToMemoryStream(), true)
            };

            //Add the owner user id to the file access
            ret.UserIndenties = ret.UserIndenties.Append(owner.UserId).ToArray();

            //Add all users from the directory to the file access.
            ((IFolder)directory).UserIndenties.ForEach(u => {
                if (!ret.UserIndenties.Contains(u))
                    ret.UserIndenties = ret.UserIndenties.Append(u).ToArray();
            });

            directory.Files = directory.Files.Append(ret).ToArray();

            Server.ApplyChanges();
            Server.OnFileAddedEvent?.Invoke(Server, ret.FullPath);

            return ret;

        }

    }


}
