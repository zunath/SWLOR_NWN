using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class AdminChatCommand: IChatCommandListDefinition
    {
        private readonly IGuiService _guiService = ServiceContainer.GetService<IGuiService>();
        private readonly ChatCommandBuilder _builder = new ();

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
