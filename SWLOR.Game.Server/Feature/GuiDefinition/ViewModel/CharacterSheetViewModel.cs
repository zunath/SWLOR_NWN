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
using static SWLOR.Game.Server.Composition.GameServices;
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
        private CharacterSheetDamageEstimate CharacterSheetDamageEstimate { get; }
        private CharacterSheetWeaponRatings CharacterSheetWeaponRatings { get; }
        private CharacterSheetWeaponTiming CharacterSheetWeaponTiming { get; }

        public CharacterSheetViewModel(CharacterSheetAttributesTab characterSheetAttributesTab, CharacterSheetDamageEstimate characterSheetDamageEstimate, CharacterSheetWeaponRatings characterSheetWeaponRatings, CharacterSheetWeaponTiming characterSheetWeaponTiming)
        {
            CharacterSheetAttributesTab = characterSheetAttributesTab;
            CharacterSheetDamageEstimate = characterSheetDamageEstimate;
            CharacterSheetWeaponRatings = characterSheetWeaponRatings;
            CharacterSheetWeaponTiming = characterSheetWeaponTiming;
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
            AddStat("Melee Deflection", FormatPercent(CombatRating.GetMeleeDeflectionChance(_target)), "Chance to negate a hostile melee weapon auto-attack while wielding a weapon without a shield.");
            AddStat("Ranged Deflection", FormatPercent(CombatRating.GetRangedDeflectionChance(_target)), "Chance to negate a hostile ranged weapon auto-attack while wielding a weapon without a shield.");
            AddStat("Shield Deflection", FormatPercent(CombatRating.GetShieldDeflectionChance(_target)), "Chance to negate either a hostile melee or ranged weapon auto-attack while equipped with a shield. Shield Deflection replaces weapon deflection while the shield is equipped.");
            AddStat("Guard", FormatPercent(CombatRating.GetGuardChance(_target)), "Chance to reduce damage and increase enmity gain.");
            AddStat("Guard Reduction", FormatPercent(GameServices.CombatDamageTaken.GetGuardDamageReductionPercent(_target)), "Amount of damage removed from a hit when Guard succeeds.");
            AddStat("Phys. Taken", FormatPercent(GetDamageTakenPercent(CombatDamageType.Physical)), "Incoming physical damage modifier after damage-taken effects. Lower is better.");
            AddStat("Force Taken", FormatPercent(GetDamageTakenPercent(CombatDamageType.Force)), "Incoming Force damage modifier after damage-taken effects. Lower is better.");
            AddStat("Physical DEF %", FormatPercent(CombatRating.GetDefensePercentAdjustment(_target, CombatDamageType.Physical)), "Bonus or penalty applied to Physical DEF. Already included in the Physical DEF shown on the Attributes tab.");
            AddStat("Force DEF %", FormatPercent(CombatRating.GetDefensePercentAdjustment(_target, CombatDamageType.Force)), "Bonus or penalty applied to Force DEF. Already included in the Force DEF shown on the Attributes tab.");
            AddStat("Ability Accuracy", FormatPercent(Stat.GetStatAdjustment(_target, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment)), "Direct percentage-point change to hit chance for weapon-skill and Force-skill ability hit checks only. Does not affect Mimicry abilities or the underlying Accuracy rating.");
            AddStat("Accuracy %", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AccuracyPercentAdjustment)), "Percentage bonus or penalty applied to the underlying Accuracy rating for attacks and ability hit checks, including Force and Mimicry. It is not a direct percentage-point change to hit chance and is already included in the Main-hand Accuracy, Off-hand Accuracy, and Force Accuracy ratings shown on the Attributes tab.");
            AddStat("Evasion %", FormatPercent(Stat.GetStatAdjustment(_target, StatType.EvasionPercentAdjustment)), "Bonus or penalty applied to Evasion. Already included in the Evasion shown on the Attributes tab.");
            AddStat("Attack %", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AttackPercentAdjustment)), "General bonus or penalty applied to Attack, including Force Attack.");
            AddStat("Force Attack %", FormatPercent(CombatRating.GetAttackPercentAdjustment(_target, SkillType.Force)), "Combined general and Force-specific Attack bonus or penalty. Already included in Force Attack on the Attributes tab.");
            var offhandWeapon = BeastMastery.IsPlayerBeast(_target) ? OBJECT_INVALID : CharacterSheetWeaponTiming.GetOffhandAttackWeapon(this);
            AddStat("Critical Rate", FormatPercent(GetCriticalRate(combatProfile.Skill)), FormatOffHandTooltip(
                "Increases the chance to score a critical hit. Actual chance varies by target Vitality.",
                GetIsObjectValid(offhandWeapon) ? FormatPercent(GetCriticalRate(Skill.GetWeaponSkill(_target, offhandWeapon))) : null));
            AddStat("Next Ability Crit", FormatPercent(GameServices.CombatAbilityBonus.GetPersistentNextSkillAbilityCriticalRateBonus(_target, combatProfile.Skill)), "Conditional Critical Rate reserved for the next matching ability. It does not affect ordinary auto-attacks and is consumed only when the ability critically hits.");
            AddStat("Assault Gadget Crit", FormatPercent(GetAssaultGadgetCriticalRate()), "Current Assault Gadget ability critical chance before target-specific bonuses. Includes the 5% baseline, Gadget Harness, Tactical Uplink, and other Devices ability bonuses; capped at 50%.");
            AddStat("Gadget DMG", DeviceAbilityEffects.GetAssaultGadgetWeaponDamageEquivalent(_target).ToString(), "DMG added to every Assault Gadget ability, the way weapon DMG is added to weapon abilities; Overload Barrage adds it only to its single-target strike. Rises with Devices rank: 6 below rank 10, then 10, 15, 19, 24, and 28 at ranks 10, 20, 30, 40, and 50. Droids use their level in place of Devices rank.");
            AddStat("Critical Damage", FormatPercent(Stat.GetStatAdjustment(_target, StatType.CriticalDamagePercentAdjustment)), "Increases the amount of damage a critical hit deals.");
            AddStat("Damage Dealt", FormatPercent(Stat.GetStatAdjustment(_target, StatType.DamageDealtPercentAdjustment)), "Adjusts all outgoing damage.");
            AddStat("Weapon/Force Damage", FormatPercent(Stat.GetStatAdjustment(_target, StatType.WeaponAndForceDamageDealtPercentAdjustment)), "Adjusts outgoing weapon and Force damage. Stacks with Damage Dealt.");
            AddHighResourceAbilityDamageStats(AddStat);
            AddStat("Healing Received", FormatPercent(Stat.GetStatAdjustment(_target, StatType.HealingReceivedPercentAdjustment)), "Adjusts the amount of healing you receive from all sources.");
            AddStat("Enmity", FormatPercent(Stat.GetStatAdjustment(_target, StatType.EnmityPercentAdjustment)), "Increases or decreases the rate at which enmity is acquired.");
            AddStat("FP Cost", FormatPercent(Stat.GetStatAdjustment(_target, StatType.FPCostPercentAdjustment)), "Adjusts the FP cost of abilities. Lower is better.");
            AddStat("STM Cost", FormatPercent(Stat.GetStatAdjustment(_target, StatType.AbilityStaminaCostPercentAdjustment)), "Adjusts the Stamina cost of abilities. Lower is better.");
            AddStat("Haste", FormatPercent(GameServices.CombatAttackTiming.CalculateAttackDelayReduction(_target)), "Increases attack speed. Negative values slow attacks.");
            AddStat("Off-Hand Haste", FormatPercent(GameServices.CombatAttackTiming.CalculateOffhandAttackDelayReduction(_target)), "Increases off-hand attack speed. Only applies while dual wielding.");
            AddStat("Single Weapon DMG", FormatPercent(EquipmentPredicates.HasSingleWeapon(_target) ? WeaponDamage.GetSingleWeaponPercent(_target) : 0),
                "Bonus to the item's weapon DMG with one eligible one-handed melee, pistol, or throwing weapon and an empty off hand. Includes the natural bonus and Doublehand; already included in equipped DMG.");
            AddStat("Ranged Evasion", FormatPercent(Stat.GetStatAdjustment(_target, StatType.RangedEvasionPercentAdjustment)), "Evasion adjustment against ranged attacks.");
            AddStat("Slow", GetEffectStateLabel(EffectTypeScript.Slow), "Reduces attack speed.");
            AddStat("Paralysis", GetEffectStateLabel(EffectTypeScript.Paralyze), "Prevents auto attacks and other actions.");
            AddStat("Movement Speed", FormatMultiplier(CreatureStat.GetMovementSpeedMultiplier(_target)), "Increases or decreases your movement speed.");
            AddStat("Force Evasion", FormatPercent(GetForceEvasion()), "Percent chance to completely evade a detrimental force ability.");
            AddStat("Force Affinity", Perk.GetForceAffinity(_target).ToString(), "-10 Dark to +10 Light. Strengthens matching powers; weakens opposing powers.");
            AddStat("Detection", Stat.GetDetection(_target).ToString(), "PER + WIL plus equipment, perk, and status-effect bonuses; Detect mode adds +5.");
            AddStat("Stealth", Stat.GetStealth(_target).ToString(), "Twice AGI plus equipment, perk, and status-effect bonuses.");
            AddStat("Experience", FormatPercent(Stat.GetStatAdjustment(_target, StatType.ExperiencePercentAdjustment)), "Bonus or penalty applied to experience gained from skill use.");

            StatsTable.Refresh(this, rows);
        }

        private void AddHighResourceAbilityDamageStats(Action<string, string, string> addStat)
        {
            AddConditionalDamage(StatType.HighFPAndStaminaAbilityDamageBonus,
                StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, " DMG");
            AddConditionalDamage(StatType.HighFPAndStaminaAbilityDamagePercentAdjustment,
                StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent, "% DMG");

            void AddConditionalDamage(StatType payload, StatType thresholdStat, string units)
            {
                foreach (var group in Stat.GetStatSources(_target, payload)
                             .Where(source => source[payload] > 0 && source[thresholdStat] > 0)
                             .GroupBy(source => source[thresholdStat]).OrderBy(group => group.Key))
                {
                    var threshold = group.Key;
                    var bonus = group.Sum(source => source[payload]);
                    var active = GameServices.CombatResources.IsCurrentFPAndStaminaAbovePercent(_target, threshold);
                    addStat($"Ability DMG above {threshold}% FP/STM",
                        active ? $"Active (+{bonus}{units})" : $"Inactive ({threshold}% required)",
                        $"Hostile combat abilities gain +{bonus}{units} while FP and STM are both above {threshold}%.");
                }
            }

        }

        private (AbilityType DamageAbility, AbilityType AccuracyAbilityOverride, SkillType Skill, uint AccuracyWeapon) GetPrimaryCombatProfile()
        {
            var mainHand = GetItemInSlot(InventorySlot.RightHand, _target);
            var mainHandType = GetBaseItemType(mainHand);
            var skill = Skill.GetWeaponSkill(_target, mainHand);

            if (BeastMastery.IsPlayerBeast(_target))
            {
                var beastType = BeastMastery.GetBeastType(_target);
                var beastDetails = BeastMastery.GetBeastDetail(beastType);
                var creatureArmor = GetItemInSlot(InventorySlot.CreatureArmor, _target);

                return (beastDetails.DamageStat, beastDetails.AccuracyStat, skill, creatureArmor);
            }

            return (CharacterSheetDamageEstimate.GetEquipmentDamageAbility(_target, mainHand), AbilityType.Invalid, skill, mainHand);
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
            return NaturalRegeneration.BaseStaminaRegenPerThirtySeconds + dbPlayer.STMRegen + GetAbilityScore(_target, AbilityType.Might) / 4 + bonus;
        }

        private int GetCriticalRate(SkillType skillType)
        {
            var criticalRateAdjustment = Stat.GetStatAdjustment(
                _target,
                StatType.CriticalRatePercentAdjustment);
            criticalRateAdjustment += GameServices.CombatDamageModifiers.GetSkillCriticalRatePercentAdjustment(_target, skillType);

            return CombatRules.CalculateCriticalRate(
                GetAbilityScore(_target, AbilityType.Perception),
                GetAbilityScore(_target, AbilityType.Vitality),
                GetSkillRank(skillType),
                criticalRateAdjustment);
        }

        private int GetAssaultGadgetCriticalRate()
        {
            return GameServices.CombatAbilityHit.GetAbilityCriticalRate(
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
                    Stat.GetStatAdjustment(_target, StatType.LeadershipPhysicalDamageTakenPercentAdjustment) +
                    Stat.GetStatAdjustment(_target, StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment),
                CombatDamageType.Force =>
                    Stat.GetStatAdjustment(_target, StatType.LeadershipForceDamageTakenPercentAdjustment) +
                    Stat.GetStatAdjustment(_target, StatType.LeadershipRecoveryForceDamageTakenPercentAdjustment),
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
        void ICharacterSheetHost.RefreshCharacterStatsList() => RefreshCharacterStatsList();
    }
}
