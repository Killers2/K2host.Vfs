/*
' /====================================================\
'| Developed Tony N. Hyde (www.k2host.co.uk)            |
'| Projected Started: 2018-11-20                        | 
'| Use: General                                         |
' \====================================================/
*/
using System;

namespace K2host.Vfs.Enums
{
    [Flags]
    public enum ODiskFlags
    {
        READ        = 1,
        READWRITE   = 2,
        WRITE       = 4,
        HIDDEN      = 8,
        SYSTEM      = 16,
        DIRECTORY   = 32,
        FILE        = 64,
        DELETED     = 128
    }

}
