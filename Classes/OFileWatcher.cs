/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Collections.Generic;
using K2host.Vfs.Delegates;
using K2host.Vfs.Interface;

namespace K2host.Vfs.Classes
{
   
    public class OFileWatcher : IFileWatcher
    {
        
        /// <summary>
        /// The event called when a file has been renamed.
        /// </summary>
        public OnRenamedEvent OnRenamed { get; set; }

        /// <summary>
        /// The event called when a file has been created.
        /// </summary>
        public OnCreatedEvent OnCreated { get; set; }

        /// <summary>
        /// The event called when a file has been removed.
        /// </summary>
        public OnRemovedEvent OnRemoved { get; set; }

        /// <summary>
        /// The file names listed before init.
        /// </summary>
        public Dictionary<string, IFile> Files { get; set; }

        /// <summary>
        /// The directory we are watching.
        /// </summary>
        public IContainer Directory { get; set; }

        /// <summary>
        /// The server host.
        /// </summary>
        public IServer Server { get; set; }

        /// <summary>
        /// The interupt operation to break out of threads..
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// The constructor
        /// </summary>
        public OFileWatcher()
        {
            Files   = new ();
            Cancel  = false;
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
