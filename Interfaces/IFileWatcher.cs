/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using K2host.Vfs.Delegates;
using System;
using System.Collections.Generic;

namespace K2host.Vfs.Interface
{

    /// <summary>
    /// This is used to help create the object class you define.
    /// </summary>
    public interface IFileWatcher : IDisposable
    {

        /// <summary>
        /// The event called when a file has been renamed.
        /// </summary>
        OnRenamedEvent OnRenamed { get; set; }

        /// <summary>
        /// The event called when a file has been created.
        /// </summary>
        OnCreatedEvent OnCreated { get; set; }

        /// <summary>
        /// The event called when a file has been removed.
        /// </summary>
        OnRemovedEvent OnRemoved { get; set; }

        /// <summary>
        /// The file listed before init.
        /// </summary>
        Dictionary<string, IFile> Files { get; set; }

        /// <summary>
        /// The directory we are watching.
        /// </summary>
        IContainer Directory { get; set; }

        /// <summary>
        /// The server host.
        /// </summary>
        IServer Server { get; set; }

        /// <summary>
        /// The interupt operation to break out of threads..
        /// </summary>
        bool Cancel { get; set; }

    }

}
