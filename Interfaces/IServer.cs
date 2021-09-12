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
using System.Security.Cryptography.X509Certificates;
using K2host.Threading.Interface;
using K2host.Vfs.Delegates;

namespace K2host.Vfs.Interface
{

    /// <summary>
    /// This is used to help create the object class you define.
    /// </summary>
    public interface IServer : IDisposable
    {

        /// <summary>
        /// 
        /// </summary>       
        OnDeviceUpdate OnDeviceUpdateEvent { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        OnMergeStart OnMergeStartEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnMergeStatus OnMergeStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnMergeCompleted OnMergeCompletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnMergeProgressReset OnMergeProgressResetEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnMergeProgressNext OnMergeProgressNextEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnMergeError OnMergeErrorEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnEmptyRecycleBinPreperation OnEmptyRecycleBinPreperationEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnEmptyRecycleBinStart OnEmptyRecycleBinStartEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnEmptyRecycleBinItemDeleted OnEmptyRecycleBinItemDeletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnEmptyRecycleBinComplete OnEmptyRecycleBinCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileExporting OnFileExportingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileExported OnFileExportedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileRestoring OnFileRestoringEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileRestored OnFileRestoredEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileDeleting OnFileDeletingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileDeleted OnFileDeletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileAdding OnFileAddingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileAdded OnFileAddedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnFileSaved OnFileSavedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDirectoryRestoring OnDirectoryRestoringEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDirectoryRestored OnDirectoryRestoredEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDirectoryDeleting OnDirectoryDeletingEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDirectoryDeleted OnDirectoryDeletedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDirectoryAdded OnDirectoryAddedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDefrageComplete OnDefrageCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDefrageStatus OnDefrageStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDefragePreperation OnDefragePreperationEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDefrageError OnDefrageErrorEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDefragProgressReset OnDefragProgressResetEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDefragProgressNext OnDefragProgressNextEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnCreateNewComplete OnCreateNewCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnCreateNewStatus OnCreateNewStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnBackupComplete OnBackupCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnBackupStatus OnBackupStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnRestoreComplete OnRestoreCompleteEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnRestoreStatus OnRestoreStatusEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnDismounted OnDismountedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnMounted OnMountedEvent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        OnError OnErrorEvent { get; set; }

        /// <summary>
        /// The thread manager.
        /// </summary>
        IThreadManager ThreadManager { get; }

        /// <summary>
        /// The key used for password encryption.
        /// </summary>
        string SystemKey { get; set; }

        /// <summary>
        /// The version of this server
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// The buffer used for (4096 4kb), (1048576 1Mb)
        /// </summary>
        int SpeedBuffer { get; set; }

        /// <summary>
        /// Used on the server to interrupt actions
        /// </summary>
        bool Cancel { get; set; }

        /// <summary>
        /// Used on the header to determine a change.
        /// </summary>
        int HeaderChange { get; set; }

        /// <summary>
        /// The VD header.
        /// </summary>
        IHeader Header { get; set; }

        /// <summary>
        /// Used to determin the VD is mounted
        /// </summary>
        bool IsMounted { get; set; }
      
        /// <summary>
        /// The path to the VD file.
        /// </summary>
        string VDisk { get; set; }

        /// <summary>
        /// The file strteam used to read the VD file.
        /// </summary>
        FileStream VDiskFileStream { get; set; }

        /// <summary>
        /// The system cert to encrypt this device.
        /// </summary>
        X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// The system cert public key for encryption.
        /// </summary>
        RSA CertificatePublicKey { get; set; }

        /// <summary>
        /// The system cert private key for decryption.
        /// </summary>
        RSA CertificatePrivateKey { get; set; }


    }

}
