using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMAdminToolsViewModel : GuiViewModelBase<DMAdminToolsViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmadmintools_content";
        public const string MainContentPartial = "DMADMINTOOLS_MAIN";

        /// <summary>
        /// Returns whether a player may use the admin tools.
        /// </summary>
        /// <param name="player">The player to check.</param>
        public static bool CanUse(uint player)
        {
            player = DMPresence.GetDM(player);
            // Staff are matched by CD key; a character without one cannot be an admin.
            return !string.IsNullOrEmpty(GetPCPublicCDKey(player)) &&
                   Authorization.GetAuthorizationLevel(player).HasFlag(AuthorizationLevel.Admin);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            ChangePartialView(MainContentElement, MainContentPartial);
        }

        public Action OnClickManageStaff() => () =>
        {
            if (CanUse(Player))
                DMPresence.ToggleWindow(Player, GuiWindowType.ManageStaff);
        };

        public Action OnClickManageBans() => () =>
        {
            if (CanUse(Player))
                DMPresence.ToggleWindow(Player, GuiWindowType.ManageBans);
        };

        public Action OnClickPropertyDiagnostics() => () =>
        {
            if (CanUse(Player))
                DMPresence.ToggleWindow(Player, GuiWindowType.PropertyDiagnostics);
        };
    }
}
