using SWLOR.Component.Player.Enums;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Player.UI.ViewModel
{
    public class SettingsViewModel: GuiViewModelBase<SettingsViewModel, GuiPayloadBase>
    {
        private readonly IDatabaseService _db;
        private readonly ISkillService _skillService;
        private readonly ILanguageService _languageService;

        public SettingsViewModel(IGuiService guiService, IDatabaseService db, ISkillService skillService, ILanguageService languageService) : base(guiService)
        {
            _db = db;
            _skillService = skillService;
            _languageService = languageService;
        }
        
        public const string SettingsView = "SETTINGS_VIEW";

        public const string GeneralPartial = "GENERAL_VIEW";
        public const string ChatPartial = "CHAT_VIEW";

        private const int NumberOfSystemColors = 2; // OOC, Emotes

        public bool DisplayAchievementNotification
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool DisplayHolonetChannel
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool SubdualMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShareLightsaberForceXP
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsForceSensitive
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool DisplayServerResetReminders
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsGeneralSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsChatSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int CurrentRed
        {
            get => Get<int>();
            set => Set(value);
        }

        public int CurrentGreen
        {
            get => Get<int>();
            set => Set(value);
        }

        public int CurrentBlue
        {
            get => Get<int>();
            set => Set(value);
        }

        private List<SkillType> _languages;
        private int SelectedIndex { get; set; }

        public GuiColor SelectedColor
        {
            get => Get<GuiColor>();
            set
            {
                Set(value);
                SetColor();
            }
        }

        public GuiBindingList<string> ChatColorNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> ChatColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ChatColorToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedIndex = -1;
            SelectedColor = new GuiColor(0, 0, 0);
            IsGeneralSelected = true;
            IsChatSelected = false;
            CurrentRed = 0;
            CurrentGreen = 0;
            CurrentBlue = 0;

            LoadGeneralView();

            ChangePartialView(SettingsView, GeneralPartial);

            WatchOnClient(model => model.DisplayAchievementNotification);
            WatchOnClient(model => model.DisplayHolonetChannel);
            WatchOnClient(model => model.SubdualMode);
            WatchOnClient(model => model.ShareLightsaberForceXP);
            WatchOnClient(model => model.DisplayServerResetReminders);
            WatchOnClient(model => model.SelectedColor);
        }

        private void LoadGeneralView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Shared.Domain.Entity.Player>(playerId);

            IsForceSensitive = dbPlayer.CharacterType == CharacterType.ForceSensitive;

            DisplayAchievementNotification = dbPlayer.Settings.DisplayAchievementNotification;
            DisplayHolonetChannel = dbPlayer.Settings.IsHolonetEnabled;
            SubdualMode = dbPlayer.Settings.IsSubdualModeEnabled;
            ShareLightsaberForceXP = dbPlayer.Settings.IsLightsaberForceShareEnabled;
            DisplayServerResetReminders = dbPlayer.Settings.DisplayServerResetReminders;
        }

        private void LoadChatView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Shared.Domain.Entity.Player>(playerId);
            var colorSettings = dbPlayer.Settings.LanguageChatColors;
            var languages = _skillService.GetActiveSkillsByCategory(SkillCategoryType.Languages);

            _languages = new List<SkillType>();
            var chatColorNames = new GuiBindingList<string>();
            var chatColors = new GuiBindingList<GuiColor>();
            var chatToggles = new GuiBindingList<bool>();

            // OOC color
            chatColorNames.Add("OOC");
            chatToggles.Add(false);

            if (dbPlayer.Settings.OOCChatColor == null)
            {
                chatColors.Add(new GuiColor(
                    CommunicationConstants.OOCChatColor.Item1,
                    CommunicationConstants.OOCChatColor.Item2,
                    CommunicationConstants.OOCChatColor.Item3));
            }
            else
            {
                chatColors.Add(new GuiColor(
                    dbPlayer.Settings.OOCChatColor.Red,
                    dbPlayer.Settings.OOCChatColor.Green,
                    dbPlayer.Settings.OOCChatColor.Blue));
            }


            // Emote color
            chatColorNames.Add("Emotes");
            chatToggles.Add(false);

            if (dbPlayer.Settings.EmoteChatColor == null)
            {
                chatColors.Add(new GuiColor(
                    CommunicationConstants.EmoteChatColor.Item1,
                    CommunicationConstants.EmoteChatColor.Item2,
                    CommunicationConstants.EmoteChatColor.Item3));
            }
            else
            {
                chatColors.Add(new GuiColor(
                    dbPlayer.Settings.EmoteChatColor.Red,
                    dbPlayer.Settings.EmoteChatColor.Green,
                    dbPlayer.Settings.EmoteChatColor.Blue));
            }

            // Language colors
            foreach (var (type, skill) in languages)
            {
                _languages.Add(type);
                chatColorNames.Add(skill.Name);
                chatToggles.Add(false);

                if (colorSettings != null &&
                    colorSettings.ContainsKey(type))
                {
                    var playerSetting = colorSettings[type];
                    chatColors.Add(new GuiColor(playerSetting.Red, playerSetting.Green, playerSetting.Blue));
                }
                else
                {
                    var (red, green, blue) = _languageService.GetColor(type);
                    chatColors.Add(new GuiColor(red, green, blue));
                }
            }

            ChatColorNames = chatColorNames;
            ChatColors = chatColors;
            ChatColorToggles = chatToggles;
        }

        private void LoadColor()
        {
            if (SelectedIndex < 0)
                return;

            SelectedColor = ChatColors[SelectedIndex];
        }

        private void SetColor()
        {
            if (SelectedIndex < 0)
                return;

            ChatColors[SelectedIndex] = SelectedColor;
            CurrentRed = SelectedColor.R;
            CurrentGreen = SelectedColor.G;
            CurrentBlue = SelectedColor.B;
        }

        public Action OnSave() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Shared.Domain.Entity.Player>(playerId);

            dbPlayer.Settings.DisplayAchievementNotification = DisplayAchievementNotification;
            dbPlayer.Settings.IsHolonetEnabled = DisplayHolonetChannel;
            dbPlayer.Settings.IsSubdualModeEnabled = SubdualMode;
            dbPlayer.Settings.IsLightsaberForceShareEnabled = ShareLightsaberForceXP;
            dbPlayer.Settings.DisplayServerResetReminders = DisplayServerResetReminders;

            // System Colors - OOC
            var systemColor = ChatColors[0];
            dbPlayer.Settings.OOCChatColor = new PlayerColor(systemColor.R, systemColor.G, systemColor.B);

            // System Colors - Emote
            systemColor = ChatColors[1];
            dbPlayer.Settings.EmoteChatColor = new PlayerColor(systemColor.R, systemColor.G, systemColor.B);

            if (dbPlayer.Settings.LanguageChatColors == null)
                dbPlayer.Settings.LanguageChatColors = new Dictionary<SkillType, PlayerColor>();

            for (var index = NumberOfSystemColors; index < ChatColors.Count; index++)
            {
                var type = _languages[index - NumberOfSystemColors];
                var color = ChatColors[index];
                dbPlayer.Settings.LanguageChatColors[type] = new PlayerColor(color.R, color.G, color.B);
            }

            _db.Set(dbPlayer);

            _guiService.TogglePlayerWindow(Player, GuiWindowType.Settings);

            // Post-save actions
            UpdateHolonetSetting();

            SendMessageToPC(Player, ColorToken.Green("Settings updated."));
        };

        public Action OnCancel() => () =>
        {
            _guiService.TogglePlayerWindow(Player, GuiWindowType.Settings);
        };

        public Action OnClickChangeDescription() => () =>
        {
            _guiService.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
        };

        private void UpdateHolonetSetting()
        {
            SetLocalBool(Player, "DISPLAY_HOLONET", DisplayHolonetChannel);
        }

        public Action OnClickGeneral() => () =>
        {
            IsGeneralSelected = true;
            IsChatSelected = false;
            ChangePartialView(SettingsView, GeneralPartial);
            LoadGeneralView();
        };

        public Action OnClickChat() => () =>
        {
            IsGeneralSelected = false;
            IsChatSelected = true;
            ChangePartialView(SettingsView, ChatPartial);
            LoadChatView();
        };

        public Action OnClickSelectChat() => () =>
        {
            if (SelectedIndex > -1)
                ChatColorToggles[SelectedIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedIndex = index;
            ChatColorToggles[SelectedIndex] = true;

            LoadColor();
        };

        public Action OnClickResetColor() => () =>
        {
            var index = NuiGetEventArrayIndex();

            ShowModal("Are you sure you want to reset this color to the default?", () =>
            {
                if (index == 0) // OOC
                {
                    ChatColors[index] = new GuiColor(
                        CommunicationConstants.OOCChatColor.Item1,
                        CommunicationConstants.OOCChatColor.Item2,
                        CommunicationConstants.OOCChatColor.Item3);
                }
                else if (index == 1) // Emotes
                {
                    ChatColors[index] = new GuiColor(
                        CommunicationConstants.EmoteChatColor.Item1,
                        CommunicationConstants.EmoteChatColor.Item2,
                        CommunicationConstants.EmoteChatColor.Item3);
                }
                else
                {
                    var type = _languages[index - NumberOfSystemColors];
                    var (red, green, blue) = _languageService.GetColor(type);
                    ChatColors[index] = new GuiColor(red, green, blue);
                }

                ChangePartialView(SettingsView, ChatPartial);
            });
        };
    }
}
