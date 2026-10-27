using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SpaceChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ExitSpaceCommand();

            return _builder.Build();
        }

        private void ExitSpaceCommand()
        {
            _builder.Create("exit")
                .Description("Exits the pilot seat when controlling a starship, or leaves a crew station or viewport.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    // Crew can leave their station at any time, even in a fight; the ship flies on without them.
                    if (Space.IsAtCrewStation(user))
                    {
                        Space.LeaveCrewStation(user, "You leave your station and return to the console.");
                        return;
                    }

                    if (!Space.IsPlayerInSpaceMode(user))
                    {
                        SendMessageToPC(user, "This command can only be used while piloting a starship.");
                        return;
                    }

                    if (Enmity.HasEnmity(user))
                    {
                        SendMessageToPC(user, "This command cannot be used while you're targeted.");
                        return;
                    }

                    Space.WarpPlayerInsideShip(user);
                });
        }
    }
}
