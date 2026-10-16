using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            // Settings apply to whatever the DM controls, so resolve it from the avatar on every change.
            _dm = DMPresence.GetDM(Player);
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            LoadSettings();

            WatchOnClient(model => model.EmoteStyleIndex);
            WatchOnClient(model => model.CommsFilterIndex);
            WatchOnClient(model => model.ShowHelmet);
            WatchOnClient(model => model.ShowCloak);
            ChangePartialView(MainContentElement, MainContentPartial);
        }

        private void LoadSettings()
        {
            var controlled = DMPresence.GetControlledCreature(_dm);
            var preferences = DMPreference.Get(controlled);
            EmoteStyleIndex = preferences.EmoteStyle == EmoteStyle.Novel ? NovelEmoteIndex : RegularEmoteIndex;
            CommsFilterIndex = preferences.CommsFilter == DMCommsFilter.OwnParty ? OwnPartyIndex : AllPartiesIndex;

            var (showHelmet, showCloak) = ArmorDisplay.GetDMArmorDisplay(controlled);
            ShowHelmet = showHelmet;
            ShowCloak = showCloak;
            ArmorDisplayText = GetIsDMPossessed(controlled)
                ? "Helmet and Cloak: possessed creature"
                : "Helmet and Cloak: your avatar";
        }

        protected override void OnClientPropertyUpdated(string propertyName)
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
                    ArmorDisplay.SetDMArmorDisplay(controlled, ShowHelmet, ShowCloak);
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
