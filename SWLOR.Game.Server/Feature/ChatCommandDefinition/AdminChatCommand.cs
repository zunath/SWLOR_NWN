﻿using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class AdminChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ManageStaffCommand();

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

    }
}
