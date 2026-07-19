using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Opens the "HP Tracker" NUI window, a temporary, narrative HP tracker listing tracked creatures near
    /// the viewer. All tracker management (add, adjust, remove) happens inside the window itself; nothing
    /// here ever affects a creature's real combat hit points.
    /// </summary>
    public class HPTrackerChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            OpenWindow();

            return _builder.Build();
        }

        private void OpenWindow()
        {
            _builder.Create("hptracker")
                .Description("Opens the HP Tracker window, listing creatures with an HP tracker near you.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, _, _, _) =>
                {
                    // Standard window-open pattern (mirrors /dice, /dmtools). A regular PC opens it directly.
                    // A DM must be possessing a creature: the window then renders on that creature's client
                    // (uiTarget) while its state lives under the master DM. A bare, unpossessed DM avatar
                    // cannot host a NUI window, so nothing opens for it — same as every SWLOR NUI window.
                    var player = user;
                    var uiTarget = OBJECT_INVALID;
                    if (GetIsDMPossessed(player))
                    {
                        uiTarget = player;
                        player = GetMaster(player);
                    }

                    // A bare (unpossessed) DM avatar can't host a NUI window (this is the same guard
                    // TogglePlayerWindow uses). Explain how to proceed instead of silently doing nothing.
                    if (!GetIsPC(player) && uiTarget == OBJECT_INVALID)
                    {
                        SendMessageToPC(user, ColorToken.Red("The HP Tracker window can't open for an unpossessed DM. Possess a creature first, or use a player character."));
                        return;
                    }

                    Gui.TogglePlayerWindow(player, GuiWindowType.HPTracker, null, OBJECT_INVALID, uiTarget);
                });
        }
    }
}
