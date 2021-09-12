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
    public interface IHeader : IContainer, IDisposable
    {
        
        /// <summary>
        /// The header version.
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// The key used for password encryption.
        /// </summary>
        string SystemKey { get; set; }

        /// <summary>
        /// The root name of the device
        /// </summary>
        string RootName { get; set; }

        /// <summary>
        /// Container for empty clusters
        /// </summary>
        ICluster[] EmptyClusters { get; set; }

        /// <summary>
        /// Container for paths to deleted entries
        /// </summary>
        string[] RecycleBin { get; set; }

        /// <summary>
        /// Users requirements and that can login to device
        /// </summary>
        IUserRequirements[] Requirements { get; set; }

        /// <summary>
        /// The base64 string of the cert pfx file to encrypt this device.
        /// </summary>
        string Certificate { get; set; }

        /// <summary>
        /// The cert pfx password to encrypt this device.
        /// </summary>
        string CertificatePassword { get; set; }

    }

}
