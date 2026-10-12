using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMToolbarMoreViewModel : GuiViewModelBase<DMToolbarMoreViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmtoolbarmore_content";
        public const string MainContentPartial = "DMTOOLBARMORE_MAIN";

        public bool IsAdmin
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            // Staff are matched by CD key; a character without one cannot be an admin.
            IsAdmin = !string.IsNullOrEmpty(GetPCPublicCDKey(Player)) &&
                      Authorization.GetAuthorizationLevel(Player).HasFlag(AuthorizationLevel.Admin);
            ChangePartialView(MainContentElement, MainContentPartial);
        }

        public Action OnClickAnimations() => () =>
        {
            Gui.TogglePlayerWindow(AnimationPreviewCatalog.GetController(Player), GuiWindowType.AnimationDebug, uiTarget: Player);
        };

        public Action OnClickManageStaff() => () =>
        {
            if (IsAdmin)
                Gui.TogglePlayerWindow(Player, GuiWindowType.ManageStaff);
        };

        public Action OnClickManageBans() => () =>
        {
            if (IsAdmin)
                Gui.TogglePlayerWindow(Player, GuiWindowType.ManageBans);
        };

        public Action OnClickPropertyDiagnostics() => () =>
        {
            if (IsAdmin)
                Gui.TogglePlayerWindow(Player, GuiWindowType.PropertyDiagnostics);
        };
    }
}
