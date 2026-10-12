using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMToolbarViewModel : GuiViewModelBase<DMToolbarViewModel, GuiPayloadBase>,
        IGuiRefreshable<DMPresenceRefreshEvent>
    {
        // The client sometimes re-centers a window placed at exactly (0, 0), so the toolbar sits one pixel in.
        public const float ToolbarX = 1f;
        public const float ToolbarY = 1f;
        // The client keeps at least 720 UI pixels of height, so a 1280x1024 screen at its highest UI scale is
        // about 914 wide. The admin toolbar must fit there without covering the portrait.
        public const float ButtonWidth = 84f;
        // Measured in the client: each button takes 4 pixels of margin and the window about 14 of padding per side.
        public const float ButtonSlotWidth = ButtonWidth + 4f;
        // Every DM sees these buttons; admins also get the Admin button at the end.
        public const int PublicButtonCount = 8;
        public const float ToolbarWidth = PublicButtonCount * ButtonSlotWidth + 28f;
        public const float AdminToolbarWidth = ToolbarWidth + ButtonSlotWidth;
        public const float ToolbarHeight = 52f;

        public bool IsAdmin
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string AppearText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AppearTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ShowAdminButton(DMAdminToolsViewModel.CanUse(Player));
            RefreshAppearText();
        }

        /// <summary>
        /// Shows or hides the Admin button and sizes the toolbar to fit.
        /// Hiding the button only affects the layout; the admin windows check permissions themselves.
        /// </summary>
        /// <param name="isAdmin">Whether to show the Admin button.</param>
        public void ShowAdminButton(bool isAdmin)
        {
            IsAdmin = isAdmin;
            // The toolbar replaces the native DM taskbar, so it always sits in the top-left corner.
            // A hidden button still takes up its space, so the toolbar is cut short before it.
            Geometry = new GuiRectangle(ToolbarX, ToolbarY, isAdmin ? AdminToolbarWidth : ToolbarWidth, ToolbarHeight);
        }

        /// <summary>
        /// Moves a DM window below the toolbar when it would open on top of it.
        /// DM window positions are not saved, so the search windows otherwise open over the toolbar.
        /// </summary>
        /// <param name="player">The DM.</param>
        /// <param name="geometry">The window's current geometry.</param>
        public static GuiRectangle PlaceBelowToolbar(uint player, GuiRectangle geometry)
        {
            // Window positions are physical pixels while sizes scale with the UI scale.
            var scale = GetPlayerDeviceProperty(player, PlayerDevicePropertyType.GuiScale) / 100f;
            if (scale <= 0f)
                scale = 1f;

            var toolbarBottom = ToolbarY + ToolbarHeight * scale + 4f;
            return geometry.Y >= toolbarBottom
                ? geometry
                : new GuiRectangle(geometry.X, toolbarBottom, geometry.Width, geometry.Height);
        }

        private void RefreshAppearText()
        {
            var isManifested = DMPresence.GetIsManifested(Player);
            AppearText = isManifested ? "Disappear" : "Appear";
            AppearTooltip = isManifested
                ? "Players can see you. Click to hide your avatar from players."
                : "Players cannot see you. Click to show your avatar to players.";
        }

        public Action OnClickChooser() => () =>
        {
            DMPresence.ToggleWindow(Player, GuiWindowType.DMChooser);
        };

        public Action OnClickCreator() => () =>
        {
            DMPresence.ToggleWindow(Player, GuiWindowType.DMCreator);
        };

        public Action OnClickPlayers() => () =>
        {
            DMPresence.ToggleWindow(Player, GuiWindowType.DMPlayerSearch);
        };

        public Action OnClickCreatureManager() => () =>
        {
            DMPresence.ToggleWindow(Player, GuiWindowType.CreatureManager);
        };

        public Action OnClickPlaceableTools() => () =>
        {
            DMPresence.ToggleWindow(Player, GuiWindowType.DMTools);
        };

        public Action OnClickAreaNotes() => () =>
        {
            DMPresence.ToggleWindow(Player, GuiWindowType.AreaNotes);
        };

        public Action OnClickMusic() => () =>
        {
            DMPresence.ToggleWindow(Player, GuiWindowType.MusicPicker, new MusicPickerPayload(MusicPickerMode.DungeonMaster));
        };

        public Action OnClickAdmin() => () =>
        {
            if (DMAdminToolsViewModel.CanUse(Player))
                DMPresence.ToggleWindow(Player, GuiWindowType.DMAdminTools);
        };

        public Action OnClickToggleAppear() => () =>
        {
            DMPresence.SetIsManifested(Player, !DMPresence.GetIsManifested(Player));
            RefreshAppearText();
        };

        public void Refresh(DMPresenceRefreshEvent payload)
        {
            RefreshAppearText();
        }
    }
}
