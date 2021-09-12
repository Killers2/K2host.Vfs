/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/

using System;

using K2host.Vfs.Enums;

namespace K2host.Vfs.Interface
{

    /// <summary>
    /// This is used to help create the object class you define.
    /// </summary>
    public interface IDiskObject : IDisposable
    {

        /// <summary>
        /// The name of the file including the extention.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The full file path and name
        /// </summary>
        string FullPath { get; set; }

        /// <summary>
        /// The size of the file in bytes
        /// </summary>
        long Length { get; set; }

        /// <summary>
        /// The date created as a long number  (Format as unixdatetime)
        /// </summary>
        long DateTimeCreated { get; set; }

        /// <summary>
        /// The date modified as a long number (Format as unixdatetime)
        /// </summary>
        long DateTimeModified { get; set; }

        /// <summary>
        /// The file properties
        /// </summary>
        ODiskFlags Properties { get; set; }

        /// <summary>
        /// Users that can access this object, empty means all access.
        /// </summary>
        long[] UserIndenties { get; set; }

    }

}
