using System;

namespace SWLOR.Game.Server.Enumeration
{
    [Flags]
    public enum CommandPermissionType
    {
        Player = 1,
        DM = 2
    }
}
