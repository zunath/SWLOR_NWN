using System.Collections.Generic;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class AdminChatCommand: IChatCommandListDefinition
    {
        private readonly IGuiService _guiService;
        private readonly ChatCommandBuilder _builder = new ();

        public AdminChatCommand(IGuiService guiService)
        {
            _guiService = guiService;
        }

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ManageStaffCommand();
            ManageBansCommand();

            return _builder.Build();
        }

        private void ManageStaffCommand()
        {
            _builder.Create("managestaff")
                .Description("Toggles the manage staff window to add/remove staff members.")
                .Permissions(AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    _guiService.TogglePlayerWindow(user, GuiWindowType.ManageStaff);
                });
        }

        private void ManageBansCommand()
        {
            _builder.Create("managebans")
                .Description("Toggles the manage bans window to add/remove banned players.")
                .Permissions(AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    _guiService.TogglePlayerWindow(user, GuiWindowType.ManageBans);
                });
        }

    }
}
