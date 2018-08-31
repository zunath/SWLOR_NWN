using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ChatCommand
{
    public class CommandDetailsAttribute: Attribute
    {
        public string Description { get; set; }
        public CommandPermissionType Permissions { get; set; }

        public CommandDetailsAttribute(string description, CommandPermissionType permissions)
        {
            Description = description;
            Permissions = permissions;
        }

    }
}
