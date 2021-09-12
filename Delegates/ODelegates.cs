/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;
using K2host.Vfs.Interface;

namespace K2host.Vfs.Delegates
{

    public delegate void OnRenamedEvent(IFileWatcher e, string originalFullPath, string newFullPath);
    public delegate void OnCreatedEvent(IFileWatcher e, string fullPath);
    public delegate void OnRemovedEvent(IFileWatcher e, string fullPath);

    public delegate void OnDeviceUpdate(IServer e);

    public delegate void OnMergeStart(IServer e);
    public delegate void OnMergeStatus(IServer e, string n);
    public delegate void OnMergeCompleted(IServer e);
    public delegate void OnMergeProgressReset(IServer e, long n);
    public delegate void OnMergeProgressNext(IServer e, long n);
    public delegate void OnMergeError(IServer e);

    public delegate void OnEmptyRecycleBinPreperation(IServer e);
    public delegate void OnEmptyRecycleBinStart(IServer e, long n);
    public delegate void OnEmptyRecycleBinItemDeleted(IServer e);
    public delegate void OnEmptyRecycleBinComplete(IServer e);

    public delegate void OnFileExporting(IServer e, string n, long m);
    public delegate void OnFileExported(IServer e, string n);
    public delegate void OnFileRestoring(IServer e, string n);
    public delegate void OnFileRestored(IServer e, string n);
    public delegate void OnFileDeleting(IServer e, string n);
    public delegate void OnFileDeleted(IServer e, string n);
    public delegate void OnFileAdding(IServer e, string n, long m);
    public delegate void OnFileAdded(IServer e, string n);
    public delegate void OnFileSaved(IServer e, IFile n);

    public delegate void OnDirectoryRestoring(IServer e, string n);
    public delegate void OnDirectoryRestored(IServer e, string n);
    public delegate void OnDirectoryDeleting(IServer e, string n);
    public delegate void OnDirectoryDeleted(IServer e, string n);
    public delegate void OnDirectoryAdded(IServer e, string n);

    public delegate void OnDefrageComplete(IServer e);
    public delegate void OnDefrageStatus(IServer e, string n);
    public delegate void OnDefragePreperation(IServer e);
    public delegate void OnDefrageError(IServer e);
    public delegate void OnDefragProgressReset(IServer e, long n);
    public delegate void OnDefragProgressNext(IServer e, long n);

    public delegate void OnCreateNewComplete(IServer e);
    public delegate void OnCreateNewStatus(IServer e, string n);

    public delegate void OnBackupComplete(IServer e);
    public delegate void OnBackupStatus(IServer e, string n);

    public delegate void OnRestoreComplete(IServer e);
    public delegate void OnRestoreStatus(IServer e, string n);

    public delegate void OnDismounted(IServer e);
    public delegate void OnMounted(IServer e);
    public delegate void OnError(IServer e, Exception ex);

}
