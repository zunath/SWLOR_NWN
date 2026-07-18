using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SettingsViewModel: GuiViewModelBase<SettingsViewModel, GuiPayloadBase>
    {
        public const string SettingsView = "SETTINGS_VIEW";

        public const string GeneralPartial = "GENERAL_VIEW";
        public const string IdentityPartial = "IDENTITY_VIEW";
        public const string ChatPartial = "CHAT_VIEW";

        private const int NumberOfSystemColors = 2; // OOC, Emotes

        public bool DisplayAchievementNotification
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool SubdualMode
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

        public bool PortraitVitals
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowDescriptorsForNamedPlayers
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowOwnDescriptor
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ScrambleAccountName
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsGeneralSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsIdentitySelected
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
            IsIdentitySelected = false;
            IsChatSelected = false;
            CurrentRed = 0;
            CurrentGreen = 0;
            CurrentBlue = 0;

            LoadGeneralView();
            LoadIdentityView();

            ChangePartialView(SettingsView, GeneralPartial);

            WatchOnClient(model => model.DisplayAchievementNotification);
            WatchOnClient(model => model.SubdualMode);
            WatchOnClient(model => model.DisplayServerResetReminders);
            WatchOnClient(model => model.PortraitVitals);
            WatchOnClient(model => model.ShowDescriptorsForNamedPlayers);
            WatchOnClient(model => model.ShowOwnDescriptor);
            WatchOnClient(model => model.ScrambleAccountName);
            WatchOnClient(model => model.SelectedColor);
        }

        private void LoadGeneralView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            IsForceSensitive = dbPlayer.CharacterType == CharacterType.ForceSensitive;

            DisplayAchievementNotification = dbPlayer.Settings.DisplayAchievementNotification;
            SubdualMode = dbPlayer.Settings.IsSubdualModeEnabled;
            DisplayServerResetReminders = dbPlayer.Settings.DisplayServerResetReminders;
            PortraitVitals = dbPlayer.Settings.PortraitVitals ?? true;
        }

        private void LoadIdentityView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            ShowDescriptorsForNamedPlayers = dbPlayer.Settings.ShowDescriptorsForNamedPlayers ?? true;
            ShowOwnDescriptor = dbPlayer.Settings.ShowOwnDescriptor ?? true;
            ScrambleAccountName = dbPlayer.Settings.ScrambleAccountName ?? true;
        }

        // One row DTO per chat color entry, replacing the three hand-synced
        // parallel GuiBindingList instances LoadChatView used to build in
        // lockstep. Language is null for the fixed OOC/Emotes rows.
        private sealed class ChatColorEntry
        {
            public SkillType? Language { get; }
            public string Name { get; }
            public GuiColor Color { get; }
            public bool Toggle { get; }

            public ChatColorEntry(SkillType? language, string name, GuiColor color, bool toggle)
            {
                Language = language;
                Name = name;
                Color = color;
                Toggle = toggle;
            }
        }

        private static readonly GuiTableSource<SettingsViewModel, ChatColorEntry> ChatColorTable =
            new GuiTableSource<SettingsViewModel, ChatColorEntry>()
                .Column((m, v) => m.ChatColorNames = v, r => r.Name)
                .Column((m, v) => m.ChatColors = v, r => r.Color)
                .Column((m, v) => m.ChatColorToggles = v, r => r.Toggle);

        private void LoadChatView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var colorSettings = dbPlayer.Settings.LanguageChatColors;
            var languages = Skill.GetActiveSkillsByCategory(SkillCategoryType.Languages);

            var rows = new List<ChatColorEntry>();

            // OOC color
            GuiColor oocColor;
            if (dbPlayer.Settings.OOCChatColor == null)
            {
                oocColor = new GuiColor(
                    Communication.OOCChatColor.Item1,
                    Communication.OOCChatColor.Item2,
                    Communication.OOCChatColor.Item3);
            }
            else
            {
                oocColor = new GuiColor(
                    dbPlayer.Settings.OOCChatColor.Red,
                    dbPlayer.Settings.OOCChatColor.Green,
                    dbPlayer.Settings.OOCChatColor.Blue);
            }
            rows.Add(new ChatColorEntry(null, "OOC", oocColor, false));

            // Emote color
            GuiColor emoteColor;
            if (dbPlayer.Settings.EmoteChatColor == null)
            {
                emoteColor = new GuiColor(
                    Communication.EmoteChatColor.Item1,
                    Communication.EmoteChatColor.Item2,
                    Communication.EmoteChatColor.Item3);
            }
            else
            {
                emoteColor = new GuiColor(
                    dbPlayer.Settings.EmoteChatColor.Red,
                    dbPlayer.Settings.EmoteChatColor.Green,
                    dbPlayer.Settings.EmoteChatColor.Blue);
            }
            rows.Add(new ChatColorEntry(null, "Emotes", emoteColor, false));

            // Language colors
            foreach (var (type, skill) in languages)
            {
                GuiColor languageColor;
                if (colorSettings != null &&
                    colorSettings.ContainsKey(type))
                {
                    var playerSetting = colorSettings[type];
                    languageColor = new GuiColor(playerSetting.Red, playerSetting.Green, playerSetting.Blue);
                }
                else
                {
                    var (red, green, blue) = Language.GetColor(type);
                    languageColor = new GuiColor(red, green, blue);
                }

                rows.Add(new ChatColorEntry(type, skill.Name, languageColor, false));
            }

            // Row-index lookups (SetColor, OnSave, OnClickResetColor) index into
            // this in lockstep with the bound lists, offset by NumberOfSystemColors.
            _languages = new List<SkillType>();
            foreach (var row in rows)
            {
                if (row.Language.HasValue)
                    _languages.Add(row.Language.Value);
            }

            ChatColorTable.Refresh(this, rows);
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
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.Settings.DisplayAchievementNotification = DisplayAchievementNotification;
            dbPlayer.Settings.IsSubdualModeEnabled = SubdualMode;
            dbPlayer.Settings.DisplayServerResetReminders = DisplayServerResetReminders;
            dbPlayer.Settings.PortraitVitals = PortraitVitals;
            if (!GetIsDM(Player) && !GetIsDMPossessed(Player))
            {
                dbPlayer.Settings.ShowDescriptorsForNamedPlayers = ShowDescriptorsForNamedPlayers;
                dbPlayer.Settings.ShowOwnDescriptor = ShowOwnDescriptor;
                dbPlayer.Settings.ScrambleAccountName = ScrambleAccountName;
            }

            if (ChatColors == null || ChatColors.Count < NumberOfSystemColors)
                LoadChatView();

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

            DB.Set(dbPlayer);

            // Apply the vitals display preference immediately (portrait overlay vs. docked window).
            PlayerStatusWindow.ApplyStatusDisplay(Player);

            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);

            PlayerName.RefreshNameOverridesForObserver(Player);
            PlayerName.RefreshNameOverridesForPlayer(Player);

            SendMessageToPC(Player, ColorToken.Green("Settings updated."));
        };

        public Action OnCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);
        };

        public Action OnClickChangeDescription() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
        };

        public Action OnClickGeneral() => () =>
        {
            IsGeneralSelected = true;
            IsIdentitySelected = false;
            IsChatSelected = false;
            ChangePartialView(SettingsView, GeneralPartial);
            LoadGeneralView();
        };

        public Action OnClickIdentity() => () =>
        {
            IsGeneralSelected = false;
            IsIdentitySelected = true;
            IsChatSelected = false;
            ChangePartialView(SettingsView, IdentityPartial);
            LoadIdentityView();
        };

        public Action OnClickChat() => () =>
        {
            IsGeneralSelected = false;
            IsIdentitySelected = false;
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
                        Communication.OOCChatColor.Item1,
                        Communication.OOCChatColor.Item2,
                        Communication.OOCChatColor.Item3);
                }
                else if (index == 1) // Emotes
                {
                    ChatColors[index] = new GuiColor(
                        Communication.EmoteChatColor.Item1,
                        Communication.EmoteChatColor.Item2,
                        Communication.EmoteChatColor.Item3);
                }
                else
                {
                    var type = _languages[index - NumberOfSystemColors];
                    var (red, green, blue) = Language.GetColor(type);
                    ChatColors[index] = new GuiColor(red, green, blue);
                }

                ChangePartialView(SettingsView, ChatPartial);
            });
        };
    }
}
