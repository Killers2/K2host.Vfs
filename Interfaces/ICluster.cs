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
    public interface ICluster : IDisposable
    {

        /// <summary>
        /// The start index of empty data.
        /// </summary>
        long StartIndex { get; set; }

        /// <summary>
        /// The size of the empty data.
        /// </summary>
        long Length { get; set; }

    }

}
