/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;

using K2host.Vfs.Enums;
using K2host.Vfs.Interface;

using gl = K2host.Core.OHelpers;

namespace K2host.Vfs.Classes
{
   
    public class OFolder : IFolder
    {

        /// <summary>
        /// The name of the file including the extention.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The path to this directory.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The full path and name of this directory.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// The size of the file in bytes
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// The date created as a long number  (Format as unixdatetime)
        /// </summary>
        public long DateTimeCreated { get; set; }

        /// <summary>
        ///  The date modified as a long number (Format as unixdatetime)
        /// </summary>
        public long DateTimeModified { get; set; }

        /// <summary>
        /// The file properties
        /// </summary>
        public ODiskFlags Properties { get; set; }

        /// <summary>
        /// Users that can access this object, empty means all access.
        /// </summary>
        public long[] UserIndenties { get; set; }

        /// <summary>
        /// Container for Files within this folder.
        /// </summary>
        public IFile[] Files { get; set; }

        /// <summary>
        /// Container for Folders within this folder.
        /// </summary>
        public IFolder[] Directories { get; set; }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OFolder()
        {
            Files               = Array.Empty<IFile>();
            Directories         = Array.Empty<IFolder>();
            UserIndenties       = Array.Empty<long>();
            DateTimeCreated     = gl.DateTime2UnixTime(DateTime.Now);
            DateTimeModified    = gl.DateTime2UnixTime(DateTime.Now);
            Properties          = ODiskFlags.READWRITE | ODiskFlags.DIRECTORY;
        }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OFolder(string fullPath)
            :this()
        {
            FullPath    = fullPath;
            Name        = FullPath.Remove(0, FullPath.LastIndexOf(@"\") + 1);
            Path        = FullPath.Remove(FullPath.LastIndexOf(@"\"));
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



                }

            IsDisposed = true;
        }

        #endregion

    }


}
