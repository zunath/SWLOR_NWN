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
        public const float ButtonWidth = 96f;
        public const int ButtonCount = 9;
        // Each button carries about 8 pixels of margin; the window adds about 16 of padding.
        public const float ToolbarWidth = ButtonCount * (ButtonWidth + 8f) + 16f;
        public const float ToolbarHeight = 52f;

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
            // The toolbar replaces the native DM taskbar, so it always sits in the top-left corner.
            Geometry = new GuiRectangle(ToolbarX, ToolbarY, ToolbarWidth, ToolbarHeight);
            RefreshAppearText();
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
            Gui.TogglePlayerWindow(Player, GuiWindowType.DMChooser);
        };

        public Action OnClickCreator() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.DMCreator);
        };

        public Action OnClickPlayers() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.DMPlayerSearch);
        };

        public Action OnClickCreatureManager() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.CreatureManager);
        };

        public Action OnClickPlaceableTools() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.DMTools);
        };

        public Action OnClickAreaNotes() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.AreaNotes);
        };

        public Action OnClickMusic() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.MusicPicker, new MusicPickerPayload(MusicPickerMode.DungeonMaster));
        };

        public Action OnClickMore() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.DMToolbarMore);
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
