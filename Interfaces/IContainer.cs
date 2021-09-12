/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Collections.Generic;

namespace K2host.Vfs.Interface
{

    /// <summary>
    /// This is used to help create the object class you define.
    /// </summary>
    public interface IContainer : IDisposable
    {

        /// <summary>
        /// Container for Files within this folder.
        /// </summary>
        IFile[] Files { get; set; }

        /// <summary>
        /// Container for Folders within this folder
        /// </summary>
        IFolder[] Directories { get; set; }

    }

}
