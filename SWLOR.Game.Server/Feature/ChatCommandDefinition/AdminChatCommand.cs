using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class AdminChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ManageStaffCommand();
            ManageBansCommand();
            PropertyDiagnosticsCommand();

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

        private void PropertyDiagnosticsCommand()
        {
            _builder.Create("propertydiagnostics")
                .Description("Toggles the property loading diagnostics window.")
                .Permissions(AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    Log.WriteStructured(
                        LogGroup.Property,
                        "Property diagnostics opened: PlayerName={PlayerName} PlayerId={PlayerId}",
                        GetName(user),
                        GetObjectUUID(user));
                    Gui.TogglePlayerWindow(user, GuiWindowType.PropertyDiagnostics);
                });
        }

    }
}
