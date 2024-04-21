using SWLOR.Core.Enumeration;
using SWLOR.Core.Service;
using SWLOR.Core.Service.ChatCommandService;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.ChatCommandDefinition
{
    public class AdminChatCommand: IChatCommandListDefinition
    {
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
                    Gui.TogglePlayerWindow(user, GuiWindowType.ManageStaff);
                });
        }

        private void ManageBansCommand()
        {
            _builder.Create("managebans")
                .Description("Toggles the manage bans window to add/remove banned players.")
                .Permissions(AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.ManageBans);
                });
        }

    }
}
