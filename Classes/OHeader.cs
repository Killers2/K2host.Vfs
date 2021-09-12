/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;
using System.IO;
using System.Linq;
using System.Text;

using K2host.Vfs.Interface;

using gl = K2host.Core.OHelpers;

namespace K2host.Vfs.Classes
{
   
    public class OHeader : IHeader
    {

        /// <summary>
        /// The header version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The key used for password encryption.
        /// </summary>
        public string SystemKey { get; set; }

        /// <summary>
        /// The root name of the device
        /// </summary>
        public string RootName { get; set; }

        /// <summary>
        /// Container for Files within this folder.
        /// </summary>
        public IFile[] Files { get; set; }

        /// <summary>
        /// Container for Folders within this folder
        /// </summary>
        public IFolder[] Directories { get; set; }

        /// <summary>
        /// Container for empty clusters
        /// </summary>
        public ICluster[] EmptyClusters { get; set; }

        /// <summary>
        /// Container for paths to deleted entries
        /// </summary>
        public string[] RecycleBin { get; set; }

        /// <summary>
        /// Users that can login to device
        /// </summary>
        public IUserRequirements[] Requirements { get; set; }

        /// <summary>
        /// The base64 string of the cert pfx file to encrypt this device.
        /// </summary>
        public string Certificate { get; set; }

        /// <summary>
        /// The cert pfx password to encrypt this device.
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OHeader()
        {
            Files           = Array.Empty<IFile>();
            Directories     = Array.Empty<IFolder>();
            EmptyClusters   = Array.Empty<ICluster>();
            RecycleBin      = Array.Empty<string>();
            Requirements    = Array.Empty<IUserRequirements>();
            Certificate     = string.Empty;
            SystemKey       = string.Empty;
        }
       
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OHeader(IUserRequirements e, string systemkey, string rootAlias = "root")
            :this()
        {
            RootName        = rootAlias;
            SystemKey       = systemkey;
            Requirements    = Requirements.Append(e).ToArray();
        }
        
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="e"></param>
        public OHeader(IUserRequirements e, string systemkey, string rootAlias = "root", string certificatePath = "", string certificatePassword = "")
            : this()
        {
            RootName        = rootAlias;
            SystemKey       = systemkey;
            Requirements    = Requirements.Append(e).ToArray();

            if (!string.IsNullOrEmpty(certificatePath) && File.Exists(certificatePath))
            {
                Certificate         = Convert.ToBase64String(File.ReadAllBytes(certificatePath));
                CertificatePassword = gl.EncryptAes(certificatePassword, systemkey, Encoding.UTF8.GetBytes(systemkey));
            }

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
