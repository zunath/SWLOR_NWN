using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using System.Collections.Generic;
using System.Linq;
using static SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetFormat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CharacterSheetViewModel : GuiViewModelBase<CharacterSheetViewModel, CharacterSheetPayload>,
        IGuiRefreshable<ChangePortraitRefreshEvent>,
        IGuiRefreshable<DisguiseChangedRefreshEvent>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<PlayerStatusRefreshEvent>,
        IGuiRefreshable<StatusEffectReceivedRefreshEvent>,
        IGuiRefreshable<StatusEffectRemovedRefreshEvent>,
        IGuiRefreshable<StatAdjustmentRefreshEvent>,
        IGuiRefreshable<BeastGainXPRefreshEvent>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>,
        IGuiRefreshable<TechniqueChangedRefreshEvent>,
        ICharacterSheetHost
    {
        private const int AttributesTabId = 0;
        private const int StatsTabId = 1;
        private const int ResistancesTabId = 2;
        private const int CraftingTabId = 3;
        public const string TabContentPartialElement = "character_sheet_tab_content";
        public const string AttributesTabPartial = "CHARACTER_SHEET_ATTRIBUTES_TAB";
        public const string StatsTabPartial = "CHARACTER_SHEET_STATS_TAB";
        public const string ResistancesTabPartial = "CHARACTER_SHEET_RESISTANCES_TAB";
        public const string CraftingTabPartial = "CHARACTER_SHEET_CRAFTING_TAB";

        private uint _target;
        private int _hitPointRefreshGeneration;

        private CharacterSheetAttributesTab CharacterSheetAttributesTab { get; }
        private CharacterSheetTableTabs CharacterSheetTableTabs { get; }
        private CharacterSheetWeaponRatings CharacterSheetWeaponRatings { get; }
        private IBeastMasteryService BeastMastery { get; }
        private IDBService DB { get; }
        private IGuiService Gui { get; }
        private IPerkService Perk { get; }
        private ISkillService Skill { get; }
        public CharacterSheetViewModel(IBeastMasteryService beastMastery, CharacterSheetAttributesTab characterSheetAttributesTab, CharacterSheetWeaponRatings characterSheetWeaponRatings, IDBService db, IGuiService gui, IPerkService perk, ISkillService skill, CharacterSheetTableTabs characterSheetTableTabs)
        {
            BeastMastery = beastMastery;
            CharacterSheetAttributesTab = characterSheetAttributesTab;
            CharacterSheetWeaponRatings = characterSheetWeaponRatings;
            DB = db;
            Gui = gui;
            Perk = perk;
            Skill = skill;
            CharacterSheetTableTabs = characterSheetTableTabs;
        }


        // Tab registration: id -> partial view -> refresh action. Replaces
        // GetTabPartialName + the RefreshSelectedTabData switch statement that
        // used to live inside RestoreSelectedTabPartial.
        private static readonly GuiTabGroup<CharacterSheetViewModel, CharacterSheetPayload> Tabs =
            new GuiTabGroup<CharacterSheetViewModel, CharacterSheetPayload>()
                .AddTab(AttributesTabId, AttributesTabPartial)
                .AddTab(StatsTabId, StatsTabPartial, m => { if (GetIsObjectValid(m._target)) m.RefreshCharacterStatsList(); })
                .AddTab(ResistancesTabId, ResistancesTabPartial, m => { if (GetIsObjectValid(m._target)) m.RefreshResistances(); })
                .AddTab(CraftingTabId, CraftingTabPartial, m => { if (GetIsObjectValid(m._target)) m.RefreshCraftingStats(); });

        // Paired-toggle sync: replaces the hand-written _isSynchronizingTabRows
        // guard. Each group maps its own local toggle index (0/1) to a shared
        // tab id.
        private static readonly GuiToggleGroupSync TopToggles = new(AttributesTabId, StatsTabId);
        private static readonly GuiToggleGroupSync BottomToggles = new(ResistancesTabId, CraftingTabId);

        // Column mappings: which bound property receives each column, and how
        // to pull that column's value out of a row DTO. Replaces the 3
        // hand-rolled parallel-list-building blocks previously duplicated
        // across RefreshCharacterStatsList / RefreshResistances / RefreshCraftingStats.
        private static readonly GuiTableSource<CharacterSheetViewModel, StatEntry> StatsTable =
            new GuiTableSource<CharacterSheetViewModel, StatEntry>()
                .Column((m, v) => m.StatNames = v, r => r.Name)
                .Column((m, v) => m.StatValues = v, r => r.Value)
                .Column((m, v) => m.StatTooltips = v, r => r.Tooltip);

        private static readonly GuiTableSource<CharacterSheetViewModel, ResistanceEntry> ResistancesTable =
            new GuiTableSource<CharacterSheetViewModel, ResistanceEntry>()
                .Column((m, v) => m.ResistanceNames = v, r => r.Name)
                .Column((m, v) => m.ResistanceScores = v, r => r.Score)
                .Column((m, v) => m.ResistanceDamageTaken = v, r => r.DamageTaken)
                .Column((m, v) => m.ResistanceStatusDurations = v, r => r.StatusDuration);

        private static readonly GuiTableSource<CharacterSheetViewModel, CraftEntry> CraftingTable =
            new GuiTableSource<CharacterSheetViewModel, CraftEntry>()
                .Column((m, v) => m.CraftNames = v, r => r.Name)
                .Column((m, v) => m.CraftControls = v, r => r.Control)
                .Column((m, v) => m.CraftCraftsmanship = v, r => r.Craftsmanship);

        public bool IsViewingTarget(uint target)
        {
            return _target == target;
        }

        public int SelectedTabId
        {
            get => Get<int>();
            set
            {
                Set(value);

                // Drive both toggle-pair properties to reflect the new
                // selection (or -1 if this tab isn't in that pair).
                TopToggles.SyncTo(value, v => TopTabId = v);
                BottomToggles.SyncTo(value, v => BottomTabId = v);

                // Runs the tab's refresh action, then swaps the nested
                // partial via the safe double-reapply path.
                Tabs.Select(this, TabContentPartialElement, value);
            }
        }

        public int TopTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                TopToggles.HandleClientChange(value, tabId => SelectedTabId = tabId);
            }
        }

        public int BottomTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                BottomToggles.HandleClientChange(value, tabId => SelectedTabId = tabId);
            }
        }

        public bool IsPlayerMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowSP
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowSkillRanks
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowAPOrLevel
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string PortraitResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HP
        {
            get => Get<string>();
            set => Set(value);
        }
        public string FP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string STM
        {
            get => Get<string>();
            set => Set(value);
        }

        public string APOrLevelLabel
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Might
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Perception
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Vitality
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Willpower
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Agility
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Social
        {
            get => Get<int>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MainHandDMG
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OffHandDMG
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MainHandTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OffHandTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AttackDelay
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AttackDelayTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Attack
        {
            get => Get<int>();
            set => Set(value);
        }

        public int ForceAttack
        {
            get => Get<int>();
            set => Set(value);
        }

        public int PhysicalDefense
        {
            get => Get<int>();
            set => Set(value);
        }

        public int ForceDefense
        {
            get => Get<int>();
            set => Set(value);
        }

        public int WeaponAccuracy
        {
            get => Get<int>();
            set => Set(value);
        }

        public string OffHandAccuracy
        {
            get => Get<string>();
            set => Set(value);
        }

        public const string AttackDescription = "Physical attack rating used to scale weapon and physical ability damage.";

        public string AttackTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public int ForceAccuracy
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Evasion
        {
            get => Get<int>();
            set => Set(value);
        }

        public string CharacterType
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Race
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SkillRanks
        {
            get => Get<string>();
            set => Set(value);
        }

        public string APOrLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string APOrLevelTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Control
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Craftsmanship
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> ResistanceNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResistanceScores
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResistanceDamageTaken
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResistanceStatusDurations
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> StatNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> StatValues
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> StatTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CraftNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CraftControls
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CraftCraftsmanship
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public bool IsMightUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPerceptionUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsVitalityUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsWillpowerUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAgilityUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSocialUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsTechniquesEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Action OnClickSkills() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Skills);
        };

        public Action OnClickGuide() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.PlayerGuide);
        };

        public Action OnClickPerks() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Perks);
        };

        public Action OnClickTechniques() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Techniques, new TechniquesPayload());
        };

        public Action OnClickChangePortrait() => () =>
        {
            var payload = new CustomizeCharacterPayload(_target);
            Gui.TogglePlayerWindow(Player, GuiWindowType.CustomizeCharacter, payload);
        };

        public Action OnClickQuests() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Quests);
        };

        public Action OnClickRecipes() => () =>
        {
            var payload = new RecipesPayload(RecipesUIMode.Recipes, SkillType.Invalid);
            Gui.TogglePlayerWindow(Player, GuiWindowType.Recipes, payload);
        };

        public Action OnClickKeyItems() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.KeyItems);
        };

        public Action OnClickCurrencies() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Currencies);
        };

        public Action OnClickAchievements() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Achievements);
        };

        public Action OnClickNotes() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Notes);
        };

        public Action OnClickOpenTrash() => () =>
        {
            var location = GetLocation(Player);
            var trash = CreateObject(ObjectType.Placeable, "reo_trash_can", location);
            AssignCommand(Player, () => ActionInteractObject(trash));
            DelayCommand(0.2f, () => SetUseableFlag(trash, false));
        };

        public Action OnClickAppearance() => () =>
        {
            var payload = new AppearanceEditorPayload(Player);
            Gui.TogglePlayerWindow(Player, GuiWindowType.AppearanceEditor, payload);
        };

        public Action OnClickSettings() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);
        };

        public Action OnClickDisguises() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Disguises);
        };

        protected override void OnModalClosedRestore() =>
            Tabs.Select(this, TabContentPartialElement, SelectedTabId);

        public Action OnClickUpgradeMight() => () =>
        {
            CharacterSheetAttributesTab.UpgradeAttribute(this, AbilityType.Might, "Might");
        };

        public Action OnClickUpgradePerception() => () =>
        {
            CharacterSheetAttributesTab.UpgradeAttribute(this, AbilityType.Perception, "Perception");
        };

        public Action OnClickUpgradeVitality() => () =>
        {
            CharacterSheetAttributesTab.UpgradeAttribute(this, AbilityType.Vitality, "Vitality");
        };

        public Action OnClickUpgradeWillpower() => () =>
        {
            CharacterSheetAttributesTab.UpgradeAttribute(this, AbilityType.Willpower, "Willpower");
        };

        public Action OnClickUpgradeAgility() => () =>
        {
            CharacterSheetAttributesTab.UpgradeAttribute(this, AbilityType.Agility, "Agility");
        };

        public Action OnClickUpgradeSocial() => () =>
        {
            CharacterSheetAttributesTab.UpgradeAttribute(this, AbilityType.Social, "Social");
        };


        private void RefreshStats() => CharacterSheetAttributesTab.RefreshStats(this);

        private void RefreshEquipmentStats()
        {
            CharacterSheetWeaponRatings.Refresh(this);

            RefreshResistances();
            RefreshCraftingStats();
            RefreshCharacterStatsList();
        }

        private void RefreshCharacterStatsList() => CharacterSheetTableTabs.RefreshCharacterStatsList(this);

        private void RefreshResistances() => CharacterSheetTableTabs.RefreshResistances(this);

        private void RefreshCraftingStats() => CharacterSheetTableTabs.RefreshCraftingStats(this);

        private void RefreshAttributes() => CharacterSheetAttributesTab.RefreshAttributes(this);

        private void RefreshPortrait()
        {
            PortraitResref = GetPortraitResRef(_target) + "l";
        }

        private void LoadData()
        {
            CharacterType = GetClassByPosition(1, _target) == ClassType.Standard ? "Standard" : "Force Sensitive";
            Race = GetStringByStrRef(Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(_target))), GetGender(_target));
            IsTechniquesEnabled = Perk.GetPerkLevel(_target, PerkType.CombatAnalyzer) >= 1;

            if (IsPlayerMode)
            {
                APOrLevelLabel = "AP";
                APOrLevelTooltip = "Increase attributes.";
            }
            else
            {
                APOrLevelLabel = "Level";
            }

            RefreshPortrait();
            RefreshStats();
            RefreshEquipmentStats();
            RefreshAttributes();
        }

        protected override void Initialize(CharacterSheetPayload initialPayload)
        {
            _target = GetIsObjectValid(initialPayload.Target) ? initialPayload.Target : Player;
            IsPlayerMode = initialPayload.IsPlayerMode;
            ShowSP = IsPlayerMode || BeastMastery.IsPlayerBeast(_target);
            ShowSkillRanks = GetIsPC(_target);
            ShowAPOrLevel = ShowSP;
            SelectedTabId = AttributesTabId;

            LoadData();
            WatchOnClient(model => model.TopTabId);
            WatchOnClient(model => model.BottomTabId);
            var refreshGeneration = ++_hitPointRefreshGeneration;
            Scheduler.Schedule(() => RefreshLiveHitPoints(refreshGeneration), TimeSpan.FromSeconds(1));
        }

        private void RefreshHitPoints()
        {
            var normalHP = ObjectPlugin.GetCurrentHitPoints(_target);
            var maxHP = GetMaxHitPoints(_target);
            var temporaryHP = TemporaryHitPointEffects.GetRemaining(_target);
            HP = $"{normalHP + temporaryHP} / {maxHP}";
        }

        public override Action OnWindowClosed() => () => ++_hitPointRefreshGeneration;

        private void RefreshLiveHitPoints(int refreshGeneration)
        {
            if (refreshGeneration != _hitPointRefreshGeneration ||
                !GetIsObjectValid(Player) || !GetIsObjectValid(_target) ||
                NuiFindWindow(Player, Gui.BuildWindowId(WindowType)) != WindowToken)
                return;

            RefreshHitPoints();
            Scheduler.Schedule(() => RefreshLiveHitPoints(refreshGeneration), TimeSpan.FromSeconds(1));
        }

        public void Refresh(ChangePortraitRefreshEvent payload)
        {
            RefreshPortrait();
        }

        public void Refresh(DisguiseChangedRefreshEvent payload)
        {
            RefreshPortrait();
            RefreshStats();
        }

        public void Refresh(SkillXPRefreshEvent payload)
        {
            if (!GetIsPC(_target))
                return;

            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);

            SkillRanks = $"{Skill.GetTotalContributingSkillRanks(dbPlayer)} / {SkillService.SkillCap}";
            SP = $"{Skill.GetTotalSkillPoints(dbPlayer)} / {SkillService.TotalSkillPointCap} ({dbPlayer.UnallocatedSP})";
            APOrLevel = $"{dbPlayer.TotalAPAcquired} / {Skill.APCap} ({dbPlayer.UnallocatedAP})";

            RefreshStats();
        }

        public void Refresh(BeastGainXPRefreshEvent payload)
        {
            if (!BeastMastery.IsPlayerBeast(_target))
                return;

            var beastId = BeastMastery.GetBeastId(_target);
            var dbBeast = DB.Get<Beast>(beastId);

            SP = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel} ({dbBeast.UnallocatedSP})";
            APOrLevel = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel}";
            APOrLevelTooltip = $"XP: {dbBeast.XP} / {BeastMastery.GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent)}";
            RefreshCharacterStatsList();
        }

        public void Refresh(PerkAcquiredRefreshEvent payload)
        {
            if (!GetIsPC(_target))
                return;

            LoadData();
        }

        public void Refresh(PerkRefundedRefreshEvent payload)
        {
            if (!GetIsPC(_target))
                return;

            LoadData();
        }

        public void Refresh(TechniqueChangedRefreshEvent payload)
        {
            if (!GetIsPC(_target))
                return;

            RefreshStats();
            RefreshEquipmentStats();
        }

        public void Refresh(EquipItemRefreshEvent payload)
        {
            RefreshEquipmentStats();
        }

        public void Refresh(UnequipItemRefreshEvent payload)
        {
            RefreshStats();
            RefreshEquipmentStats();
        }

        void IGuiRefreshable<PlayerStatusRefreshEvent>.Refresh(PlayerStatusRefreshEvent payload)
        {
            if (!GetIsObjectValid(_target))
                return;

            switch (payload.Type)
            {
                case PlayerStatusRefreshEvent.StatType.HP:
                case PlayerStatusRefreshEvent.StatType.FP:
                case PlayerStatusRefreshEvent.StatType.STM:
                    RefreshStats();
                    RefreshEquipmentStats();
                    break;
            }
        }

        public void Refresh(StatusEffectReceivedRefreshEvent payload)
        {
            RefreshStats();
            RefreshEquipmentStats();
        }

        public void Refresh(StatusEffectRemovedRefreshEvent payload)
        {
            RefreshStats();
            RefreshEquipmentStats();
        }

        public void Refresh(StatAdjustmentRefreshEvent payload)
        {
            RefreshStats();
            RefreshEquipmentStats();
        }

        uint ICharacterSheetHost.Player => Player;
        uint ICharacterSheetHost.Target => _target;
        bool ICharacterSheetHost.IsPlayerMode => IsPlayerMode;
        string ICharacterSheetHost.AttackDescription => AttackDescription;
        string ICharacterSheetHost.HP { get => HP; set => HP = value; }
        string ICharacterSheetHost.FP { get => FP; set => FP = value; }
        string ICharacterSheetHost.STM { get => STM; set => STM = value; }
        string ICharacterSheetHost.Name { get => Name; set => Name = value; }
        int ICharacterSheetHost.Might { get => Might; set => Might = value; }
        int ICharacterSheetHost.Perception { get => Perception; set => Perception = value; }
        int ICharacterSheetHost.Vitality { get => Vitality; set => Vitality = value; }
        int ICharacterSheetHost.Willpower { get => Willpower; set => Willpower = value; }
        int ICharacterSheetHost.Agility { get => Agility; set => Agility = value; }
        int ICharacterSheetHost.Social { get => Social; set => Social = value; }
        bool ICharacterSheetHost.IsMightUpgradeAvailable { get => IsMightUpgradeAvailable; set => IsMightUpgradeAvailable = value; }
        bool ICharacterSheetHost.IsPerceptionUpgradeAvailable { get => IsPerceptionUpgradeAvailable; set => IsPerceptionUpgradeAvailable = value; }
        bool ICharacterSheetHost.IsVitalityUpgradeAvailable { get => IsVitalityUpgradeAvailable; set => IsVitalityUpgradeAvailable = value; }
        bool ICharacterSheetHost.IsWillpowerUpgradeAvailable { get => IsWillpowerUpgradeAvailable; set => IsWillpowerUpgradeAvailable = value; }
        bool ICharacterSheetHost.IsAgilityUpgradeAvailable { get => IsAgilityUpgradeAvailable; set => IsAgilityUpgradeAvailable = value; }
        bool ICharacterSheetHost.IsSocialUpgradeAvailable { get => IsSocialUpgradeAvailable; set => IsSocialUpgradeAvailable = value; }
        string ICharacterSheetHost.SkillRanks { get => SkillRanks; set => SkillRanks = value; }
        string ICharacterSheetHost.SP { get => SP; set => SP = value; }
        string ICharacterSheetHost.APOrLevel { get => APOrLevel; set => APOrLevel = value; }
        string ICharacterSheetHost.APOrLevelTooltip { get => APOrLevelTooltip; set => APOrLevelTooltip = value; }
        string ICharacterSheetHost.MainHandDMG { get => MainHandDMG; set => MainHandDMG = value; }
        string ICharacterSheetHost.OffHandDMG { get => OffHandDMG; set => OffHandDMG = value; }
        string ICharacterSheetHost.MainHandTooltip { get => MainHandTooltip; set => MainHandTooltip = value; }
        string ICharacterSheetHost.OffHandTooltip { get => OffHandTooltip; set => OffHandTooltip = value; }
        string ICharacterSheetHost.AttackDelay { get => AttackDelay; set => AttackDelay = value; }
        string ICharacterSheetHost.AttackDelayTooltip { get => AttackDelayTooltip; set => AttackDelayTooltip = value; }
        int ICharacterSheetHost.Attack { get => Attack; set => Attack = value; }
        string ICharacterSheetHost.AttackTooltip { get => AttackTooltip; set => AttackTooltip = value; }
        int ICharacterSheetHost.ForceAttack { get => ForceAttack; set => ForceAttack = value; }
        int ICharacterSheetHost.PhysicalDefense { get => PhysicalDefense; set => PhysicalDefense = value; }
        int ICharacterSheetHost.ForceDefense { get => ForceDefense; set => ForceDefense = value; }
        int ICharacterSheetHost.WeaponAccuracy { get => WeaponAccuracy; set => WeaponAccuracy = value; }
        string ICharacterSheetHost.OffHandAccuracy { get => OffHandAccuracy; set => OffHandAccuracy = value; }
        int ICharacterSheetHost.ForceAccuracy { get => ForceAccuracy; set => ForceAccuracy = value; }
        int ICharacterSheetHost.Evasion { get => Evasion; set => Evasion = value; }
        void ICharacterSheetHost.ShowModal(string prompt, Action confirmAction) => ShowModal(prompt, confirmAction);
        void ICharacterSheetHost.LoadData() => LoadData();
        void ICharacterSheetHost.RefreshHitPoints() => RefreshHitPoints();
        string ICharacterSheetHost.Control { get => Control; set => Control = value; }
        string ICharacterSheetHost.Craftsmanship { get => Craftsmanship; set => Craftsmanship = value; }
        void ICharacterSheetHost.RefreshCharacterStatsList() => RefreshCharacterStatsList();
        void ICharacterSheetHost.RefreshStatsTable(IEnumerable<StatEntry> rows) => StatsTable.Refresh(this, rows);
        void ICharacterSheetHost.RefreshResistancesTable(IEnumerable<ResistanceEntry> rows) => ResistancesTable.Refresh(this, rows);
        void ICharacterSheetHost.RefreshCraftingTable(IEnumerable<CraftEntry> rows) => CraftingTable.Refresh(this, rows);
    }
}
