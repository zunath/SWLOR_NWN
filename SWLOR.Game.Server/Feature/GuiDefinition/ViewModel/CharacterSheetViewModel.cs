using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CharacterSheetViewModel: GuiViewModelBase<CharacterSheetViewModel, CharacterSheetPayload>,
        IGuiRefreshable<ChangePortraitRefreshEvent>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<PlayerStatusRefreshEvent>,
        IGuiRefreshable<StatusEffectReceivedRefreshEvent>,
        IGuiRefreshable<StatusEffectRemovedRefreshEvent>,
        IGuiRefreshable<BeastGainXPRefreshEvent>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>
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
        private bool _isSynchronizingTabRows;

        public int SelectedTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                RefreshTabRowSelection();

                RestoreSelectedTabPartial();
            }
        }

        public int TopTabId
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (_isSynchronizingTabRows || value < 0)
                    return;

                SelectTab(value == 0 ? AttributesTabId : StatsTabId);
            }
        }

        public int BottomTabId
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (_isSynchronizingTabRows || value < 0)
                    return;

                SelectTab(value == 0 ? ResistancesTabId : CraftingTabId);
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

        public string StatusResistances
        {
            get => Get<string>();
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

        public int DefensePhysical
        {
            get => Get<int>();
            set => Set(value);
        }

        public int DefenseForce
        {
            get => Get<int>();
            set => Set(value);
        }

        public string DefenseElemental
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Accuracy
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

        public Action OnClickSkills() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Skills);
        };

        public Action OnClickPerks() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Perks);
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

            Dialog.StartConversation(Player, Player, nameof(HoloComDialog));
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
                try
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
                }
                finally
                {
                    RestoreSelectedTabPartial();
                }
            }, RestoreSelectedTabPartial);
        }

        private void SelectTab(int tabId)
        {
            if (SelectedTabId == tabId)
            {
                RefreshTabRowSelection();
                RestoreSelectedTabPartial();
                return;
            }

            SelectedTabId = tabId;
        }

        private void RefreshTabRowSelection()
        {
            _isSynchronizingTabRows = true;

            TopTabId = SelectedTabId switch
            {
                AttributesTabId => 0,
                StatsTabId => 1,
                _ => -1
            };

            BottomTabId = SelectedTabId switch
            {
                ResistancesTabId => 0,
                CraftingTabId => 1,
                _ => -1
            };

            _isSynchronizingTabRows = false;
        }

        private void RestoreSelectedTabPartial()
        {
            void RefreshSelectedTabData()
            {
                if (!GetIsObjectValid(_target))
                    return;

                if (SelectedTabId == StatsTabId)
                {
                    RefreshCharacterStatsList();
                }
                else if (SelectedTabId == ResistancesTabId)
                {
                    RefreshResistances();
                }
                else if (SelectedTabId == CraftingTabId)
                {
                    RefreshCraftingStats();
                }
            }

            void ApplySelectedTabPartial()
            {
                RefreshSelectedTabData();
                ChangePartialView(TabContentPartialElement, GetTabPartialName(SelectedTabId));
                RefreshSelectedTabData();
            }

            // Use the same root redraw path as modal close/open before replacing the nested tab panel.
            ChangePartialView("_window_", "%%WINDOW_MAIN%%");
            ApplySelectedTabPartial();
            // NUI can drop nested partial layouts while its parent is being redrawn.
            // Reapply on the next tick so tab switches use the same refresh path as modal swaps.
            DelayCommand(0.0f, ApplySelectedTabPartial);
        }

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
            Name = GetName(_target);
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
            (string, string) GetCombatInfo( uint item)
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
            DefensePhysical = Stat.GetDefense(_target, CombatDamageType.Physical, AbilityType.Vitality);
            DefenseForce = Stat.GetDefense(_target, CombatDamageType.Force, AbilityType.Willpower);

            Accuracy = Stat.GetAccuracy(_target, mainHand, accuracyStatOverride, SkillType.Invalid);
            Evasion = Stat.GetEvasion(_target, SkillType.Invalid);

            RefreshResistances();
            RefreshCraftingStats();
            RefreshCharacterStatsList();
        }

        private (string Value, string Tooltip) GetAttackDelayInfo()
        {
            var weaponDelayMilliseconds = Combat.CalculateAttackDelay(_target);
            var totalDelayMilliseconds = weaponDelayMilliseconds + Combat.BaseAttackDelayMilliseconds;
            var weaponDelaySeconds = weaponDelayMilliseconds / 1000f;
            var baseDelaySeconds = Combat.BaseAttackDelayMilliseconds / 1000f;
            var totalDelaySeconds = totalDelayMilliseconds / 1000f;

            return (
                $"{totalDelaySeconds:0.##}s",
                $"Est. Delay: {totalDelaySeconds:0.##}s ({weaponDelaySeconds:0.##}s weapon + {baseDelaySeconds:0.##}s animation)"
            );
        }

        private void RefreshCharacterStatsList()
        {
            var names = new GuiBindingList<string>();
            var values = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();

            void AddStat(string name, string value, string tooltip)
            {
                names.Add(name);
                values.Add(value);
                tooltips.Add(tooltip);
            }

            var combatProfile = GetPrimaryCombatProfile();

            AddStat("HP Regen", GetHPRegenValue().ToString(), "Amount of HP restored automatically by natural regeneration.");
            AddStat("FP Regen", GetFPRegenValue().ToString(), "Amount of FP restored automatically by natural regeneration.");
            AddStat("STM Regen", GetStaminaRegenValue().ToString(), "Amount of STM restored automatically by natural regeneration.");
            AddStat("Recast Reduction", FormatPercent(Recast.GetRecastReductionPercent(_target)), "Reduces the time in between ability usage.");
            AddStat("Shield Deflection", FormatPercent(Stat.GetShieldDeflectionChance(_target)), "Ability to deflect attacks with a shield.");
            AddStat("Attack Deflection", FormatPercent(Stat.GetAttackDeflectionChance(_target)), "Ability to deflect attacks.");
            AddStat("Guard", FormatPercent(Stat.GetGuardChance(_target)), "Chance to reduce damage by 20% and increase enmity gain.");
            AddStat("Phys. Taken", FormatPercent(GetDamageTakenPercent(CombatDamageType.Physical)), "Incoming physical damage modifier after damage-taken effects. Lower is better.");
            AddStat("Force Taken", FormatPercent(GetDamageTakenPercent(CombatDamageType.Force)), "Incoming Force damage modifier after damage-taken effects. Lower is better.");
            AddStat("Critical Rate", FormatPercent(GetCriticalRate(combatProfile.Skill)), "Increases the chance to score a critical hit. Actual chance varies by target Vitality.");
            AddStat("Critical Damage", FormatPercent(Stat.GetStatAdjustment(_target, StatType.CriticalDamagePercentAdjustment)), "Increases the amount of damage a critical hit deals.");
            AddStat("Enmity", FormatPercent(Stat.GetStatAdjustment(_target, StatType.EnmityPercentAdjustment)), "Increases or decreases the rate at which enmity is acquired.");
            AddStat("Haste", FormatPercent(Combat.CalculateAttackDelayReduction(_target)), "Increases attack speed.");
            AddStat("Slow", GetEffectStateLabel(EffectTypeScript.Slow), "Reduces attack speed.");
            AddStat("Paralysis", GetEffectStateLabel(EffectTypeScript.Paralyze), "Prevents auto attacks and other actions.");
            AddStat("Movement Speed", FormatMultiplier(Stat.GetMovementSpeedMultiplier(_target)), "Increases or decreases your movement speed.");
            AddStat("Force Evasion", FormatPercent(GetForceEvasion()), "Percent chance to completely evade a detrimental force ability.");
            AddStat("Force Affinity", Perk.GetForceAffinity(_target).ToString(), "Affects Force ability effectiveness based on type. Range: -10 to 10. Negative represents Dark-side and positive represents Light-side.");

            StatNames = names;
            StatValues = values;
            StatTooltips = tooltips;
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
            return Combat.CalculateCriticalRate(
                GetAbilityScore(_target, AbilityType.Perception),
                GetAbilityScore(_target, AbilityType.Vitality),
                GetSkillRank(skillType),
                Stat.GetStatAdjustment(_target, StatType.CriticalRatePercentAdjustment));
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
                CombatDamageType.Physical => Stat.GetStatAdjustment(_target, StatType.PhysicalDamageTakenPercentAdjustment),
                CombatDamageType.Force => Stat.GetStatAdjustment(_target, StatType.ForceDamageTakenPercentAdjustment),
                _ => 0
            };

            var percent = ApplyDamageTakenPercentAdjustment(100, typeAdjustment);
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
                <= 0 => "Full Duration",
                < 25 => "Slightly reduced",
                < 45 => "Reduced",
                < 60 => "Strongly reduced",
                _ => "Greatly reduced"
            };
        }

        private static string GetTabPartialName(int tabId)
        {
            return tabId switch
            {
                StatsTabId => StatsTabPartial,
                ResistancesTabId => ResistancesTabPartial,
                CraftingTabId => CraftingTabPartial,
                _ => AttributesTabPartial
            };
        }

        private void RefreshResistances()
        {
            var names = new GuiBindingList<string>();
            var scores = new GuiBindingList<string>();
            var damageTaken = new GuiBindingList<string>();
            var statusDurations = new GuiBindingList<string>();

            foreach (var resistanceType in Resistance.GetAllResistanceTypes())
            {
                var score = Resistance.GetResistance(_target, resistanceType);
                var takenPercent = (int)Math.Round(Resistance.CalculateResistanceDamageMultiplier(_target, resistanceType) * 100f);

                names.Add(resistanceType.ToString());
                scores.Add(score.ToString());
                damageTaken.Add($"{takenPercent}% taken");
                statusDurations.Add(GetStatusDurationLabel(score));
            }

            ResistanceNames = names;
            ResistanceScores = scores;
            ResistanceDamageTaken = damageTaken;
            ResistanceStatusDurations = statusDurations;

            var fireDefense = Resistance.GetResistance(_target, ResistanceType.Fire);
            var poisonDefense = Resistance.GetResistance(_target, ResistanceType.Poison);
            var electricalDefense = Resistance.GetResistance(_target, ResistanceType.Electrical);
            var iceDefense = Resistance.GetResistance(_target, ResistanceType.Ice);
            DefenseElemental = $"{fireDefense}/{poisonDefense}/{electricalDefense}/{iceDefense}";

            StatusResistances = Resistance.GetResistance(_target, ResistanceType.Mind) + "/" +
                                Resistance.GetResistance(_target, ResistanceType.Mobility) + "/" +
                                Resistance.GetResistance(_target, ResistanceType.Trauma) + "/" +
                                Resistance.GetResistance(_target, ResistanceType.Disruption);
        }

        private void RefreshCraftingStats()
        {
            var names = new GuiBindingList<string>();
            var controls = new GuiBindingList<string>();
            var craftsmanship = new GuiBindingList<string>();
            var legacyControl = string.Empty;
            var legacyCraftsmanship = string.Empty;

            var index = 0;
            foreach (var (skillType, detail) in Skill.GetActiveCraftingSkills())
            {
                var control = Stat.CalculateControl(_target, skillType);
                var craft = Stat.CalculateCraftsmanship(_target, skillType);

                names.Add(detail.Name);
                controls.Add(control.ToString());
                craftsmanship.Add(craft.ToString());

                legacyControl += index == 0 ? control.ToString() : $"/{control}";
                legacyCraftsmanship += index == 0 ? craft.ToString() : $"/{craft}";
                index++;
            }

            CraftNames = names;
            CraftControls = controls;
            CraftCraftsmanship = craftsmanship;
            Control = legacyControl;
            Craftsmanship = legacyCraftsmanship;
        }

        private void RefreshAttributes()
        {
            if (GetIsPC(_target))
            {
                var playerId = GetObjectUUID(_target);
                var dbPlayer = DB.Get<Player>(playerId);

                SP = $"{dbPlayer.TotalSPAcquired} / {Skill.SkillCap} ({dbPlayer.UnallocatedSP})";
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

        public void Refresh(SkillXPRefreshEvent payload)
        {
            if (!GetIsPC(_target))
                return;

            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);

            SP = $"{dbPlayer.TotalSPAcquired} / {Skill.SkillCap} ({dbPlayer.UnallocatedSP})";
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
    }
}
