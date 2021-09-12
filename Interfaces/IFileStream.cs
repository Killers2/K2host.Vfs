/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.IO;

namespace K2host.Vfs.Interface
{

    /// <summary>
    /// This is used to help create the object class you define.
    /// </summary>
    public interface IFileStream : IDisposable
    {

        /// <summary>
        /// The encrypted status based on the server.
        /// </summary>
        bool IsEncrypted { get; }

        /// <summary>
        /// The start index of empty data.
        /// </summary>
        long Position { get; set; }

        /// <summary>
        /// The length of the stream.
        /// </summary>
        long Length { get; set; }

        /// <summary>
        /// The <see cref="IFile"/> object to read.
        /// </summary>
        IFile File { get; set; }

        /// <summary>
        /// The data partition to read from
        /// </summary>
        FileStream DataPartition { get; }

        /// <summary>
        /// The server with the mounted partition.
        /// </summary>
        IServer Server { get; }
       
        /// <summary>
        /// The status of the stream.
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// The status of the stream.
        /// </summary>
        bool CanWrite { get; }

    }

}
