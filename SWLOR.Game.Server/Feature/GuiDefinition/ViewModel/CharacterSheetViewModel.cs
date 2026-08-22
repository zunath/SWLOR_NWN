using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using System.Linq;
using Skill = SWLOR.Game.Server.Service.Skill;

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
        IGuiRefreshable<TechniqueChangedRefreshEvent>
    {
        private const int MaxPurchasedAttributeScore = 26;
        private const int RacialAttributeBonus = 1;
        private const int MaxRacialAttributeScore = MaxPurchasedAttributeScore + RacialAttributeBonus;
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

        // Row DTOs for the three tables below - one list of these per refresh,
        // instead of hand-synced parallel GuiBindingList<string> instances.
        private sealed class StatEntry
        {
            public string Name { get; }
            public string Value { get; }
            public string Tooltip { get; }

            public StatEntry(string name, string value, string tooltip)
            {
                Name = name;
                Value = value;
                Tooltip = tooltip;
            }
        }

        private sealed class ResistanceEntry
        {
            public string Name { get; }
            public string Score { get; }
            public string DamageTaken { get; }
            public string StatusDuration { get; }

            public ResistanceEntry(string name, string score, string damageTaken, string statusDuration)
            {
                Name = name;
                Score = score;
                DamageTaken = damageTaken;
                StatusDuration = statusDuration;
            }
        }

        private sealed class CraftEntry
        {
            public string Name { get; }
            public string Control { get; }
            public string Craftsmanship { get; }

            public CraftEntry(string name, string control, string craftsmanship)
            {
                Name = name;
                Control = control;
                Craftsmanship = craftsmanship;
            }
        }

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

        public bool IsHolocomEnabled
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

        public Action OnClickHoloCom() => () =>
        {
            if (Space.IsPlayerInSpaceMode(Player))
            {
                SendMessageToPC(Player, ColorToken.Red("Holocom cannot be used in space."));
                return;
            }

            Gui.TogglePlayerWindow(Player, GuiWindowType.HoloCom);
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

        private static int GetPurchasedAttributeScore(Player dbPlayer, AbilityType ability)
        {
            var baseScore = dbPlayer.BaseStats.TryGetValue(ability, out var baseValue)
                ? baseValue
                : 0;
            var upgradedScore = dbPlayer.UpgradedStats.TryGetValue(ability, out var upgradedValue)
                ? upgradedValue
                : 0;

            return baseScore + upgradedScore;
        }

        private void UpgradeAttribute(AbilityType ability, string abilityName)
        {
            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);
            var isRacial = dbPlayer.RacialStat == AbilityType.Invalid;
            var promptMessage = isRacial
                ? "WARNING: You are about to spend your one-time racial stat bonus. Once spent, this action can only be undone with a stat rebuild. Are you SURE you want to upgrade this stat?"
                : $"Upgrading your {abilityName} attribute will consume 1 AP. Are you sure you want to upgrade it?";

            ShowModal(promptMessage, () =>
            {
                if (GetResRef(GetArea(_target)) == "char_migration")
                {
                    FloatingTextStringOnCreature($"Stats cannot be upgraded in this area.", _target, false);
                    return;
                }

                playerId = GetObjectUUID(_target);
                dbPlayer = DB.Get<Player>(playerId);
                isRacial = dbPlayer.RacialStat == AbilityType.Invalid;
                var rawScore = CreaturePlugin.GetRawAbilityScore(_target, ability);
                var purchasedScore = GetPurchasedAttributeScore(dbPlayer, ability);

                if (isRacial)
                {
                    if (rawScore >= MaxRacialAttributeScore || purchasedScore > MaxPurchasedAttributeScore)
                    {
                        FloatingTextStringOnCreature($"You cannot upgrade this attribute beyond {MaxRacialAttributeScore} with a racial bonus.", _target, false);
                        return;
                    }

                    dbPlayer.RacialStat = ability;
                }
                else
                {
                    if (purchasedScore >= MaxPurchasedAttributeScore)
                    {
                        FloatingTextStringOnCreature($"You cannot upgrade this attribute beyond {MaxPurchasedAttributeScore} with AP.", _target, false);
                        return;
                    }

                    if (rawScore >= MaxRacialAttributeScore)
                    {
                        FloatingTextStringOnCreature($"You cannot upgrade this attribute beyond {MaxRacialAttributeScore}.", _target, false);
                        return;
                    }

                    if (dbPlayer.UnallocatedAP <= 0)
                    {
                        FloatingTextStringOnCreature("You do not have enough AP to purchase this upgrade.", _target, false);
                        return;
                    }

                    dbPlayer.UnallocatedAP--;
                    dbPlayer.UpgradedStats[ability]++;
                }

                CreaturePlugin.ModifyRawAbilityScore(_target, ability, 1);

                DB.Set(dbPlayer);

                FloatingTextStringOnCreature($"Your {abilityName} attribute has increased!", _target, false);
                LoadData();
            });
        }

        protected override void OnModalClosedRestore() =>
            Tabs.Select(this, TabContentPartialElement, SelectedTabId);

        private bool IsAttributeUpgradeAvailable(Player dbPlayer, AbilityType ability, bool isRacialBonusAvailable)
        {
            var rawScore = CreaturePlugin.GetRawAbilityScore(_target, ability);
            var purchasedScore = GetPurchasedAttributeScore(dbPlayer, ability);

            if (isRacialBonusAvailable)
            {
                return rawScore < MaxRacialAttributeScore &&
                       purchasedScore <= MaxPurchasedAttributeScore;
            }

            return dbPlayer.UnallocatedAP > 0 &&
                   purchasedScore < MaxPurchasedAttributeScore &&
                   rawScore < MaxRacialAttributeScore;
        }

        public Action OnClickUpgradeMight() => () =>
        {
            UpgradeAttribute(AbilityType.Might, "Might");
        };

        public Action OnClickUpgradePerception() => () =>
        {
            UpgradeAttribute(AbilityType.Perception, "Perception");
        };

        public Action OnClickUpgradeVitality() => () =>
        {
            UpgradeAttribute(AbilityType.Vitality, "Vitality");
        };

        public Action OnClickUpgradeWillpower() => () =>
        {
            UpgradeAttribute(AbilityType.Willpower, "Willpower");
        };

        public Action OnClickUpgradeAgility() => () =>
        {
            UpgradeAttribute(AbilityType.Agility, "Agility");
        };

        public Action OnClickUpgradeSocial() => () =>
        {
            UpgradeAttribute(AbilityType.Social, "Social");
        };


        private void RefreshStats()
        {
            HP = GetCurrentHitPoints(_target) + " / " + GetMaxHitPoints(_target);

            if (GetClassByPosition(1, _target) == ClassType.Standard)
            {
                FP = $"0 / 0";
            }
            else
            {
                var currentFP = Stat.GetCurrentFP(_target);
                var maxFP = Stat.GetMaxFP(_target);
                if (currentFP < 0)
                    currentFP = 0;
                if (maxFP < 0)
                    maxFP = 0;

                FP = $"{currentFP} / {maxFP}";
            }

            var currentSTM = Stat.GetCurrentStamina(_target);
            var maxSTM = Stat.GetMaxStamina(_target);
            if (currentSTM < 0)
                currentSTM = 0;
            if (maxSTM < 0)
                maxSTM = 0;

            STM = $"{currentSTM} / {maxSTM}";
            Name = PlayerName.GetDisplayName(Player, _target);
            Might = GetAbilityScore(_target, AbilityType.Might);
            Perception = GetAbilityScore(_target, AbilityType.Perception);
            Vitality = GetAbilityScore(_target, AbilityType.Vitality);
            Willpower = GetAbilityScore(_target, AbilityType.Willpower);
            Agility = GetAbilityScore(_target, AbilityType.Agility);
            Social = GetAbilityScore(_target, AbilityType.Social);

            if (IsPlayerMode)
            {
                var playerId = GetObjectUUID(_target);
                var dbPlayer = DB.Get<Player>(playerId);

                var isRacialBonusAvailable = dbPlayer.RacialStat == AbilityType.Invalid;
                IsMightUpgradeAvailable = IsAttributeUpgradeAvailable(dbPlayer, AbilityType.Might, isRacialBonusAvailable);
                IsPerceptionUpgradeAvailable = IsAttributeUpgradeAvailable(dbPlayer, AbilityType.Perception, isRacialBonusAvailable);
                IsVitalityUpgradeAvailable = IsAttributeUpgradeAvailable(dbPlayer, AbilityType.Vitality, isRacialBonusAvailable);
                IsWillpowerUpgradeAvailable = IsAttributeUpgradeAvailable(dbPlayer, AbilityType.Willpower, isRacialBonusAvailable);
                IsAgilityUpgradeAvailable = IsAttributeUpgradeAvailable(dbPlayer, AbilityType.Agility, isRacialBonusAvailable);
                IsSocialUpgradeAvailable = IsAttributeUpgradeAvailable(dbPlayer, AbilityType.Social, isRacialBonusAvailable);
            }

            RefreshCharacterStatsList();
        }

        private void RefreshEquipmentStats()
        {
            // Builds a damage estimate using the player's stats as a baseline.
            (string, string) GetCombatInfo(uint item)
            {
                var itemType = GetBaseItemType(item);
                var skill = Skill.GetSkillTypeByBaseItem(itemType);
                int skillRank;

                if (GetIsPC(_target))
                {
                    var playerId = GetObjectUUID(_target);
                    var dbPlayer = DB.Get<Player>(playerId);
                    skillRank = dbPlayer.Skills[skill].Rank;
                }
                else
                {
                    var npcStats = Stat.GetNPCStats(_target);
                    skillRank = npcStats.Level;
                }

                var damageAbility = Combat.GetWeaponDamageAbilityType(_target, itemType);
                var damageStat = GetAbilityScore(_target, damageAbility);
                var dmg = Item.GetDMG(item) + Combat.GetMiscDMGBonus(_target, itemType);
                var dmgText = $"{dmg} DMG";
                var attack = Stat.GetAttack(_target, damageAbility, skill);
                var defense = Stat.CalculateDefense(damageStat, skillRank, 0);
                var (min, max) = Combat.CalculateDamageRange(attack, dmg, damageStat, defense, damageStat, 0);
                var tooltip = $"Est. Damage: {min} - {max}";

                return (dmgText, tooltip);
            }

            var mainHand = GetItemInSlot(InventorySlot.RightHand, _target);
            var offHand = GetItemInSlot(InventorySlot.LeftHand, _target);
            var forceAccuracyWeapon = SelectForceAccuracyWeapon(mainHand, offHand, GetIsObjectValid(mainHand));
            var mainHandType = GetBaseItemType(mainHand);
            var attackDelayInfo = GetAttackDelayInfo();
            AttackDelay = attackDelayInfo.Value;
            AttackDelayTooltip = attackDelayInfo.Tooltip;

            if (GetIsObjectValid(mainHand))
            {
                var dmgInfo = GetCombatInfo(mainHand);
                MainHandDMG = dmgInfo.Item1;
                MainHandTooltip = dmgInfo.Item2;
            }
            else
            {
                MainHandDMG = "-";
                MainHandTooltip = "Est. Damage: N/A";
            }

            if (GetIsObjectValid(offHand))
            {
                var dmgInfo = GetCombatInfo(offHand);
                OffHandDMG = dmgInfo.Item1;
                OffHandTooltip = dmgInfo.Item2;
            }
            else
            {
                OffHandDMG = "-";
                OffHandTooltip = "Est. Damage: N/A";
            }

            AbilityType damageStat;
            AbilityType accuracyStatOverride;

            if (BeastMastery.IsPlayerBeast(_target))
            {
                var beastType = BeastMastery.GetBeastType(_target);
                var beastDetails = BeastMastery.GetBeastDetail(beastType);
                damageStat = beastDetails.DamageStat;
                accuracyStatOverride = beastDetails.AccuracyStat;
                mainHand = GetItemInSlot(InventorySlot.CreatureArmor, _target);
            }
            else
            {
                damageStat = Combat.GetWeaponDamageAbilityType(_target, mainHandType);
                accuracyStatOverride = AbilityType.Invalid;

            }

            var mainHandSkill = Skill.GetSkillTypeByBaseItem(mainHandType);
            Attack = Stat.GetAttack(_target, damageStat, mainHandSkill);
            ForceAttack = Stat.GetAttack(_target, AbilityType.Willpower, SkillType.Force);
            PhysicalDefense = Stat.GetDefense(_target, CombatDamageType.Physical, AbilityType.Vitality);
            ForceDefense = Stat.GetDefense(_target, CombatDamageType.Force, AbilityType.Willpower);

            WeaponAccuracy = Stat.GetAccuracy(_target, mainHand, accuracyStatOverride, SkillType.Invalid);
            ForceAccuracy = Stat.GetAccuracy(
                _target,
                forceAccuracyWeapon,
                AbilityType.Willpower,
                SkillType.Force,
                ignoreWeaponAccuracyStatOverride: true);
            Evasion = Stat.GetEvasion(_target, SkillType.Invalid);

            RefreshResistances();
            RefreshCraftingStats();
            RefreshCharacterStatsList();
        }

        private static uint SelectForceAccuracyWeapon(uint mainHand, uint offHand, bool isMainHandValid)
        {
            return isMainHandValid ? mainHand : offHand;
        }

        private (string Value, string Tooltip) GetAttackDelayInfo()
        {
            var attackSkillType = Combat.GetEquippedWeaponSkillType(_target);
            StatusEffect.TryGetLimitedAttackDelayReduction(
                _target,
                attackSkillType,
                out var limitedAttackDelayReductionPercent,
                out _);
            var attackerDelayMilliseconds = Combat.CalculateAttackDelay(
                _target,
                limitedAttackDelayReductionPercent);
            var useDefaultMinimumDelay = Combat.HasNextAutoAttackNoDelay(_target, attackSkillType);
            var effectiveDelayMilliseconds = Combat.CalculateEffectiveAttackDelay(attackerDelayMilliseconds, useDefaultMinimumDelay);
            var attackerDelaySeconds = attackerDelayMilliseconds / 1000f;
            var baseDelaySeconds = Combat.BaseAttackDelayMilliseconds / 1000f;
            var effectiveDelaySeconds = effectiveDelayMilliseconds / 1000f;
            var swingDelaySeconds = Combat.CalculateAttackSwingDelay(effectiveDelayMilliseconds) / 1000f;

            string tooltip;
            if (useDefaultMinimumDelay)
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s (next swing at {swingDelaySeconds:0.##}s resolves extra attacks at your fastest possible speed)";
            else if (attackerDelayMilliseconds <= Combat.BaseAttackDelayMilliseconds)
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s ({baseDelaySeconds:0.##}s default minimum)";
            else if (effectiveDelayMilliseconds < Combat.BaseAttackDelayMilliseconds)
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s (swings every {swingDelaySeconds:0.##}s resolve extra attacks)";
            else
                tooltip = $"Est. Delay: {effectiveDelaySeconds:0.##}s ({attackerDelaySeconds:0.##}s attacker - {baseDelaySeconds:0.##}s default)";

            return (
                $"{effectiveDelaySeconds:0.##}s",
                tooltip
            );
        }

        private void RefreshCharacterStatsList()
        {
            var rows = new List<StatEntry>();
            void AddStat(string name, string value, string tooltip)
            {
                rows.Add(new StatEntry(name, value, tooltip));
            }

            var combatProfile = GetPrimaryCombatProfile();

            AddStat("HP Regen", GetHPRegenValue().ToString(), "Amount of HP restored automatically by natural regeneration.");
            AddStat("FP Regen", GetFPRegenValue().ToString(), "Amount of FP restored automatically by natural regeneration.");
            AddStat("STM Regen", GetStaminaRegenValue().ToString(), "Amount of STM restored automatically by natural regeneration.");
            AddStat("Combat Readiness", FormatPercent(Stat.GetCombatReadinessPercent(_target)), "Increases activated ability damage, healing, and temporary HP. Does not reduce cooldowns.");
            AddStat("Melee Deflection", FormatPercent(Stat.GetMeleeDeflectionChance(_target)), "Chance to negate a hostile melee weapon auto-attack while wielding a weapon without a shield.");
            AddStat("Ranged Deflection", FormatPercent(Stat.GetRangedDeflectionChance(_target)), "Chance to negate a hostile ranged weapon auto-attack while wielding a weapon without a shield.");
            AddStat("Shield Deflection", FormatPercent(Stat.GetShieldDeflectionChance(_target)), "Chance to negate either a hostile melee or ranged weapon auto-attack while equipped with a shield. Shield Deflection replaces weapon deflection while the shield is equipped.");
            AddStat("Guard", FormatPercent(Stat.GetGuardChance(_target)), "Chance to reduce damage and increase enmity gain.");
            AddStat("Guard Reduction", FormatPercent(Combat.GetGuardDamageReductionPercent(_target)), "Amount of damage removed from a hit when Guard succeeds.");
            AddStat("Phys. Taken", FormatPercent(GetDamageTakenPercent(CombatDamageType.Physical)), "Incoming physical damage modifier after damage-taken effects. Lower is better.");
            AddStat("Force Taken", FormatPercent(GetDamageTakenPercent(CombatDamageType.Force)), "Incoming Force damage modifier after damage-taken effects. Lower is better.");
            AddStat("Physical DEF %", FormatPercent(Stat.GetDefensePercentAdjustment(_target, CombatDamageType.Physical)), "Bonus or penalty applied to Physical DEF. Already included in the Physical DEF shown on the Attributes tab.");
            AddStat("Force DEF %", FormatPercent(Stat.GetDefensePercentAdjustment(_target, CombatDamageType.Force)), "Bonus or penalty applied to Force DEF. Already included in the Force DEF shown on the Attributes tab.");
            AddStat("Ability Accuracy", FormatPercent(Stat.GetStatAdjustment(_target, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment)), "Direct percentage-point change to hit chance for weapon-skill and Force-skill ability hit checks only. Does not affect Mimicry abilities or the underlying Accuracy rating.");
            AddStat("Accuracy %", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AccuracyPercentAdjustment)), "Percentage bonus or penalty applied to the underlying Accuracy rating for attacks and ability hit checks, including Force and Mimicry. It is not a direct percentage-point change to hit chance and is already included in the Weapon Accuracy and Force Accuracy ratings shown on the Attributes tab.");
            AddStat("Evasion %", FormatPercent(Stat.GetStatAdjustment(_target, StatType.EvasionPercentAdjustment)), "Bonus or penalty applied to Evasion. Already included in the Evasion shown on the Attributes tab.");
            AddStat("Attack %", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AttackPercentAdjustment)), "Bonus or penalty applied to Attack when using physical attacks and abilities.");
            AddStat("Force Attack %", FormatPercent(Stat.GetStatAdjustment(_target, StatType.ForceAttackPercentAdjustment)), "Bonus or penalty applied to Attack when using Force-typed attacks and abilities.");
            AddStat("Critical Rate", FormatPercent(GetCriticalRate(combatProfile.Skill)), "Increases the chance to score a critical hit. Actual chance varies by target Vitality.");
            AddStat("Assault Gadget Crit", FormatPercent(GetAssaultGadgetCriticalRate()), "Current Assault Gadget ability critical chance before target-specific bonuses. Includes the 5% baseline, Gadget Harness, Tactical Uplink, and other Devices ability bonuses; capped at 50%.");
            AddStat("Critical Damage", FormatPercent(Stat.GetStatAdjustment(_target, StatType.CriticalDamagePercentAdjustment)), "Increases the amount of damage a critical hit deals.");
            AddStat("Damage Dealt", FormatPercent(Stat.GetStatAdjustment(_target, StatType.DamageDealtPercentAdjustment)), "Adjusts all outgoing damage.");
            AddStat("Weapon/Force Damage", FormatPercent(Stat.GetStatAdjustment(_target, StatType.WeaponAndForceDamageDealtPercentAdjustment)), "Adjusts outgoing weapon and Force damage. Stacks with Damage Dealt.");
            AddHighResourceAbilityDamageStats(AddStat);
            AddStat("Healing Received", FormatPercent(Stat.GetStatAdjustment(_target, StatType.HealingReceivedPercentAdjustment)), "Adjusts the amount of healing you receive from all sources.");
            AddStat("Enmity", FormatPercent(Stat.GetStatAdjustment(_target, StatType.EnmityPercentAdjustment)), "Increases or decreases the rate at which enmity is acquired.");
            AddStat("FP Cost", FormatPercent(Stat.GetStatAdjustment(_target, StatType.FPCostPercentAdjustment)), "Adjusts the FP cost of abilities. Lower is better.");
            AddStat("STM Cost", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AbilityStaminaCostPercentAdjustment)), "Adjusts the Stamina cost of abilities. Lower is better.");
            AddStat("Haste", FormatPercent(Combat.CalculateAttackDelayReduction(_target)), "Increases attack speed. Negative values slow attacks.");
            AddStat("Off-Hand Haste", FormatPercent(Combat.CalculateOffhandAttackDelayReduction(_target)), "Increases off-hand attack speed. Only applies while dual wielding.");
            AddStat("Ranged Evasion", FormatPercent(Stat.GetStatAdjustment(_target, StatType.RangedEvasionPercentAdjustment)), "Evasion adjustment against ranged attacks.");
            AddStat("Slow", GetEffectStateLabel(EffectTypeScript.Slow), "Reduces attack speed.");
            AddStat("Paralysis", GetEffectStateLabel(EffectTypeScript.Paralyze), "Prevents auto attacks and other actions.");
            AddStat("Movement Speed", FormatMultiplier(Stat.GetMovementSpeedMultiplier(_target)), "Increases or decreases your movement speed.");
            AddStat("Force Evasion", FormatPercent(GetForceEvasion()), "Percent chance to completely evade a detrimental force ability.");
            AddStat("Force Affinity", Perk.GetForceAffinity(_target).ToString(), "Range: -10 (Dark) to +10 (Light). Matching-side powers gain 5% magnitude per point, up to +50%, and +5% hit chance at full affinity; opposing powers lose the same. Affinity does not change duration, which remains subject to resistance and duration modifiers.");
            AddStat("Detection", Stat.GetDetection(_target).ToString(), "PER + WIL plus equipment, perk, and status-effect bonuses; Detect mode adds +5.");
            AddStat("Stealth", Stat.GetStealth(_target).ToString(), "Twice AGI plus equipment, perk, and status-effect bonuses.");
            AddStat("Experience", FormatPercent(Stat.GetStatAdjustment(_target, StatType.ExperiencePercentAdjustment)), "Bonus or penalty applied to experience gained from skill use.");

            StatsTable.Refresh(this, rows);
        }

        private void AddHighResourceAbilityDamageStats(Action<string, string, string> addStat)
        {
            var flatBonus = Stat.GetStatAdjustment(
                _target,
                StatType.HighFPAndStaminaAbilityDamageBonus);
            var flatThreshold = Stat.GetStatAdjustment(
                _target,
                StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent);
            if (flatBonus > 0 && flatThreshold > 0)
            {
                var active = Combat.IsCurrentFPAndStaminaAtOrAbovePercent(_target, flatThreshold);
                addStat(
                    "High-Resource Ability DMG",
                    active ? $"Active (+{flatBonus} DMG)" : $"Inactive ({flatThreshold}% required)",
                    $"Combined conditional bonus: hostile combat abilities gain +{flatBonus} DMG while FP and STM are both at least {flatThreshold}%.");
            }

            var percentBonus = Stat.GetStatAdjustment(
                _target,
                StatType.HighFPAndStaminaAbilityDamagePercentAdjustment);
            var percentThreshold = Stat.GetStatAdjustment(
                _target,
                StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent);
            if (percentBonus > 0 && percentThreshold > 0)
            {
                var active = Combat.IsCurrentFPAndStaminaAtOrAbovePercent(_target, percentThreshold);
                addStat(
                    "Balanced Attunement",
                    active ? $"Active (+{percentBonus}% DMG)" : $"Inactive ({percentThreshold}% required)",
                    $"Hostile combat abilities deal +{percentBonus}% damage while FP and STM are both at least {percentThreshold}%.");
            }
        }

        private (AbilityType DamageAbility, AbilityType AccuracyAbilityOverride, SkillType Skill, uint AccuracyWeapon) GetPrimaryCombatProfile()
        {
            var mainHand = GetItemInSlot(InventorySlot.RightHand, _target);
            var mainHandType = GetBaseItemType(mainHand);
            var skill = Skill.GetSkillTypeByBaseItem(mainHandType);

            if (BeastMastery.IsPlayerBeast(_target))
            {
                var beastType = BeastMastery.GetBeastType(_target);
                var beastDetails = BeastMastery.GetBeastDetail(beastType);
                var creatureArmor = GetItemInSlot(InventorySlot.CreatureArmor, _target);

                return (beastDetails.DamageStat, beastDetails.AccuracyStat, skill, creatureArmor);
            }

            return (Combat.GetWeaponDamageAbilityType(_target, mainHandType), AbilityType.Invalid, skill, mainHand);
        }

        private bool IsPlayerCharacterTarget()
        {
            return GetIsPC(_target) && !GetIsDM(_target);
        }

        private Player GetPlayerEntity()
        {
            return DB.Get<Player>(GetObjectUUID(_target));
        }

        private int GetHPRegenValue()
        {
            var bonus = Stat.GetStatAdjustment(_target, StatType.HPRegen);
            if (!IsPlayerCharacterTarget())
                return bonus;

            var dbPlayer = GetPlayerEntity();
            return dbPlayer.HPRegen + GetAbilityScore(_target, AbilityType.Vitality) + bonus;
        }

        private int GetFPRegenValue()
        {
            var bonus = Stat.GetStatAdjustment(_target, StatType.FPRegen);
            if (!IsPlayerCharacterTarget())
                return bonus;

            var dbPlayer = GetPlayerEntity();
            return 1 + dbPlayer.FPRegen + GetAbilityScore(_target, AbilityType.Willpower) / 4 + bonus;
        }

        private int GetStaminaRegenValue()
        {
            var bonus = Stat.GetStatAdjustment(_target, StatType.StaminaRegen);
            if (!IsPlayerCharacterTarget())
                return bonus;

            var dbPlayer = GetPlayerEntity();
            return 1 + dbPlayer.STMRegen + GetAbilityScore(_target, AbilityType.Might) / 4 + bonus;
        }

        private int GetCriticalRate(SkillType skillType)
        {
            var criticalRateAdjustment = Stat.GetStatAdjustment(
                _target,
                StatType.CriticalRatePercentAdjustment);
            criticalRateAdjustment += Combat.GetSkillCriticalRatePercentAdjustment(_target, skillType);

            return Combat.CalculateCriticalRate(
                GetAbilityScore(_target, AbilityType.Perception),
                GetAbilityScore(_target, AbilityType.Vitality),
                GetSkillRank(skillType),
                criticalRateAdjustment);
        }

        private int GetAssaultGadgetCriticalRate()
        {
            return Combat.GetAbilityCriticalRate(
                _target,
                SkillType.Devices,
                false,
                Stat.GetStatAdjustment(_target, StatType.AssaultGadgetCriticalRatePercentAdjustment));
        }

        private int GetSkillRank(SkillType skillType)
        {
            if (IsPlayerCharacterTarget())
            {
                if (skillType == SkillType.Invalid)
                    return 0;

                var dbPlayer = GetPlayerEntity();
                return dbPlayer.Skills.TryGetValue(skillType, out var skill)
                    ? skill.Rank
                    : 0;
            }

            var npcStats = Stat.GetNPCStats(_target);
            return npcStats.Skills.TryGetValue(skillType, out var rank)
                ? rank
                : npcStats.Level;
        }

        private string GetEffectStateLabel(EffectTypeScript effectType)
        {
            for (var effect = GetFirstEffect(_target); GetIsEffectValid(effect); effect = GetNextEffect(_target))
            {
                if (GetEffectType(effect) == effectType)
                    return "Active";
            }

            return "Inactive";
        }

        private int GetForceEvasion()
        {
            var skillType = Stat.GetStatAdjustment(_target, StatType.IncomingAbilityHitChancePercentAdjustmentSkillType);
            if (skillType != (int)SkillType.Force)
                return 0;

            return Math.Max(0, -Stat.GetStatAdjustment(_target, StatType.IncomingAbilityHitChancePercentAdjustment));
        }

        private int GetDamageTakenPercent(CombatDamageType damageType)
        {
            var typeAdjustment = damageType switch
            {
                CombatDamageType.Physical =>
                    Stat.GetStatAdjustment(_target, StatType.PhysicalDamageTakenPercentAdjustment),
                CombatDamageType.Force =>
                    Stat.GetStatAdjustment(_target, StatType.ForceDamageTakenPercentAdjustment),
                _ => 0
            };
            var leadershipAdjustment = damageType switch
            {
                CombatDamageType.Physical =>
                    Stat.GetStatAdjustment(_target, StatType.LeadershipPhysicalDamageTakenPercentAdjustment),
                CombatDamageType.Force =>
                    Stat.GetStatAdjustment(_target, StatType.LeadershipForceDamageTakenPercentAdjustment),
                _ => Stat.GetStatAdjustment(_target, StatType.LeadershipOtherDamageTakenPercentAdjustment)
            };

            var percent = ApplyDamageTakenPercentAdjustment(100, typeAdjustment);
            percent = ApplyDamageTakenPercentAdjustment(percent, leadershipAdjustment);
            return ApplyDamageTakenPercentAdjustment(
                percent,
                Stat.GetStatAdjustment(_target, StatType.DamageTakenPercentAdjustment));
        }

        private static int ApplyDamageTakenPercentAdjustment(int percent, int adjustment)
        {
            if (percent <= 0 || adjustment <= -100)
                return 0;

            if (adjustment == 0)
                return percent;

            return Math.Max(0, percent + (int)Math.Ceiling(percent * (adjustment / 100f)));
        }

        private static string FormatPercent(int value)
        {
            return $"{value}%";
        }

        private static string FormatMultiplier(float value)
        {
            return $"{value:0.##}x";
        }

        private static string GetStatusDurationLabel(int score)
        {
            return score switch
            {
                < -60 => "Greatly extended",
                < -25 => "Extended",
                < 0 => "Slightly extended",
                0 => "Full Duration",
                < 25 => "Slightly reduced",
                < 45 => "Reduced",
                < 60 => "Strongly reduced",
                >= Resistance.MaximumResistance => "Immune",
                _ => "Greatly reduced"
            };
        }

        private void RefreshResistances()
        {
            var rows = Resistance.GetAllResistanceTypes().Select(resistanceType =>
            {
                var score = Resistance.GetResistance(_target, resistanceType);
                var takenPercent = (int)Math.Round(Resistance.CalculateResistanceDamageMultiplier(_target, resistanceType) * 100f);

                return new ResistanceEntry(
                    resistanceType.ToString(),
                    score.ToString(),
                    $"{takenPercent}% taken",
                    GetStatusDurationLabel(score));
            });

            ResistancesTable.Refresh(this, rows);
        }

        private void RefreshCraftingStats()
        {
            var rows = new List<CraftEntry>();
            var legacyControl = string.Empty;
            var legacyCraftsmanship = string.Empty;

            var index = 0;
            foreach (var (skillType, detail) in Skill.GetActiveCraftingSkills())
            {
                var control = Stat.CalculateControl(_target, skillType);
                var craft = Stat.CalculateCraftsmanship(_target, skillType);

                rows.Add(new CraftEntry(detail.Name, control.ToString(), craft.ToString()));

                legacyControl += index == 0 ? control.ToString() : $"/{control}";
                legacyCraftsmanship += index == 0 ? craft.ToString() : $"/{craft}";
                index++;
            }

            CraftingTable.Refresh(this, rows);
            Control = legacyControl;
            Craftsmanship = legacyCraftsmanship;
        }

        private void RefreshAttributes()
        {
            if (GetIsPC(_target))
            {
                var playerId = GetObjectUUID(_target);
                var dbPlayer = DB.Get<Player>(playerId);

                SkillRanks = $"{Skill.GetTotalContributingSkillRanks(dbPlayer)} / {Skill.SkillCap}";
                SP = $"{Skill.GetTotalSkillPoints(dbPlayer)} / {Skill.TotalSkillPointCap} ({dbPlayer.UnallocatedSP})";
                APOrLevel = $"{dbPlayer.TotalAPAcquired} / {Skill.APCap} ({dbPlayer.UnallocatedAP})";
            }
            else if (BeastMastery.IsPlayerBeast(_target))
            {
                var beastId = BeastMastery.GetBeastId(_target);
                var dbBeast = DB.Get<Beast>(beastId);

                SP = $"{dbBeast.Level} / {BeastMastery.MaxLevel} ({dbBeast.UnallocatedSP})";
                APOrLevel = $"{dbBeast.Level} / {BeastMastery.MaxLevel}";
                APOrLevelTooltip = $"XP: {dbBeast.XP} / {BeastMastery.GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent)}";
            }
        }

        private void RefreshPortrait()
        {
            PortraitResref = GetPortraitResRef(_target) + "l";
        }

        private void LoadData()
        {
            CharacterType = GetClassByPosition(1, _target) == ClassType.Standard ? "Standard" : "Force Sensitive";
            Race = GetStringByStrRef(Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(_target))), GetGender(_target));
            IsHolocomEnabled = !Space.IsPlayerInSpaceMode(_target);
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

            SkillRanks = $"{Skill.GetTotalContributingSkillRanks(dbPlayer)} / {Skill.SkillCap}";
            SP = $"{Skill.GetTotalSkillPoints(dbPlayer)} / {Skill.TotalSkillPointCap} ({dbPlayer.UnallocatedSP})";
            APOrLevel = $"{dbPlayer.TotalAPAcquired} / {Skill.APCap} ({dbPlayer.UnallocatedAP})";

            RefreshStats();
        }

        public void Refresh(BeastGainXPRefreshEvent payload)
        {
            if (!BeastMastery.IsPlayerBeast(_target))
                return;

            var beastId = BeastMastery.GetBeastId(_target);
            var dbBeast = DB.Get<Beast>(beastId);

            SP = $"{dbBeast.Level} / {BeastMastery.MaxLevel} ({dbBeast.UnallocatedSP})";
            APOrLevel = $"{dbBeast.Level} / {BeastMastery.MaxLevel}";
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
            if (!GetIsPC(_target))
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
    }
}
