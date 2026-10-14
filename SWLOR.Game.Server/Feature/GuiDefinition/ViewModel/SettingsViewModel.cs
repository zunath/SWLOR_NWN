using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SettingsViewModel: GuiViewModelBase<SettingsViewModel, GuiPayloadBase>
    {
        public const string SettingsView = "SETTINGS_VIEW";

        public const string GeneralPartial = "GENERAL_VIEW";
        public const string IdentityPartial = "IDENTITY_VIEW";
        public const string ChatPartial = "CHAT_VIEW";
        public const string TargetingPartial = "TARGETING_VIEW";

        private static readonly GuiTabGroup<SettingsViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<SettingsViewModel, GuiPayloadBase>()
                .AddTab(0, GeneralPartial)
                .AddTab(1, IdentityPartial)
                .AddTab(2, ChatPartial)
                .AddTab(3, TargetingPartial);
        private readonly GuiToggleGroupSync _tabToggles = new(0, 1, 2, 3);

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

        public bool DisplayCommsOutOfRangeWarnings
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool DisplayCooldownFloatingText
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int SelectedTabId { get => Get<int>(); private set => Set(value); }
        public int TabToggleValue
        {
            get => Get<int>();
            set { Set(value); _tabToggles.HandleClientChange(value, SelectTab); }
        }

        public bool SelfQuickCastEnabled
        {
            get => Get<bool>();
            set { Set(value); RefreshQuickCastAvailability(); }
        }
        public bool EnemyQuickCastEnabled
        {
            get => Get<bool>();
            set { Set(value); RefreshQuickCastAvailability(); }
        }
        public int SelectedSelfCastFeat
        {
            get => Get<int>();
            set { Set(value); RefreshQuickCastAvailability(); }
        }
        public int SelectedEnemyCastFeat
        {
            get => Get<int>();
            set { Set(value); RefreshQuickCastAvailability(); }
        }
        public GuiBindingList<GuiComboEntry> SelfCastAbilities { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public GuiBindingList<GuiComboEntry> EnemyCastAbilities { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public bool CanSelfQuickCast { get => Get<bool>(); private set => Set(value); }
        public bool CanEnemyQuickCast { get => Get<bool>(); private set => Set(value); }
        public string TargetingFeedback { get => Get<string>(); private set => Set(value); }

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
            SelectedTabId = 0;
            _tabToggles.SyncTo(0, value => TabToggleValue = value);
            CurrentRed = 0;
            CurrentGreen = 0;
            CurrentBlue = 0;

            LoadGeneralView();
            LoadIdentityView();
            LoadChatView();
            LoadTargetingView();

            Tabs.Select(this, SettingsView, SelectedTabId, onAfterApply: RefreshPartialViewBindings);

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.DisplayAchievementNotification);
            WatchOnClient(model => model.SubdualMode);
            WatchOnClient(model => model.DisplayServerResetReminders);
            WatchOnClient(model => model.PortraitVitals);
            WatchOnClient(model => model.ShowDescriptorsForNamedPlayers);
            WatchOnClient(model => model.ShowOwnDescriptor);
            WatchOnClient(model => model.ScrambleAccountName);
            WatchOnClient(model => model.DisplayCommsOutOfRangeWarnings);
            WatchOnClient(model => model.DisplayCooldownFloatingText);
            WatchOnClient(model => model.SelectedColor);
            WatchOnClient(model => model.SelfQuickCastEnabled);
            WatchOnClient(model => model.EnemyQuickCastEnabled);
            WatchOnClient(model => model.SelectedSelfCastFeat);
            WatchOnClient(model => model.SelectedEnemyCastFeat);
        }

        private void LoadTargetingView()
        {
            SelfCastAbilities = BuildQuickCastOptions(QuickCastTargetType.Self);
            EnemyCastAbilities = BuildQuickCastOptions(QuickCastTargetType.CurrentEnemy);
            SelectedSelfCastFeat = SelfCastAbilities[0].Value;
            SelectedEnemyCastFeat = EnemyCastAbilities[0].Value;
            SelfQuickCastEnabled = QuickCast.IsEnabled(Player, QuickCastTargetType.Self);
            EnemyQuickCastEnabled = QuickCast.IsEnabled(Player, QuickCastTargetType.CurrentEnemy);
            TargetingFeedback = "Save enables these buttons for this login. No chat commands or quickbar changes are needed.";
        }

        private GuiBindingList<GuiComboEntry> BuildQuickCastOptions(QuickCastTargetType type)
        {
            var options = new GuiBindingList<GuiComboEntry>();
            foreach (var (feat, name) in QuickCast.GetAvailableAbilities(Player, type))
                options.Add(new GuiComboEntry(name, (int)feat));
            if (options.Count == 0)
                options.Add(new GuiComboEntry("No eligible abilities", -1));
            return options;
        }

        private void RefreshQuickCastAvailability()
        {
            CanSelfQuickCast = SelfQuickCastEnabled && QuickCast.IsEnabled(Player, QuickCastTargetType.Self) &&
                               SelfCastAbilities?.Any(entry => entry.Value >= 0 && entry.Value == SelectedSelfCastFeat) == true;
            CanEnemyQuickCast = EnemyQuickCastEnabled && QuickCast.IsEnabled(Player, QuickCastTargetType.CurrentEnemy) &&
                                EnemyCastAbilities?.Any(entry => entry.Value >= 0 && entry.Value == SelectedEnemyCastFeat) == true;
        }

        private void CastSelectedAbility(QuickCastTargetType type)
        {
            var self = type == QuickCastTargetType.Self;
            if (!(self ? CanSelfQuickCast : CanEnemyQuickCast))
            {
                TargetingFeedback = "Enable this mode, choose an eligible ability, and click Save first.";
                return;
            }
            var feat = self ? SelectedSelfCastFeat : SelectedEnemyCastFeat;
            // NUI events run on the module; ability delays and effects need the same
            // creature context as native feat activation.
            AssignCommand(Player, () =>
            {
                if (QuickCast.TryCast(Player, type, feat.ToString(), out var error))
                    TargetingFeedback = "Ability started.";
                else
                    TargetingFeedback = string.IsNullOrWhiteSpace(error) ? "Ability could not be activated. See your combat log." : error;
            });
        }

        public Action OnSelfQuickCast() => () => CastSelectedAbility(QuickCastTargetType.Self);
        public Action OnEnemyQuickCast() => () => CastSelectedAbility(QuickCastTargetType.CurrentEnemy);

        private void LoadGeneralView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            IsForceSensitive = dbPlayer.CharacterType == CharacterType.ForceSensitive;

            DisplayAchievementNotification = dbPlayer.Settings.DisplayAchievementNotification;
            SubdualMode = dbPlayer.Settings.IsSubdualModeEnabled;
            DisplayServerResetReminders = dbPlayer.Settings.DisplayServerResetReminders;
            PortraitVitals = dbPlayer.Settings.PortraitVitals ?? true;
            DisplayCommsOutOfRangeWarnings = dbPlayer.Settings.DisplayCommsOutOfRangeWarnings ?? true;
            DisplayCooldownFloatingText = dbPlayer.Settings.DisplayCooldownFloatingText;
        }

        private void LoadIdentityView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            ShowDescriptorsForNamedPlayers = dbPlayer.Settings.ShowDescriptorsForNamedPlayers ?? true;
            ShowOwnDescriptor = dbPlayer.Settings.ShowOwnDescriptor ?? true;
            ScrambleAccountName = dbPlayer.Settings.ScrambleAccountName ?? true;
        }

        private void LoadChatView()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var colorSettings = dbPlayer.Settings.LanguageChatColors;
            var languages = Skill.GetActiveSkillsByCategory(SkillCategoryType.Languages);

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
                    Communication.OOCChatColor.Item1,
                    Communication.OOCChatColor.Item2,
                    Communication.OOCChatColor.Item3));
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
                    Communication.EmoteChatColor.Item1,
                    Communication.EmoteChatColor.Item2,
                    Communication.EmoteChatColor.Item3));
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
                    var (red, green, blue) = Language.GetColor(type);
                    chatColors.Add(new GuiColor(red, green, blue));
                }
            }

            ChatColorNames = chatColorNames;
            ChatColors = chatColors;
            ChatColorToggles = chatToggles;
        }

        private void SelectTab(int tabId)
        {
            // Capture the client's current position before the partial-view redraw workaround
            // temporarily changes the window geometry.
            UpdatePropertyFromClient(nameof(Geometry));

            SelectedTabId = tabId;
            _tabToggles.SyncTo(tabId, value => TabToggleValue = value);
            Tabs.Select(this, SettingsView, tabId, onAfterApply: RefreshPartialViewBindings);
        }

        private void RefreshPartialViewBindings()
        {
            // Republish scalar bindings after replacing the partial. Newly inserted controls
            // otherwise render their client-side defaults until the value changes, which makes
            // default-enabled settings such as account-name hiding appear unchecked.
            OnPropertyChanged(nameof(DisplayAchievementNotification));
            OnPropertyChanged(nameof(SubdualMode));
            OnPropertyChanged(nameof(DisplayServerResetReminders));
            OnPropertyChanged(nameof(PortraitVitals));
            OnPropertyChanged(nameof(DisplayCommsOutOfRangeWarnings));
            OnPropertyChanged(nameof(DisplayCooldownFloatingText));
            OnPropertyChanged(nameof(ShowOwnDescriptor));
            OnPropertyChanged(nameof(ShowDescriptorsForNamedPlayers));
            OnPropertyChanged(nameof(ScrambleAccountName));
            OnPropertyChanged(nameof(CurrentRed));
            OnPropertyChanged(nameof(CurrentGreen));
            OnPropertyChanged(nameof(CurrentBlue));
            OnPropertyChanged(nameof(TabToggleValue));
            OnPropertyChanged(nameof(SelfQuickCastEnabled));
            OnPropertyChanged(nameof(EnemyQuickCastEnabled));
            OnPropertyChanged(nameof(SelectedSelfCastFeat));
            OnPropertyChanged(nameof(SelectedEnemyCastFeat));
            OnPropertyChanged(nameof(SelfCastAbilities));
            OnPropertyChanged(nameof(EnemyCastAbilities));
            OnPropertyChanged(nameof(CanSelfQuickCast));
            OnPropertyChanged(nameof(CanEnemyQuickCast));
            OnPropertyChanged(nameof(TargetingFeedback));

            // Republish list bindings so any newly inserted list can populate its rows without
            // reloading persisted values over unsaved changes.
            ChatColorNames?.ResetBindings();
            ChatColors?.ResetBindings();
            ChatColorToggles?.ResetBindings();
        }

        protected override void OnModalClosedRestore() => SelectTab(SelectedTabId);

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
            dbPlayer.Settings.DisplayCommsOutOfRangeWarnings = DisplayCommsOutOfRangeWarnings;
            dbPlayer.Settings.DisplayCooldownFloatingText = DisplayCooldownFloatingText;
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
            QuickCast.SetEnabled(Player, QuickCastTargetType.Self, SelfQuickCastEnabled);
            QuickCast.SetEnabled(Player, QuickCastTargetType.CurrentEnemy, EnemyQuickCastEnabled);
            RefreshQuickCastAvailability();
            TargetingFeedback = "Targeting settings applied for this login. Both modes reset to off when you log out.";
            Log.Write(LogGroup.Server, $"Settings saved for player {playerId}.");

            // Apply the vitals display preference immediately (portrait overlay vs. docked window).
            PlayerStatusWindow.ApplyStatusDisplay(Player);

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
            if (Disguise.GetActiveDisguise(Player) != null)
            {
                SendMessageToPC(Player, ColorToken.Red("Edit your active disguise's biography from the Disguises window. Deactivate it to edit your normal biography."));
                return;
            }

            Gui.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
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
            UpdatePropertyFromClient(nameof(Geometry));

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
            });
        };
    }
}
