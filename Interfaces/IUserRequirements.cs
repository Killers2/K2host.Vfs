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
    public interface IUserRequirements : IDisposable
    {

        /// <summary>
        /// The size allocated to the user.
        /// </summary>
        long SpaceLimit { get; set; }

        /// <summary>
        /// The size of the space used.
        /// </summary>
        long SpaceUsed { get; set; }

        /// <summary>
        /// The user id linked via a type
        /// </summary>
        long UserId { get; set; }

    }

}
