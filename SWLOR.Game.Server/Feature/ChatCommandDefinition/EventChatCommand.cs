using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class EventChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Events();
            LeaveEvent();

            return _builder.Build();
        }

        private void Events()
        {
            _builder.Create("events")
                .Description("Shows running and upcoming DM events, where you can join one. DMs schedule and run events here.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    if (GetIsDM(user) || GetIsDMPossessed(user))
                        DMPresence.ToggleWindow(user, GuiWindowType.DMEvents);
                    else
                        Gui.TogglePlayerWindow(user, GuiWindowType.Events);
                });
        }

        private void LeaveEvent()
        {
            _builder.Create("leaveevent")
                .Description("Leaves the DM event you joined and sends you back to where you joined from.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var error = HostedEvents.Leave(user);
                    SendMessageToPC(user, string.IsNullOrEmpty(error)
                        ? ColorToken.Green("Going back to where you joined from...")
                        : ColorToken.Red(error));
                });
        }
    }
}
