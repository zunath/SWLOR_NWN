using System;
using System.Collections.Generic;
using System.Linq;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;
using Ability = SWLOR.Game.Server.Service.Ability;
using ClassType = SWLOR.NWN.API.NWScript.Enum.ClassType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using RacialType = SWLOR.NWN.API.NWScript.Enum.RacialType;
using SavingThrow = SWLOR.NWN.API.NWScript.Enum.SavingThrow;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CharacterFullRebuildViewModel: GuiViewModelBase<CharacterFullRebuildViewModel, GuiPayloadBase>
    {
        private ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        [ScriptHandler(ScriptName.OnCharacterRebuild)]
        public static void LoadCharacterMigrationWindow()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, ColorToken.Red("Only players may use this object."));
                return;
            }

            ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(player)), player);
            Stat.RestoreFP(player, Stat.GetMaxFP(player));
            Stat.RestoreStamina(player, Stat.GetMaxStamina(player));
            Gui.TogglePlayerWindow(player, GuiWindowType.CharacterMigration, null, OBJECT_SELF);
        }

        [ScriptHandler(ScriptName.OnExitRebuild)]
        public static void ExitRebuildArea()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player))
            {
                FloatingTextStringOnCreature("Only players may use this.", player, false);
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.RebuildComplete)
            {
                FloatingTextStringOnCreature("Your rebuild must be completed before returning to the spending area.", player, false);
                return;
            }

            var location = GetLocation(GetWaypointByTag("REBUILD_TO_SPENDING_LANDING"));
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(location);
            });
        }

        [ScriptHandler(ScriptName.OnExitSpending)]
        public static void ExitSpendingArea()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player))
            {
                FloatingTextStringOnCreature("Only players may use this.", player, false);
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.RebuildComplete)
            {
                FloatingTextStringOnCreature("Your rebuild must be completed before returning to the game world.", player, false);
                return;
            }

            if (string.IsNullOrWhiteSpace(dbPlayer.LocationAreaResref))
            {
                var location = GetLocation(GetWaypointByTag("CZ220_LANDING"));
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(location);
                });
            }
            else
            {
                var area = Area.GetAreaByResref(dbPlayer.LocationAreaResref);
                var position = Vector3(dbPlayer.LocationX, dbPlayer.LocationY, dbPlayer.LocationZ);
                var location = Location(area, position, dbPlayer.LocationOrientation);
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(location);
                });
            }

        }

        public bool CanDistribute
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int CharacterType
        {
            get => Get<int>();
            set => Set(value);
        }

        private const int MaxAbilityIncreases = 15;
        
        private int _remainingAbilityPoints;
        private int _remainingSkillPoints;
        private int _might;
        private int _perception;
        private int _vitality;
        private int _willpower;
        private int _agility;
        private int _social;

        public string RemainingAbilityPoints
        {
            get => Get<string>();
            set => Set(value);
        }

        public string RemainingSkillPoints
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Might
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Perception
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Vitality
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Willpower
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Agility
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Social
        {
            get => Get<string>();
            set => Set(value);
        }

        private readonly List<SkillType> _skills = new();
        private readonly List<int> _skillDistributionPoints = new();

        public GuiBindingList<string> SkillNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> SkillTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private void ResetControls()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);

            _might = 0;
            _perception = 0;
            _vitality = 0;
            _willpower = 0;
            _agility = 0;
            _social = 0;
            RecalculateAvailableAbilityPoints();
            Might = $"MGT [{_might}]";
            Perception = $"PER [{_perception}]";
            Vitality = $"VIT [{_vitality}]";
            Willpower = $"WIL [{_willpower}]";
            Agility = $"AGI [{_agility}]";
            Social = $"SOC [{_social}]";

            var racialStat = dbPlayer.RacialStat;
            var might = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Might) ;
            var perception = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Perception);
            var agility = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Agility);
            var vitality = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Vitality);
            var willpower = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Willpower);
            var social = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Social);

            CanDistribute = dbPlayer.Perks.Count == 0
                            && dbPlayer.Skills
                                .Where(x =>
                                {
                                    var detail = Skill.GetSkillDetails(x.Key);
                                    return detail.ContributesToSkillCap;
                                })
                                .Sum(s => s.Value.Rank) == 0
                            && might <= 10 + (racialStat == AbilityType.Might ? 1 : 0)
                            && perception <= 10 + (racialStat == AbilityType.Perception ? 1 : 0)
                            && agility <= 10 + (racialStat == AbilityType.Agility ? 1 : 0)
                            && vitality <= 10 + (racialStat == AbilityType.Vitality ? 1 : 0)
                            && willpower <= 10 + (racialStat == AbilityType.Willpower ? 1 : 0)
                            && social <= 10 + (racialStat == AbilityType.Social ? 1 : 0);

            RecalculateAvailableSkillPoints();
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            CharacterType = GetClassByPosition(1, Player) == ClassType.Standard ? 0 : 1;
            LoadSkills();
            ResetControls();
            WatchOnClient(model => model.CharacterType);
        }

        private void LoadSkills()
        {
            var availableSkills = Skill.GetActiveContributingSkills();
            var skills = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();

            _skills.Clear();
            _skillDistributionPoints.Clear();
            foreach (var (type, detail) in availableSkills)
            {
                _skills.Add(type);
                _skillDistributionPoints.Add(0);
                skills.Add($"{detail.Name} [0]");
                tooltips.Add(detail.Description);
            }

            SkillNames = skills;
            SkillTooltips = tooltips;
        }

        private void RecalculateAvailableAbilityPoints()
        {
            _remainingAbilityPoints = MaxAbilityIncreases - _might - _perception - _vitality - _willpower - _agility - _social;
            RemainingAbilityPoints = $"Attributes - {_remainingAbilityPoints} Points Remaining";
        }

        private void RecalculateAvailableSkillPoints()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);

            _remainingSkillPoints = dbPlayer.TotalSPAcquired - _skillDistributionPoints.Sum();
            RemainingSkillPoints = $"Skills - {_remainingSkillPoints} Points Remaining";
        }

        public Action OnClickResetEverything() => () =>
        {
            void UnequipAllItems()
            {
                AssignCommand(Player, () => ClearAllActions());
                for (var index = 0; index < NumberOfInventorySlots; index++)
                {
                    var slot = (InventorySlot)index;
                    var item = GetItemInSlot(slot, Player);
                    if (GetIsObjectValid(item)
                        && slot != InventorySlot.CreatureArmor 
                        && slot != InventorySlot.CreatureBite
                        && slot != InventorySlot.CreatureLeft
                        && slot != InventorySlot.CreatureRight)
                    {
                        AssignCommand(Player, () =>
                        {
                            ActionUnequipItem(item);
                        });
                    }
                }
            }

            void RefundAllPerks()
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = _db.Get<Player>(playerId);
                var pcPerks = dbPlayer.Perks.ToDictionary(x => x.Key, y => y.Value);

                foreach (var (type, level) in pcPerks)
                {
                    var perkDetail = Perk.GetPerkDetails(type);
                    var refundAmount = perkDetail.PerkLevels
                        .Where(x => x.Key <= level)
                        .Sum(x => x.Value.Price);

                    dbPlayer.UnallocatedSP += refundAmount;
                    dbPlayer.Perks.Remove(type);
                    _logger.Write<PerkRefundLogGroup>($"REFUND - {playerId} - Refunded Date {DateTime.UtcNow} - Level {level} - PerkID {type}");
                   
                    // Remove all feats granted by all levels of this perk.
                    var feats = perkDetail.PerkLevels.Values.SelectMany(s => s.GrantedFeats);
                    foreach (var feat in feats)
                    {
                        CreaturePlugin.RemoveFeat(Player, feat);
                    }

                    // Run all of the triggers related to refunding this perk.
                    foreach (var action in perkDetail.RefundedTriggers)
                    {
                        action(Player);
                    }
                }

                _db.Set(dbPlayer);
            }

            void RefundAllSkills()
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = _db.Get<Player>(playerId);
                
                foreach (var (type, _) in dbPlayer.Skills)
                {
                    var detail = Skill.GetSkillDetails(type);
                    if (!detail.ContributesToSkillCap)
                        continue;

                    dbPlayer.Skills[type].Rank = 0;
                    dbPlayer.Skills[type].XP = 0;
                    dbPlayer.Skills[type].IsLocked = false;
                }

                _db.Set(dbPlayer);
            }

            void ResetStats()
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = _db.Get<Player>(playerId);

                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Might, 10);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Perception, 10);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Vitality, 10);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Willpower, 10);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Agility, 10);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Social, 10);
                CreaturePlugin.SetBaseAttackBonus(Player, 1);

                CreaturePlugin.SetBaseSavingThrow(Player, SavingThrow.Fortitude, 0);
                CreaturePlugin.SetBaseSavingThrow(Player, SavingThrow.Will, 0);
                CreaturePlugin.SetBaseSavingThrow(Player, SavingThrow.Reflex, 0);

                dbPlayer.BaseStats[AbilityType.Might] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Might);
                dbPlayer.BaseStats[AbilityType.Perception] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Perception);
                dbPlayer.BaseStats[AbilityType.Vitality] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Vitality);
                dbPlayer.BaseStats[AbilityType.Willpower] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Willpower);
                dbPlayer.BaseStats[AbilityType.Agility] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Agility);
                dbPlayer.BaseStats[AbilityType.Social] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Social);

                dbPlayer.UpgradedStats[AbilityType.Might] = 0;
                dbPlayer.UpgradedStats[AbilityType.Perception] = 0;
                dbPlayer.UpgradedStats[AbilityType.Vitality] = 0;
                dbPlayer.UpgradedStats[AbilityType.Willpower] = 0;
                dbPlayer.UpgradedStats[AbilityType.Agility] = 0;
                dbPlayer.UpgradedStats[AbilityType.Social] = 0;

                dbPlayer.UnallocatedAP = dbPlayer.TotalAPAcquired;
                dbPlayer.RebuildComplete = false;
                _db.Set(dbPlayer);

                // Reapply racial stat bonus
                if (dbPlayer.RacialStat != AbilityType.Invalid)
                {
                    CreaturePlugin.ModifyRawAbilityScore(Player, dbPlayer.RacialStat, 1);
                }
            }

            ShowModal($"WARNING: Your perks and skill points will be refunded. Your stats will be reinitialized to 10 (before racial bonuses are applied). You will be required to distribute all of these points before leaving this area. Partial XP towards the next skill rank will be LOST. Are you sure you'd like to proceed?", () =>
            {
                if (Ability.IsAnyAbilityToggled(Player))
                {
                    FloatingTextStringOnCreature(ColorToken.Red("Please toggle all abilities OFF and try again."), Player, false);
                    return;
                }

                UnequipAllItems();
                RefundAllPerks();
                RefundAllSkills();
                ResetStats();

                ResetControls();

                ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(Player)), Player);

                FloatingTextStringOnCreature(ColorToken.Green("Character reset successfully!"), Player, false);
            });
        };

        public Action OnClickSubtractMight() => () =>
        {
            _might--;
            if (_might < 0)
                _might = 0;

            RecalculateAvailableAbilityPoints();
            Might = $"MGT [{_might}]";
        };

        public Action OnClickAddMight() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _might++;
            if (_might > 6)
                _might = 6;

            RecalculateAvailableAbilityPoints();
            Might = $"MGT [{_might}]";
        };

        public Action OnClickSubtractPerception() => () =>
        {
            _perception--;
            if (_perception < 0)
                _perception = 0;

            RecalculateAvailableAbilityPoints();
            Perception = $"PER [{_perception}]";
        };

        public Action OnClickAddPerception() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _perception++;
            if (_perception > 6)
                _perception = 6;

            RecalculateAvailableAbilityPoints();
            Perception = $"PER [{_perception}]";
        };


        public Action OnClickSubtractVitality() => () =>
        {
            _vitality--;
            if (_vitality < 0)
                _vitality = 0;

            RecalculateAvailableAbilityPoints();
            Vitality = $"VIT [{_vitality}]";
        };

        public Action OnClickAddVitality() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _vitality++;
            if (_vitality > 6)
                _vitality = 6;

            RecalculateAvailableAbilityPoints();
            Vitality = $"VIT [{_vitality}]";
        };

        public Action OnClickSubtractWillpower() => () =>
        {
            _willpower--;
            if (_willpower < 0)
                _willpower = 0;

            RecalculateAvailableAbilityPoints();
            Willpower = $"WIL [{_willpower}]";
        };

        public Action OnClickAddWillpower() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _willpower++;
            if (_willpower > 6)
                _willpower = 6;

            RecalculateAvailableAbilityPoints();
            Willpower = $"WIL [{_willpower}]";
        };

        public Action OnClickSubtractAgility() => () =>
        {
            _agility--;
            if (_agility < 0)
                _agility = 0;

            RecalculateAvailableAbilityPoints();
            Agility = $"AGI [{_agility}]";
        };

        public Action OnClickAddAgility() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _agility++;
            if (_agility > 6)
                _agility = 6;

            RecalculateAvailableAbilityPoints();
            Agility = $"AGI [{_agility}]";
        };

        public Action OnClickSubtractSocial() => () =>
        {
            _social--;
            if (_social < 0)
                _social = 0;

            RecalculateAvailableAbilityPoints();
            Social = $"SOC [{_social}]";
        };

        public Action OnClickAddSocial() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _social++;
            if (_social > 6)
                _social = 6;

            RecalculateAvailableAbilityPoints();
            Social = $"SOC [{_social}]";
        };

        public Action OnClickSubtractTenSkillPoints() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var skillType = _skills[index];
            var skill = Skill.GetSkillDetails(skillType);
            var currentValue = _skillDistributionPoints[index] - 10;
            if (currentValue < 0)
                currentValue = 0;

            _skillDistributionPoints[index] = currentValue;

            SkillNames[index] = $"{skill.Name} [{currentValue}]";
            RecalculateAvailableSkillPoints();
        };

        public Action OnClickSubtractOneSkillPoint() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var skillType = _skills[index];
            var skill = Skill.GetSkillDetails(skillType);
            var currentValue = _skillDistributionPoints[index] - 1;
            if (currentValue < 0)
                currentValue = 0;

            _skillDistributionPoints[index] = currentValue;

            SkillNames[index] = $"{skill.Name} [{currentValue}]";
            RecalculateAvailableSkillPoints();
        };

        public Action OnClickAddOneSkillPoint() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var skillType = _skills[index];
            var skill = Skill.GetSkillDetails(skillType);
            var amount = _remainingSkillPoints >= 1 ? 1 : 0;
            var currentValue = _skillDistributionPoints[index] + amount;
            if (currentValue > skill.MaxRank)
                currentValue = skill.MaxRank;

            _skillDistributionPoints[index] = currentValue;

            SkillNames[index] = $"{skill.Name} [{currentValue}]";
            RecalculateAvailableSkillPoints();
        };

        public Action OnClickAddTenSkillPoints() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var skillType = _skills[index];
            var skill = Skill.GetSkillDetails(skillType);
            var amount = _remainingSkillPoints >= 10 ? 10 : _remainingSkillPoints;
            var currentValue = _skillDistributionPoints[index] + amount;
            if (currentValue > skill.MaxRank)
                currentValue = skill.MaxRank;

            _skillDistributionPoints[index] = currentValue;

            SkillNames[index] = $"{skill.Name} [{currentValue}]";
            RecalculateAvailableSkillPoints();
        };


        public Action OnClickSave() => () =>
        {
            ShowModal($"Are you sure you'd like to save these changes?", () =>
            {
                var race = GetRacialType(Player);

                if (_remainingAbilityPoints > 0 || _remainingSkillPoints > 0)
                {
                    FloatingTextStringOnCreature(ColorToken.Red("Please distribute all ability points and skill points first. Resize the window if needed."), Player, false);
                    return;
                }

                var forceIndex = _skills.IndexOf(SkillType.Force);
                var devicesIndex = _skills.IndexOf(SkillType.Devices);

                if (_skillDistributionPoints[forceIndex] > 0 && CharacterType == 0)
                {
                    FloatingTextStringOnCreature("Standard characters cannot gain ranks in the Force skill.", Player, false);
                    return;
                }

                if (_skillDistributionPoints[devicesIndex] > 0 && CharacterType == 1)
                {
                    FloatingTextStringOnCreature("Force characters cannot gain ranks in the Devices skill.", Player, false);
                    return;
                }

                if (race == RacialType.Droid && CharacterType == 1)
                {
                    FloatingTextStringOnCreature("Droids may not be Force Sensitive.", Player, false);
                    return;
                }

                var playerId = GetObjectUUID(Player);
                var dbPlayer = _db.Get<Player>(playerId);

                CreaturePlugin.ModifyRawAbilityScore(Player, AbilityType.Might, _might);
                CreaturePlugin.ModifyRawAbilityScore(Player, AbilityType.Perception, _perception);
                CreaturePlugin.ModifyRawAbilityScore(Player, AbilityType.Vitality, _vitality);
                CreaturePlugin.ModifyRawAbilityScore(Player, AbilityType.Willpower, _willpower);
                CreaturePlugin.ModifyRawAbilityScore(Player, AbilityType.Agility, _agility);
                CreaturePlugin.ModifyRawAbilityScore(Player, AbilityType.Social, _social);

                dbPlayer.BaseStats[AbilityType.Might] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Might);
                dbPlayer.BaseStats[AbilityType.Perception] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Perception);
                dbPlayer.BaseStats[AbilityType.Vitality] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Vitality);
                dbPlayer.BaseStats[AbilityType.Willpower] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Willpower);
                dbPlayer.BaseStats[AbilityType.Agility] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Agility);
                dbPlayer.BaseStats[AbilityType.Social] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Social);

                if (CharacterType == 0)
                {
                    CreaturePlugin.SetClassByPosition(Player, 0, ClassType.Standard);
                    dbPlayer.CharacterType = Enumeration.CharacterType.Standard;
                }
                else
                {
                    CreaturePlugin.SetClassByPosition(Player, 0, ClassType.ForceSensitive);
                    dbPlayer.CharacterType = Enumeration.CharacterType.ForceSensitive;
                }

                for (var index = 0; index < _skills.Count; index++)
                {
                    var skill = _skills[index];
                    var rank = _skillDistributionPoints[index];

                    dbPlayer.Skills[skill].Rank = rank;
                }

                dbPlayer.RebuildComplete = true;

                Gui.TogglePlayerWindow(Player, GuiWindowType.CharacterMigration, null, TetherObject);
                FloatingTextStringOnCreature(ColorToken.Green("Character rebuild complete!"), Player, false);

                _db.Set(dbPlayer);
            });
        };
    }
}
