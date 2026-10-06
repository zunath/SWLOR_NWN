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
            _builder.Create("cockpit").Description("Opens ship banks and prepared operating techniques.").Permissions(AuthorizationLevel.All)
                .Action((user,target,location,args)=>
                {
                    if (!Space.IsPlayerInSpaceMode(user)){SendMessageToPC(user,"Use Operations in ship management while docked.");return;}
                    var player=DB.Get<Entity.Player>(GetObjectUUID(user));
                    Gui.TogglePlayerWindow(user,Service.GuiService.GuiWindowType.ShipCockpit,new Feature.GuiDefinition.Payload.ShipCockpitPayload(player.ActiveShipId));
                });

            return _builder.Build();
        }

        private void ExitSpaceCommand()
        {
            _builder.Create("exit")
                .Description("Exits the pilot seat when controlling a starship.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
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
