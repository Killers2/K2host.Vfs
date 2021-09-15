
# K2host.Vfs

A Virtual file system library supporting certificate encryption and system streaming.

Nuget Package: https://www.nuget.org/packages/K2host.Vfs/

----------------------------------------------------------------------------------------------------------------

# Getting Started

You will need to create the instance of the engine.<br />
The engine has lots of callbacks that allow you to trigger enternal events.
When creating the instance you will need to pass an instance of the OThreadManager

```c#
var engine = new OServer(new OThreadManager())
{
                
    OnMountedEvent              = (e) => { Console.WriteLine("Mounted."); },
    OnDismountedEvent           = (e) => { Console.WriteLine("Dismounted."); },
    OnDeviceUpdateEvent         = (e) => { Console.WriteLine("Device Updated."); },
               
    OnBackupStatusEvent         = (e, status) => { Console.WriteLine(status); },
    OnBackupCompleteEvent       = (e) => { Console.WriteLine("Backup Complete."); },
                
    OnRestoreStatusEvent        = (e, status) => { Console.WriteLine(status); },
    OnRestoreCompleteEvent      = (e) => { Console.WriteLine("Restore Complete."); },
                
    OnCreateNewStatusEvent      = (e, status) => { Console.WriteLine(status); },
    OnCreateNewCompleteEvent    = (e) => { Console.WriteLine("Create New Complete."); },
                
    OnDefragePreperationEvent   = (e) => { Console.WriteLine("Defrage Preperation."); },
    OnDefrageStatusEvent        = (e, status) => { Console.WriteLine(status); },
    OnDefragProgressNextEvent   = (e, status) => { Console.WriteLine("Defrage Pass " + status.ToString()); },
    OnDefragProgressResetEvent  = (e, status) => { Console.WriteLine("Defrage Reset " + status.ToString()); },
    OnDefrageCompleteEvent      = (e) => { Console.WriteLine("Defrage Complete."); },
    OnDefrageErrorEvent         = (e) => { Console.WriteLine("Defrage Error."); },

    OnDirectoryAddedEvent       = (e, status) => { Console.WriteLine(status); },

    OnDirectoryDeletingEvent    = (e, status) => { Console.WriteLine(status); },
    OnDirectoryDeletedEvent     = (e, status) => { Console.WriteLine(status); },
    OnDirectoryRestoredEvent    = (e, status) => { Console.WriteLine(status); },
    OnDirectoryRestoringEvent   = (e, status) => { Console.WriteLine(status); },

    OnEmptyRecycleBinPreperationEvent   = (e) => { Console.WriteLine("Empty Recycle Bin Preperation"); },
    OnEmptyRecycleBinStartEvent         = (e, status) => { Console.WriteLine(status); },
    OnEmptyRecycleBinItemDeletedEvent   = (e) => { Console.WriteLine("Empty Recycle Bin Item Deleted"); },
    OnEmptyRecycleBinCompleteEvent      = (e) => { Console.WriteLine("Empty Recycle Bin Complete."); },

    OnFileAddedEvent            = (e, status) => { Console.WriteLine(status); },
    OnFileAddingEvent           = (e, status, n) => { Console.WriteLine(status); },
    OnFileDeletedEvent          = (e, status) => { Console.WriteLine(status); },
    OnFileDeletingEvent         = (e, status) => { Console.WriteLine(status); },
    OnFileExportedEvent         = (e, status) => { Console.WriteLine(status); },
    OnFileExportingEvent        = (e, status, n) => { Console.WriteLine(status); },
    OnFileRestoredEvent         = (e, status) => { Console.WriteLine(status); },
    OnFileRestoringEvent        = (e, status) => { Console.WriteLine(status); },
    OnFileSavedEvent            = (e, status) => { Console.WriteLine(status); },

    OnMergeCompletedEvent       = (e) => { Console.WriteLine("Merge Completed."); },
    OnMergeErrorEvent           = (e) => { Console.WriteLine("Merge Error."); },
    OnMergeProgressNextEvent    = (e, n) => { Console.WriteLine("Merge Progress Next " +  n.ToString()); },
    OnMergeProgressResetEvent   = (e, n) => { Console.WriteLine("Merge Progress Reset " + n.ToString()); },
    OnMergeStartEvent           = (e) => { Console.WriteLine("Merge Start."); },
    OnMergeStatusEvent          = (e, status) => { Console.WriteLine(status); },

    OnErrorEvent                = (e, ex) => { Console.WriteLine(ex.ToString()); },

    Version                     = "CodeName: VFSV5",
    SpeedBuffer                 = 4096 * 4, // 16384

};
```

The Version property is a name of your choosing.<br />
The SpeedBuffer property is the chunk size when reading and writing to and from streams, the bigger the buffer size the faster streams read and write.

----------------------------------------------------------------------------------------------------------------

# Creating a new vdisk

An example for creating a new vdisk using the engine.

```c#
try
{
    //This can be any ERP / CRM user from any software where all we need is the ID of the object set as the owner of the vdisk.
    OErpUser owner = OErpUser.Retrieve(1, ConnectionString, out _);

    OServer engine = new(new OThreadManager())
    {
        Version = "CodeName: VFSV5",
        OnCreateNewStatusEvent = (e, n) => { Console.WriteLine(n); },
        OnCreateNewCompleteEvent = (e) => { Console.WriteLine("Completed.."); }
    };

    engine.CreateNew(
        @"C:\Vdisks\Vdisk.vddf",
        @"C:",  // The root alias in the Vdisk.
        new OUserRequirements()
        {
            UserId = owner.Uid,
            SpaceLimit = -1,    // Any size allowed.
            SpaceUsed = 0
        },
        "SOME-AES-ENCRYPTION-KEY",
        string.Empty,
        string.Empty
    ).Dispose();

    owner.Dispose();

}
catch (Exception ex) {

    Console.Write(ex.Message);

}
```

For a new encrypted vdisk.

```c#
try
{
    //This can be any ERP / CRM user from any software where all 
    //we need is the ID of the object set as the owner of the vdisk.
    OErpUser owner = OErpUser.Retrieve(1, ConnectionString, out _);

    OServer engine = new(new OThreadManager())
    {
        Version = "CodeName: VFSV5",
        OnCreateNewStatusEvent = (e, n) => { Console.WriteLine(n); },
        OnCreateNewCompleteEvent = (e) => { Console.WriteLine("Completed.."); }
    };

    engine.CreateNew(
        @"C:\Vdisks\Vdisk.vddf",
        @"C:",  // The root alias in the Vdisk.
        new OUserRequirements()
        {
            UserId = owner.Uid,
            SpaceLimit = -1,    // Any size allowed.
            SpaceUsed = 0
        },
        "SOME-AES-ENCRYPTION-KEY",
        "THE PATH TO YOUR CERT.PFX FILE",
        "THE PASSWORD FOR YOUR CERT PFX FILE"
    ).Dispose();

    owner.Dispose();

}
catch (Exception ex) {

    Console.Write(ex.Message);

}
```
# Mounting and dismounting a vdisk

After creating a disk you can mount it using:

```c#
engine
    .Mount(@"C:\Vdisks\Vdisk.vddf");
```
Dismount the disk by using:

```c#
engine
    .Dismount()
    .Dispose();
```

# Backup and restore a vdisk

To back up the vdisk use:

```c#
try
{
    engine.Mount(@"C:\Vdisks\Vdisk.vddf")
        .Backup()
        .Dismount()
        .Dispose();
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```

Then to restaore the vdisk using:

```c#
try
{
    engine.Mount(@"C:\Vdisks\Vdisk.vddf");

    engine.Restore(@"C:\Vdisks\V-Disk Backup\THE BACKED UP FILE.zip")
        .Dismount()
        .Dispose();
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```

----------------------------------------------------------------------------------------------------------------

For dumping the vdisk as a dump you can use:
```c#
try
{
    FileStream dump = new(@"C:\Vdisks\disk.dat", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
                    
    engine.Mount(@"C:\Vdisks\Vdisk.vddf")
        .DumpDisk(dump)
        .Dismount()
        .Dispose();

    dump.Close();
    dump.Dispose();
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```

# Defrag the vdisk

This process removes clusters, moves files and shrinks the disk if needed.

```c#
try
{
    engine.Mount(@"C:\Vdisks\Vdisk.vddf")
        .Defrag()
        .Dismount()
        .Dispose();

}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```

# Files and folders

An example method for adding folders and files:

```c#
try
{

    engine.Mount(@"C:\Vdisks\Vdisk.vddf");

    var owner = engine.Header.Requirements[0];

    IFolder di;

    di = engine.DirectoryCreateDirectory(@"C:\Test Files", owner);
    di = engine.DirectoryCreateDirectory(@"C:\Test Files\Stuff", owner);
    di = engine.DirectoryCreateDirectory(@"C:\Test Files\Stuff\AnotherDir", owner);

    engine.FileAdd(
        @"C:\Test Files", // Destination
        new FileStream(@"D:\Development\SomeFile.pdf", FileMode.Open, FileAccess.Read, FileShare.Read), // Source
        owner
    );

    engine.FileAdd(
        @"C:\Test Files\Stuff",
        new FileStream(@"D:\Development\SomeFile2.pdf", FileMode.Open, FileAccess.Read, FileShare.Read), // Source
        owner
    );

    engine.FileAdd(
        @"C:\Test Files\Stuff\AnotherDir",
        new FileStream(@"D:\Development\SomeFile3.pdf", FileMode.Open, FileAccess.Read, FileShare.Read), // Source
        owner
    );

    engine.Dismount()
        .Dispose();

}
catch (Exception ex)
{
    Console.Write(ex.Message); 
}
```

An example method for adding files using streams:

```c#
try
{
    engine.Mount(@"C:\Vdisks\Vdisk.vddf");

    FileStream  fs      = new(@"D:\Development\SomeFile.pdf", FileMode.Open, FileAccess.Read, FileShare.Read);  // Source Stream
    byte[]      buffer  = new byte[2048];
    int         read    = 1;
    IFileStream stream  = engine.CreateNewFileStream(@"C:\Test Files\Stuff\AnotherDir\SomeFile.pdf");           // Destination Stream
               
    while (read > 0)
    {

        read = fs.Read(buffer, 0, buffer.Length);

        long written = stream.Write(buffer, 0, read);

    }

    stream.Close();
    stream.Dispose();

    engine.FileExtract(
        @"C:\Test Files\Stuff\AnotherDir\SomeFile.pdf", // Source File
        new FileStream(@"C:\SomeFile.pdf", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite) // Destination Stream
    );

    engine.Dismount()
        .Dispose();
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```

An example method for downloading or extracting files from the vdisk:

```c#
try
{

    engine.Mount(@"C:\Vdisks\Vdisk.vddf");

    IFile fi = engine.DirectoryGetFile(@"C:\Test Files\SomeFile.pdf");

    engine.FileExtract(
        fi.FullPath,
        new FileStream(@"C:\SomeFile.pdf", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite)
    );

    fi = engine.DirectoryGetFile(@"C:\Test Files\SomeFile2.docx");

    engine.FileExtract(
        fi.FullPath,
        new FileStream(@"C:\SomeFile2.docx", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite)
    );

    fi = engine.DirectoryGetFile(@"C:\Test Files\SomeFile3.csv");

    engine.FileExtract(
        fi.FullPath,
        new FileStream(@"C:\SomeFile3.csv", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite)
    );

    engine.Dismount()
        .Dispose();
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```

An example method for downloading or extracting files from the vdisk using streams:

```c#
try
{
    engine.Mount(@"C:\Vdisks\Vdisk.vddf");

    IFile       fi          = engine.DirectoryGetFile(@"C:\Test Files\SomeFile.pdf");
    IFileStream output      = engine.CreateFileStream(fi);
    FileStream  fs          = new(@"C:\SomeFile.pdf", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
    byte[]      buffer      = new byte[engine.SpeedBuffer];
    long        read        = 0;

    while (output.Position < output.Length) 
    {
        read = output.Read(buffer, 0, buffer.Length);
        fs.Write(buffer, 0, (int)read);
    }

    output.Flush();
    output.Close();
    output.Dispose();

    fs.Close();
    fs.Dispose();

    engine.Dismount()
        .Dispose();
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}

```

# File data dumps

Here is an exmple for dumping the file data from the vdisk.

```c#
try
{
    engine.Mount(@"C:\Vdisks\Vdisk.vddf");

    FileStream dump = new(@"C:\datadump.dat", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite); // Destination Stream
                
    engine.DumpData(engine.DirectoryGetFile(@"C:\Test Files\Stuff\AnotherDir\SomeFile.pdf"), dump); // Source File
                
    dump.Close();
    dump.Dispose();

    engine.Dismount()
        .Dispose();

}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```
# Deleting files and the RecycleBin

Below shows you how to delete a file and restore a file from the bin.

```c#
try
{
    engine.Mount(@"C:\Vdisks\Vdisk.vddf");

    engine.FileDelete(@"C:\Test Files\Stuff\SomeFile.docx");

    engine.RecycleBinRestore(@"C:\Test Files\Stuff\SomeFile.docx");
                
    engine.Dismount()
        .Dispose();

    Assert.IsTrue(true);

}
catch (Exception ex)
{

    Console.Write(ex.Message);

    Assert.IsTrue(false);

}
```

This method shows how to empty the bin and remove file data.
This will delete the containing structure, then return the file object(s) to delete the data.

```c#
try
{

    engine.Mount(@"C:\Vdisks\Vdisk.vddf");
   
    engine.FileDelete(@"C:\Test Files\Stuff\AnotherDir\SomeFile.pdf");

    engine.RecycleBinEmpty(out IFile[] filesToDelete)   //Empties the header container and passes the file objects back to be removed from the disk
        .RemoveDiskData(filesToDelete)                  //Removes the file data from the vdisk partition.
        .Dismount()
        .Dispose();
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
```
Enjoy :)
