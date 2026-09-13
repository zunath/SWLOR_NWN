using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Staff-facing chat commands for the Masteries system (see MASTERY_SPEC.md, Phase 3).
    /// </summary>
    public class MasteryChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            MasteryReviewCommand();

            return _builder.Build();
        }

        private void MasteryReviewCommand()
        {
            _builder.Create("masteryreview")
                .Description("Toggles the mastery review queue, for approving/denying player mastery requests.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.MasteryReview);
                });
        }
    }
}
