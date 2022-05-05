using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SpatialChatCommand: IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("coord")
                .Description("Displays your current coordinates in the area.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var position = GetPosition(user);
                    var cellX = (int)(position.X / 10);
                    var cellY = (int)(position.Y / 10);

                    SendMessageToPC(user, $"Current Area Coordinates: ({cellX}, {cellY})");
                });

            builder.Create("pos")
                .Description("Displays your current position in the area.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var position = GetPosition(user);
                    SendMessageToPC(user, $"Current Position: ({position.X}, {position.Y}, {position.Z})");
                });

            builder.Create("time")
                .Description("Returns the current UTC server time.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var now = DateTime.UtcNow;
                    var nowText = now.ToString("yyyy-MM-dd HH:mm:ss");

                    SendMessageToPC(user, "Current Server Date: " + nowText);
                });

            return builder.Build();
        }
    }
}
