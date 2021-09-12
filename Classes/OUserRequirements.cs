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
   
    public class OUserRequirements : IUserRequirements
    {

        /// <summary>
        /// The size allocated to the user.
        /// </summary>
        public long SpaceLimit { get; set; }

        /// <summary>
        /// The size of the space used.
        /// </summary>
        public long SpaceUsed { get; set; }

        /// <summary>
        /// The user attached
        /// </summary>
        public long UserId { get; set; }
       
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OUserRequirements()
        {
            SpaceLimit  = 0;
            SpaceUsed   = 0;
            UserId      = 0;
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
