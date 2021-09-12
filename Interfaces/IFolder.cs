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
    public interface IFolder : IDiskObject, IContainer, IDisposable
    {
        
        /// <summary>
        /// The path to this directory.
        /// </summary>
        string Path { get; set; }

    }

}
