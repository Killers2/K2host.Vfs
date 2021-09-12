/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;

using K2host.Vfs.Interface;

namespace K2host.Vfs.Classes
{
   
    public class OCluster : ICluster
    {

        /// <summary>
        /// The start index of empty data.
        /// </summary>
        public long StartIndex { get; set; }

        /// <summary>
        /// The size of the empty data.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// The constructor
        /// </summary>
        public OCluster()
        {

        }
       
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OCluster(long startIndex, long length)
        {
            StartIndex  = startIndex;
            Length      = length;
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
