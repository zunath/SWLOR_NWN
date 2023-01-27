using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SpatialChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Coordinates();
            Position();
            Time();

            return _builder.Build();
        }

        private void Coordinates()
        {
            _builder.Create("coord")
                .Description("Displays your current coordinates in the area.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var position = GetPosition(user);
                    var cellX = (int)(position.X / 10);
                    var cellY = (int)(position.Y / 10);

                    SendMessageToPC(user, $"Current Area Coordinates: ({cellX}, {cellY})");
                });
        }

        private void Position()
        {
            _builder.Create("pos")
                .Description("Displays your current position in the area.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var position = GetPosition(user);
                    SendMessageToPC(user, $"Current Position: ({position.X}, {position.Y}, {position.Z})");
                });
        }

        private void Time()
        {
            _builder.Create("time")
                .Description("Returns the current UTC server time and the in-game time.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var now = DateTime.UtcNow;
                    var nowText = now.ToString("yyyy-MM-dd HH:mm:ss");
                    var gameTime = GetTimeHour().ToString().PadLeft(2, '0') + ":" +
                                   GetTimeMinute().ToString().PadLeft(2, '0') + ":" +
                                   GetTimeSecond().ToString().PadLeft(2, '0');

                    SendMessageToPC(user, $"Current Server Date: {nowText}");
                    SendMessageToPC(user, $"Current World Time: {gameTime}");
                });

        }
    }
}
