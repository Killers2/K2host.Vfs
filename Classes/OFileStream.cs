/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.IO;
using System.Security.Cryptography;
using K2host.Vfs.Extentions;
using K2host.Vfs.Interface;

namespace K2host.Vfs.Classes
{
   
    public class OFileStream : IFileStream
    {

        /// <summary>
        /// Used for the hasher to write the last block on close.
        /// </summary>
        protected byte[] mLastWriteBuffer = Array.Empty<byte>();
        
        /// <summary>
        /// The posistion stored here for property that triggers a seek
        /// </summary>
        protected long mPosition = 0;

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
        /// The start index of empty data.
        /// </summary>
        public long Position
        {
            get { return mPosition; } 
            set { mPosition = this.Seek(value, SeekOrigin.Begin); } 
        }

        /// <summary>
        /// The length of the stream.
        /// </summary>
        public long Length { get; set; }

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
        /// The status of the stream.
        /// </summary>
        public bool CanRead { get; }

        /// <summary>
        /// The status of the stream.
        /// </summary>
        public bool CanWrite { get; }

        /// <summary>
        /// The constructor read only
        /// </summary>
        public OFileStream(IServer server, IFile file)
        {
            IsEncrypted     = server.Certificate != null;
            Server          = server;
            DataPartition   = new(Server.VDisk, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            File            = file;
            CanWrite        = false;
            CanRead         = true;
            Length          = file.Length;
            XMd5Algorithm   = null;
        }

        /// <summary>
        /// The constructor read and write
        /// </summary>
        /// <param name="server"></param>
        public OFileStream(IServer server)
        {
            IsEncrypted     = server.Certificate != null;
            Server          = server;
            DataPartition   = new(Server.VDisk, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            File            = null;
            CanWrite        = true;
            CanRead         = true;
            Position        = 0;
            Length          = 0;
            XMd5Algorithm   = MD5.Create();
        }

        #region Destructor

        bool IsDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
                if (disposing)
                {
                    DataPartition?.Close();
                    DataPartition?.Dispose();
                }

            IsDisposed = true;
        }

        #endregion

    }


}
