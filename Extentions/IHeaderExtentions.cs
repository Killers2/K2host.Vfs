/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System.IO;
using System.Text;
using System.Collections.Generic;

using Newtonsoft.Json;

using K2host.Core.JsonConverters;
using K2host.Vfs.Classes;
using K2host.Vfs.Interface;

namespace K2host.Vfs.Extentions
{
    public static class IHeaderExtentions
    {

        /// <summary>
        /// Converts the header to an array of bytes.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this IHeader e)
        { 

            return Encoding.UTF8
                    .GetBytes(JsonConvert.SerializeObject(e, new JsonSerializerSettings()
                    {
                        Converters = new List<JsonConverter>() {
                            { new InterfaceConverter<IFile,             OFile>() },
                            { new InterfaceConverter<IFolder,           OFolder>() },
                            { new InterfaceConverter<ICluster,          OCluster>() },
                            { new InterfaceConverter<IHeader,           OHeader>() },
                            { new InterfaceConverter<IUserRequirements, OUserRequirements>() }
                        },
                        Formatting = Formatting.None
                    }));

        }
        
        /// <summary>
        /// Converts a memory stream of bytes to a valid header.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IHeader GetHeader(this MemoryStream e)
        {

            IHeader output = JsonConvert.DeserializeObject<OHeader>(Encoding.UTF8.GetString(e.ToArray()), new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>() {
                        { new InterfaceConverter<IFile,             OFile>() },
                        { new InterfaceConverter<IFolder,           OFolder>() },
                        { new InterfaceConverter<ICluster,          OCluster>() },
                        { new InterfaceConverter<IHeader,           OHeader>() },
                        { new InterfaceConverter<IUserRequirements, OUserRequirements>() }
                    },
                Formatting = Formatting.None
            });

            e.Close();
            e.Dispose();

            return output;

        }


    }
}
