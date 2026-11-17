using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.TimeZoneService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMSettingsViewModel : GuiViewModelBase<DMSettingsViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmsettings_content";
        public const string MainContentPartial = "DMSETTINGS_MAIN";
        public const int RegularEmoteIndex = 0;
        public const int NovelEmoteIndex = 1;
        public const int AllPartiesIndex = 0;
        public const int OwnPartyIndex = 1;

        private uint _dm;

        public int EmoteStyleIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public int CommsFilterIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool ShowHelmet
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowCloak
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ArmorDisplayText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int TimeZoneIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            // Settings apply to whatever the DM controls, so resolve it from the avatar on every change.
            _dm = DMPresence.GetDM(Player);
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            ChangePartialView(MainContentElement, MainContentPartial);
            LoadSettings();

            // Every change saves at once, and a control that missed its value reports its default (layout rule R7),
            // which would overwrite the saved choice. Send the values again and watch from the next tick.
            var windowToken = WindowToken;
            DelayCommand(0f, () => Listen(windowToken));
        }

        private void Listen(int windowToken)
        {
            if (WindowToken != windowToken || !Gui.IsWindowOpen(Player, WindowType))
                return;

            // Watching sends each value again, now that its control exists.
            LoadSettings();
            WatchOnClient(model => model.EmoteStyleIndex);
            WatchOnClient(model => model.CommsFilterIndex);
            WatchOnClient(model => model.ShowHelmet);
            WatchOnClient(model => model.ShowCloak);
            WatchOnClient(model => model.TimeZoneIndex);
        }

        private void LoadSettings()
        {
            var controlled = DMPresence.GetControlledCreature(_dm);
            var preferences = DMPreference.Get(controlled);
            EmoteStyleIndex = preferences.EmoteStyle == EmoteStyle.Novel ? NovelEmoteIndex : RegularEmoteIndex;
            CommsFilterIndex = preferences.CommsFilter == DMCommsFilter.OwnParty ? OwnPartyIndex : AllPartiesIndex;
            TimeZoneIndex = PlayerTimeZones.IndexOf(preferences.TimeZoneId);

            var (showHelmet, showCloak) = ArmorDisplay.GetDMArmorDisplay(controlled);
            ShowHelmet = showHelmet;
            ShowCloak = showCloak;
            ArmorDisplayText = GetIsDMPossessed(controlled)
                ? "Helmet and Cloak: possessed creature"
                : "Helmet and Cloak: your avatar";
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            SaveSetting(propertyName);
        }

        private void SaveSetting(string propertyName)
        {
            var controlled = DMPresence.GetControlledCreature(_dm);
            if (!GetIsDM(controlled) && !GetIsDMPossessed(controlled))
                return;

            var saved = true;
            switch (propertyName)
            {
                case nameof(EmoteStyleIndex):
                    var style = EmoteStyleIndex == NovelEmoteIndex ? EmoteStyle.Novel : EmoteStyle.Regular;
                    saved = Communication.SetEmoteStyle(controlled, style);
                    break;
                case nameof(CommsFilterIndex):
                    var filter = CommsFilterIndex == OwnPartyIndex ? DMCommsFilter.OwnParty : DMCommsFilter.AllParties;
                    saved = DMPreference.Update(controlled, preferences => preferences.CommsFilter = filter);
                    break;
                case nameof(ShowHelmet):
                case nameof(ShowCloak):
                    saved = ArmorDisplay.SetDMArmorDisplay(controlled, ShowHelmet, ShowCloak);
                    break;
                case nameof(TimeZoneIndex):
                    var timeZoneId = PlayerTimeZones.GetByIndex(TimeZoneIndex).Id;
                    saved = DMPreference.Update(controlled, preferences => preferences.TimeZoneId = timeZoneId);
                    if (saved)
                        HostedEvents.RefreshWindows(_dm);
                    break;
                default:
                    return;
            }

            if (!saved)
            {
                DMPresence.SendMessage(_dm, ColorToken.Red("Your DM settings could not be saved."));
                LoadSettings();
            }
        }
    }
}
