/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

using K2host.Vfs.Interface;
using K2host.Vfs.Delegates;
using K2host.Threading.Interface;

namespace K2host.Vfs.Classes
{
   
    public class OServer : IServer
    {

        /// <summary>
        /// 
        /// </summary>       
        public OnDeviceUpdate OnDeviceUpdateEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnMergeStart OnMergeStartEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnMergeStatus OnMergeStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnMergeCompleted OnMergeCompletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnMergeProgressReset OnMergeProgressResetEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnMergeProgressNext OnMergeProgressNextEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnMergeError OnMergeErrorEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnEmptyRecycleBinPreperation OnEmptyRecycleBinPreperationEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnEmptyRecycleBinStart OnEmptyRecycleBinStartEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnEmptyRecycleBinItemDeleted OnEmptyRecycleBinItemDeletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnEmptyRecycleBinComplete OnEmptyRecycleBinCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileExporting OnFileExportingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileExported OnFileExportedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileRestoring OnFileRestoringEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileRestored OnFileRestoredEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileDeleting OnFileDeletingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileDeleted OnFileDeletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileAdding OnFileAddingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileAdded OnFileAddedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnFileSaved OnFileSavedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDirectoryRestoring OnDirectoryRestoringEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDirectoryRestored OnDirectoryRestoredEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDirectoryDeleting OnDirectoryDeletingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDirectoryDeleted OnDirectoryDeletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDirectoryAdded OnDirectoryAddedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDefrageComplete OnDefrageCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDefrageStatus OnDefrageStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDefragePreperation OnDefragePreperationEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDefrageError OnDefrageErrorEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDefragProgressReset OnDefragProgressResetEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDefragProgressNext OnDefragProgressNextEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnCreateNewComplete OnCreateNewCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnCreateNewStatus OnCreateNewStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnBackupComplete OnBackupCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnBackupStatus OnBackupStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnRestoreComplete OnRestoreCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnRestoreStatus OnRestoreStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnDismounted OnDismountedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnMounted OnMountedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OnError OnErrorEvent { get; set; }

        /// <summary>
        /// The thread manager.
        /// </summary>
        public IThreadManager ThreadManager { get; }

        /// <summary>
        /// The version of this server
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The key used for password encryption.
        /// </summary>
        public string SystemKey { get; set; }

        /// <summary>
        /// The buffer used for (4096 4kb), (1048576 1Mb)
        /// </summary>
        public int SpeedBuffer { get; set; }

        /// <summary>
        /// Used on the server to interrupt actions
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Used on the header to determine a change.
        /// </summary>
        public int HeaderChange { get; set; }

        /// <summary>
        /// The VD header.
        /// </summary>
        public IHeader Header { get; set; }

        /// <summary>
        /// Used to determin the VD is mounted
        /// </summary>
        public bool IsMounted { get; set; }

        /// <summary>
        /// The path to the VD file.
        /// </summary>
        public string VDisk { get; set; }

        /// <summary>
        /// The file strteam used to read the VD file.
        /// </summary>
        public FileStream VDiskFileStream { get; set; }

        /// <summary>
        /// The system cert to encrypt this device.
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// The system cert public key for encryption.
        /// </summary>
        public RSA CertificatePublicKey { get; set; }

        /// <summary>
        /// The system cert private key for decryption.
        /// </summary>
        public RSA CertificatePrivateKey { get; set; }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OServer(IThreadManager threadManager)
        {
            ThreadManager           = threadManager;
            Certificate             = null;
            CertificatePublicKey    = null;
            CertificatePrivateKey   = null;
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
                    VDiskFileStream?.Close();
                    VDiskFileStream?.Dispose();
                }

            IsDisposed = true;
        }

        #endregion

    }


}
