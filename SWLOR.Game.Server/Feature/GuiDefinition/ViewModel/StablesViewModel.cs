using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class StablesViewModel : GuiViewModelBase<StablesViewModel, GuiPayloadBase>
    {
        public const string BeastDetailsPartial = "BEAST_DETAILS_PARTIAL";
        public const string PartialViewStats = "PARTIAL_VIEW_STATS";
        public const string PartialViewPurities = "PARTIAL_VIEW_PURITIES";
        public const string PartialViewPerks = "PARTIAL_VIEW_PERKS";

        private readonly List<string> _beastIds = new();
        private int _selectedBeastIndex = -1;

        public string Instructions
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor InstructionsColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiBindingList<string> BeastNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> BeastToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> BeastNameColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public string BeastCount
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsStatsToggled
        {
            get => Get<bool>();
            set
            {
                if (value)
                {
                    ChangePartialView(BeastDetailsPartial, PartialViewStats);
                    LoadSelectedBeast();
                }

                Set(value);
            }
        }

        public bool IsPuritiesToggled
        {
            get => Get<bool>();
            set
            {
                if (value)
                {
                    ChangePartialView(BeastDetailsPartial, PartialViewPurities);
                    LoadSelectedBeast();
                }

                Set(value);
            }
        }

        public bool IsPerksToggled
        {
            get => Get<bool>();
            set
            {
                if (value)
                {
                    ChangePartialView(BeastDetailsPartial, PartialViewPerks);
                    LoadSelectedBeast();
                }

                Set(value);
            }
        }

        public string Name
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
        public string SP
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Level
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

        public string MainHand
        {
            get => Get<string>();
            set => Set(value);
        }
        public string OffHand
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Attack
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Accuracy
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Evasion
        {
            get => Get<string>();
            set => Set(value);
        }
        public string PhysicalDefense
        {
            get => Get<string>();
            set => Set(value);
        }
        public string ForceDefense
        {
            get => Get<string>();
            set => Set(value);
        }
        public string ElementalDefense
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Role
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SavingThrows
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AttackPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string AccuracyPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string EvasionPurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PhysicalDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ForceDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FireDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string IceDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PoisonDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ElectricalDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FortitudePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string ReflexPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string WillPurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LearningPurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string ToggleMakeActiveButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string XPTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsBeastSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _selectedBeastIndex = -1;
            IsBeastSelected = false;
            ToggleMakeActiveButtonText = "Make Active";
            XPTooltip = $"XP: 0 / 0";
            InstructionsColor = GuiColor.Red;
            Instructions = string.Empty;

            IsStatsToggled = true;
            IsPuritiesToggled = false;
            LoadBeasts();

            Name = string.Empty;

            WatchOnClient(model => model.Name);
        }
        
        private void LoadBeasts()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var perkLevel = Perk.GetEffectivePerkLevel(Player, PerkType.Stabling) + 1;
            var dbQuery = new DBQuery<Beast>()
                .AddFieldSearch(nameof(Beast.OwnerPlayerId), playerId, false);
            var dbBeasts = DB.Search(dbQuery)
                .OrderBy(o => o.Name)
                .ToList();

            _beastIds.Clear();
            var beastNames = new GuiBindingList<string>();
            var beastToggles = new GuiBindingList<bool>();
            var beastNameColors = new GuiBindingList<GuiColor>();

            foreach (var dbBeast in dbBeasts)
            {
                _beastIds.Add(dbBeast.Id);
                beastNames.Add(dbBeast.Name);
                beastToggles.Add(false);
                
                if(dbBeast.Id == dbPlayer.ActiveBeastId)
                    beastNameColors.Add(GuiColor.Green);
                else 
                    beastNameColors.Add(GuiColor.White);
            }

            BeastNames = beastNames;
            BeastToggles = beastToggles;
            BeastNameColors = beastNameColors;
            BeastCount = $"Beasts: {dbBeasts.Count} / {perkLevel}";
            _selectedBeastIndex = -1;
            ClearSelectedBeast();
        }

        private void ClearSelectedBeast()
        {
            if (_selectedBeastIndex > -1)
                return;

            IsBeastSelected = false;
            Name = string.Empty;
            HP = string.Empty;
            FP = string.Empty;
            STM = string.Empty;
            SP = string.Empty;
            Level = string.Empty;

            Might = string.Empty;
            Perception = string.Empty;
            Vitality = string.Empty;
            Willpower = string.Empty;
            Agility = string.Empty;
            Social = string.Empty;

            MainHand = string.Empty;
            OffHand = string.Empty;

            Attack = string.Empty;
            Accuracy = string.Empty;
            Evasion = string.Empty;

            PhysicalDefense = string.Empty;
            ForceDefense = string.Empty;
            ElementalDefense = string.Empty;

            Role = string.Empty;
            SavingThrows = string.Empty;

            PerkNames = new GuiBindingList<string>();

            AttackPurity = string.Empty;
            AccuracyPurity = string.Empty;
            EvasionPurity = string.Empty;

            PhysicalDefensePurity = string.Empty;
            ForceDefensePurity = string.Empty;
            FireDefensePurity = string.Empty;
            IceDefensePurity = string.Empty;
            PoisonDefensePurity = string.Empty;
            ElectricalDefensePurity = string.Empty;

            FortitudePurity = string.Empty;
            ReflexPurity = string.Empty;
            WillPurity = string.Empty;

            LearningPurity = string.Empty;

            XPTooltip = $"XP: 0 / 0";
        }

        private void LoadSelectedBeast()
        {
            if (_selectedBeastIndex <= -1)
                return;

            var playerId = GetObjectUUID(Player);
            var beastId = _beastIds[_selectedBeastIndex];
            var dbBeast = DB.Get<Beast>(beastId);
            var dbPlayer = DB.Get<Player>(playerId);
            var beastDetails = BeastMastery.GetBeastDetail(dbBeast.Type);
            var roleDetails = BeastMastery.GetBeastRoleDetail(beastDetails.Role);
            var level = beastDetails.Levels[dbBeast.Level];

            if (dbPlayer.ActiveBeastId == beastId)
            {
                ToggleMakeActiveButtonText = "Make Inactive";
            }
            else
            {
                ToggleMakeActiveButtonText = "Make Active";
            }

            // Details Page
            Name = dbBeast.Name;
            XPTooltip = $"XP: {dbBeast.XP} / {BeastMastery.GetRequiredXP(dbBeast.Level)}";

            var hp = level.HP + 40 * ((level.Stats[AbilityType.Vitality] - 10) / 2);
            var fp = Stat.GetMaxFP(level.FP, (level.Stats[AbilityType.Willpower] - 10) / 2, 0);
            if (fp < 0)
                fp = 0;

            var stm= Stat.GetMaxStamina(level.STM, (level.Stats[AbilityType.Agility]-10) / 2, 0);
            if (stm < 0)
                stm = 0;

            HP = $"{hp}";
            FP = $"{fp}";
            STM = $"{stm}";
            SP = $"{dbBeast.Level} / {BeastMastery.MaxLevel} ({dbBeast.UnallocatedSP})";
            Level = $"{dbBeast.Level} / {BeastMastery.MaxLevel}";
            Might = $"{level.Stats[AbilityType.Might]}";
            Perception = $"{level.Stats[AbilityType.Perception]}";
            Vitality = $"{level.Stats[AbilityType.Vitality]}";
            Willpower = $"{level.Stats[AbilityType.Willpower]}";
            Agility = $"{level.Stats[AbilityType.Agility]}";
            Social = $"{level.Stats[AbilityType.Social]}";

            MainHand = "-";
            OffHand = "-";

            var attack = Stat.GetAttack(dbBeast.Level, level.Stats[beastDetails.DamageStat], (int)(level.MaxAttackBonus * (dbBeast.AttackPurity * 0.01f)));
            var accuracy = Stat.GetAccuracy(dbBeast.Level, level.Stats[beastDetails.AccuracyStat], (int)(level.MaxAccuracyBonus * (dbBeast.AccuracyPurity * 0.01f)));
            var evasion = Stat.GetEvasion(dbBeast.Level, level.Stats[AbilityType.Agility], (int)(level.MaxEvasionBonus * (dbBeast.EvasionPurity * 0.01f)));
            Attack = $"{attack}";
            Accuracy = $"{accuracy}";
            Evasion = $"{evasion}";

            var physicalDefense = Stat.CalculateDefense(level.Stats[AbilityType.Vitality], dbBeast.Level, (int)(level.MaxDefenseBonuses[CombatDamageType.Physical] * (dbBeast.DefensePurities[CombatDamageType.Physical] * 0.01f)));
            var forceDefense = Stat.CalculateDefense(level.Stats[AbilityType.Willpower], dbBeast.Level, (int)(level.MaxDefenseBonuses[CombatDamageType.Force] * (dbBeast.DefensePurities[CombatDamageType.Force] * 0.01f)));
            var fireDefense = $"{(int)(level.MaxDefenseBonuses[CombatDamageType.Fire] * (dbBeast.DefensePurities[CombatDamageType.Fire] * 0.01f))}";
            var poisonDefense = $"{(int)(level.MaxDefenseBonuses[CombatDamageType.Poison] * (dbBeast.DefensePurities[CombatDamageType.Poison] * 0.01f))}";
            var electricalDefense = $"{(int)(level.MaxDefenseBonuses[CombatDamageType.Electrical] * (dbBeast.DefensePurities[CombatDamageType.Electrical] * 0.01f))}";
            var iceDefense = $"{(int)(level.MaxDefenseBonuses[CombatDamageType.Ice] * (dbBeast.DefensePurities[CombatDamageType.Ice] * 0.01f))}";
            PhysicalDefense = $"{physicalDefense}";
            ForceDefense = $"{forceDefense}";
            ElementalDefense = $"{fireDefense}/{poisonDefense}/{electricalDefense}/{iceDefense}";

            Role = roleDetails.Name;

            var fortitude = (level.Stats[AbilityType.Might] - 10) / 2 + (int)(level.MaxSavingThrowBonuses[SavingThrow.Fortitude] * (dbBeast.SavingThrowPurities[SavingThrow.Fortitude] * 0.01f));
            var reflex = (level.Stats[AbilityType.Perception] - 10) / 2 + (int)(level.MaxSavingThrowBonuses[SavingThrow.Reflex] * (dbBeast.SavingThrowPurities[SavingThrow.Reflex] * 0.01f));
            var will = (level.Stats[AbilityType.Willpower] - 10) / 2 + (int)(level.MaxSavingThrowBonuses[SavingThrow.Will] * (dbBeast.SavingThrowPurities[SavingThrow.Will] * 0.01f));

            SavingThrows = $"{fortitude}/{reflex}/{will}";

            // Perks Page
            var perkNames = new GuiBindingList<string>();

            foreach (var (type, perkLevel) in dbBeast.Perks)
            {
                var perkDetail = Perk.GetPerkDetails(type);
                perkNames.Add($"{perkDetail.Name} {perkLevel}");
            }

            PerkNames = perkNames;

            // Purities Page
            AttackPurity = $"{dbBeast.AttackPurity}%";
            AccuracyPurity = $"{dbBeast.AccuracyPurity}%";
            EvasionPurity = $"{dbBeast.EvasionPurity}%";

            PhysicalDefensePurity = $"{dbBeast.DefensePurities[CombatDamageType.Physical]}%";
            ForceDefensePurity = $"{dbBeast.DefensePurities[CombatDamageType.Force]}%";
            FireDefensePurity = $"{dbBeast.DefensePurities[CombatDamageType.Fire]}%";
            IceDefensePurity = $"{dbBeast.DefensePurities[CombatDamageType.Ice]}%";
            PoisonDefensePurity = $"{dbBeast.DefensePurities[CombatDamageType.Poison]}%";
            ElectricalDefensePurity = $"{dbBeast.DefensePurities[CombatDamageType.Electrical]}%";

            FortitudePurity = $"{dbBeast.SavingThrowPurities[SavingThrow.Fortitude]}%";
            ReflexPurity = $"{dbBeast.SavingThrowPurities[SavingThrow.Reflex]}%";
            WillPurity = $"{dbBeast.SavingThrowPurities[SavingThrow.Will]}%";

            LearningPurity = $"{dbBeast.LearningPurity}%";

            IsBeastSelected = true;
        }

        private void ClearInstructions()
        {
            Instructions = string.Empty;
        }

        public Action OnClickBeast() => () =>
        {
            if (_selectedBeastIndex > -1)
            {
                BeastToggles[_selectedBeastIndex] = false;
            }
            
            _selectedBeastIndex = NuiGetEventArrayIndex();

            BeastToggles[_selectedBeastIndex] = true;

            LoadSelectedBeast();
            ClearInstructions();
            IsBeastSelected = true;
        };

        public Action OnClickToggleActive() => () =>
        {
            ClearInstructions();
            if (_selectedBeastIndex <= -1)
                return;

            var beast = GetAssociate(AssociateType.Henchman, Player);
            if (BeastMastery.IsPlayerBeast(beast))
            {
                Instructions = "Dismiss your active beast first.";
                return;
            }

            var beastNameColors = new GuiBindingList<GuiColor>();
            for (var index = 0; index < BeastNames.Count; index++)
            {
                beastNameColors.Add(GuiColor.White);
            }
            BeastNameColors = beastNameColors;

            var beastId = _beastIds[_selectedBeastIndex];
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer.ActiveBeastId == beastId)
            {
                dbPlayer.ActiveBeastId = string.Empty;
                ToggleMakeActiveButtonText = "Make Active";
                BeastNameColors[_selectedBeastIndex] = GuiColor.White;
            }
            else
            {
                dbPlayer.ActiveBeastId = beastId;
                ToggleMakeActiveButtonText = "Make Inactive";
                BeastNameColors[_selectedBeastIndex] = GuiColor.Green;
            }

            DB.Set(dbPlayer);
        };

        public Action OnClickReleaseBeast() => () =>
        {
            ShowModal($"WARNING: Releasing a beast will permanently remove it forever. This action is irreversible. Are you sure you want to release this beast?",
                () =>
                {
                    if (_selectedBeastIndex <= -1)
                        return;

                    var beastId = _beastIds[_selectedBeastIndex];
                    var playerId = GetObjectUUID(Player);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var beast = GetAssociate(AssociateType.Henchman, Player);
                    if (BeastMastery.IsPlayerBeast(beast) && BeastMastery.GetBeastId(beast) == beastId)
                    {
                        DestroyObject(beast);
                    }

                    if (dbPlayer.ActiveBeastId == beastId)
                    {
                        dbPlayer.ActiveBeastId = string.Empty;
                        DB.Set(dbPlayer);
                    }

                    DB.Delete<Beast>(beastId);

                    BeastNameColors.RemoveAt(_selectedBeastIndex);
                    BeastNames.RemoveAt(_selectedBeastIndex);
                    BeastToggles.RemoveAt(_selectedBeastIndex);

                    _selectedBeastIndex = -1;
                    IsBeastSelected = false;
                    ClearSelectedBeast();

                    IsPerksToggled = false;
                    IsPuritiesToggled = false;
                    IsStatsToggled = true;

                    LoadBeasts();
                },
                () =>
                {
                    IsPerksToggled = false;
                    IsPuritiesToggled = false;
                    IsStatsToggled = true;
                });
        };

        public Action OnClickStats() => () =>
        {
            IsStatsToggled = true;
            IsPuritiesToggled = false;
            IsPerksToggled = false;

            ClearInstructions();
        };

        public Action OnClickPurities() => () =>
        {
            IsStatsToggled = false;
            IsPuritiesToggled = true;
            IsPerksToggled = false;

            ClearInstructions();
        };

        public Action OnClickPerks() => () =>
        {
            IsStatsToggled = false;
            IsPuritiesToggled = false;
            IsPerksToggled = true;

            ClearInstructions();
        };

        public Action OnClickSaveName() => () =>
        {
            if (_selectedBeastIndex <= -1)
                return;

            if (string.IsNullOrWhiteSpace(Name))
            {
                Instructions = $"Please enter a name.";
                return;
            }

            if (Instructions.Length > 30)
            {
                Instructions = "Name must be 30 characters or less.";
                return;
            }

            ClearInstructions();

            var beastId = _beastIds[_selectedBeastIndex];
            var dbBeast = DB.Get<Beast>(beastId);

            dbBeast.Name = Name;
            DB.Set(dbBeast);

            BeastNames[_selectedBeastIndex] = dbBeast.Name;

            var beast = GetAssociate(AssociateType.Henchman, Player);
            if (BeastMastery.IsPlayerBeast(beast) && BeastMastery.GetBeastId(beast) == beastId)
            {
                SetName(beast, dbBeast.Name);
            }
        };
    }
}
