/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using K2host.Core;
using K2host.Threading.Classes;
using K2host.Threading.Extentions;

using K2host.Vfs.Interface;

namespace K2host.Vfs.Extentions
{

    public static class IFileWatcherExtentions
    {

        /// <summary>
        /// Starts the watch process on a directory
        /// </summary>
        /// <param name="e"></param>
        public static void Start(this IFileWatcher e)
        {

            e.Server.ThreadManager.Add(
                new OThread(
                    new ThreadStart(() => {
                        try
                        {
                            while (!e.Cancel)
                            {

                                string fullPath = string.Empty;

                                //Check the list for any renamed files from the original watch list.
                                if (e.Directory.Files.Length == e.Files.Count) {
                                    Dictionary<string, IFile> amendments = new();
                                    foreach (string originalPath in e.Files.Keys) {
                                        IFile fi = e.Files[originalPath];
                                        if (fi.FullPath != originalPath) {
                                            amendments.Add(originalPath, fi);
                                            e.OnRenamed?.Invoke(e, originalPath, fi.FullPath);
                                        }
                                    }
                                    amendments.ForEach(fi => { e.Files.ChangeKey(fi.Key, fi.Value.FullPath); });
                                    amendments.Clear();
                                }

                                //Check the list for any added files from the original watch list.
                                if (e.Directory.Files.Length > e.Files.Count) {
                                    foreach (string item in e.Directory.Files.Select(f => f.FullPath).ToArray()) {
                                        if (!e.Files.ContainsKey(item)) {
                                            fullPath = item;
                                            break;
                                        }
                                    }
                                    e.Files.Add(fullPath, e.Directory.Files.Where(f => f.FullPath == fullPath).FirstOrDefault());
                                    e.OnCreated?.Invoke(e, fullPath);
                                }

                                //Check the list for any removed files from the original watch list.
                                if (e.Directory.Files.Length < e.Files.Count) {
                                    foreach (string item in e.Files.Keys) {
                                        if (!e.Directory.Files.Where(f => f.FullPath == item).Any()) {
                                            fullPath = item;
                                            break;
                                        }
                                    }
                                    e.Files.Remove(fullPath);
                                    e.OnRemoved?.Invoke(e, fullPath);
                                }

                                Thread.Sleep(500);
                            }
                        }
                        catch { }
                    }))).Start();
            
        }

        /// <summary>
        /// This will cancel the operation and stop the thread.
        /// </summary>
        /// <param name="e"></param>
        public static void Close(this IFileWatcher e)
        {
            e.Cancel = true;
        }


    }

}
