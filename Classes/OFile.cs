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
   
    public class OFile : IFile
    {

        /// <summary>
        /// The name of the file including the extention.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The file extention
        /// </summary>
        public string Extention { get; set; }

        /// <summary>
        /// The full file path and name
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
        /// The date modified as a long number (Format as unixdatetime)
        /// </summary>
        public long DateTimeModified { get; set; }

        /// <summary>
        /// The Original File MD5 Hash for checking data integrity
        /// </summary>
        public string MD5 { get; set; }

        /// <summary>
        /// The file data start index
        /// </summary>
        public long ExDataIndex { get; set; }

        /// <summary>
        /// The file properties
        /// </summary>
        public ODiskFlags Properties { get; set; }

        /// <summary>
        /// Users that can access this object, empty means all access.
        /// </summary>
        public long[] UserIndenties { get; set; }

        /// <summary>
        /// The constructor
        /// </summary>
        public OFile()
        {
            Properties          = ODiskFlags.READ | ODiskFlags.FILE;
            DateTimeCreated     = gl.DateTime2UnixTime(DateTime.Now);
            DateTimeModified    = gl.DateTime2UnixTime(DateTime.Now);
            UserIndenties       = Array.Empty<long>();
            MD5                 = string.Empty;
        }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="fullname">This is the full file path including the name</param>
        public OFile(string fullname)
            :this()
        {
            FullPath    = fullname;
            Name        = FullPath.Remove(0, FullPath.LastIndexOf(@"\") + 1);
            Extention   = Name.Remove(0, Name.LastIndexOf("."));
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
