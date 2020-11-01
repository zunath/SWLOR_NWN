
using System;

namespace SWLOR.Game.Server.Legacy.Enumeration
{
    [Flags]
    public enum CommandPermissionType
    {
        Player = 1,
        DM = 2,
        Admin = 4
    }
}
