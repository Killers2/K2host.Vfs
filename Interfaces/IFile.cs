/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;

namespace K2host.Vfs.Interface
{

    /// <summary>
    /// This is used to help create the object class you define.
    /// </summary>
    public interface IFile : IDiskObject, IDisposable
    {

        /// <summary>
        /// The file extention
        /// </summary>
        string Extention { get; set; }

        /// <summary>
        /// The Original File MD5 Hash for checking data integrity
        /// </summary>
        string MD5 { get; set; }

        /// <summary>
        /// The file data start index
        /// </summary>
        long ExDataIndex { get; set; }

    }

}
