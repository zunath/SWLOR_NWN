using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ChatCommand
{
    /// <summary>
    /// This attribute should exist on all chat commands.
    /// </summary>
    public class CommandDetailsAttribute: Attribute
    {
        /// <summary>
        /// The explanation of what the chat command does. Used when a player enters the /help chat command
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// All users who are able to use the chat command.
        /// </summary>
        public CommandPermissionType Permissions { get; set; }

        public CommandDetailsAttribute(string description, CommandPermissionType permissions)
        {
            Description = description;
            Permissions = permissions;
        }

    }
}
