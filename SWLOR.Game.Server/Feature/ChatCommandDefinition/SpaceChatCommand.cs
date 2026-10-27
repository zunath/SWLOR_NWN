using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SpaceChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ExitSpaceCommand();
            DockCommand();
            UndockCommand();
            ShipHudCommand();
            HoloNetCommand();

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

        private void DockCommand()
        {
            _builder.Create("dock")
                .Description("Docks your Small or Medium starship aboard the player's Large or Capital ship you target in space.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) => Space.DockWithTarget(user));
        }

        private void UndockCommand()
        {
            _builder.Create("undock")
                .Description("Undocks your starship from the ship you stand aboard.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) => Space.UndockFromInterior(user));
        }

        private void ShipHudCommand()
        {
            _builder.Create("shiphud", "hud")
                .Description("Opens or closes the Ship HUD while piloting a starship.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) => SpaceHud.Toggle(user));
        }

        private void HoloNetCommand()
        {
            _builder.Create("holonet")
                .Description("Opens or closes the HoloNet price board: the best commodity prices across the exchanges, up to an hour old.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) => Gui.TogglePlayerWindow(user, GuiWindowType.HoloNetBoard));
        }
    }
}
