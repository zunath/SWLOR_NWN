using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class StablesViewModel : GuiViewModelBase<StablesViewModel, GuiPayloadBase>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>
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
        public string ElementalResistance
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Role
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StatusResistance
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

        public string FireResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string IceResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PoisonResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ElectricalResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MindResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string MobilityResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string TraumaResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string DisruptionResistancePurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LearningPurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string XPPenalty
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
            RefreshBeastCount(dbBeasts.Count);
            _selectedBeastIndex = -1;
            ClearSelectedBeast();
        }

        private void RefreshBeastCount(int beastCount)
        {
            var capacity = Perk.GetPerkLevel(Player, PerkType.Stabling) + 1;
            BeastCount = $"Beasts: {beastCount} / {capacity}";
        }

        public void Refresh(PerkAcquiredRefreshEvent payload)
        {
            if (payload.Type != PerkType.Stabling)
                return;

            RefreshBeastCount(_beastIds.Count);
        }

        public void Refresh(PerkRefundedRefreshEvent payload)
        {
            if (payload.Type != PerkType.Stabling)
                return;

            RefreshBeastCount(_beastIds.Count);
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
            ElementalResistance = string.Empty;

            Role = string.Empty;
            StatusResistance = string.Empty;

            PerkNames = new GuiBindingList<string>();

            AttackPurity = string.Empty;
            AccuracyPurity = string.Empty;
            EvasionPurity = string.Empty;

            PhysicalDefensePurity = string.Empty;
            ForceDefensePurity = string.Empty;
            FireResistancePurity = string.Empty;
            IceResistancePurity = string.Empty;
            PoisonResistancePurity = string.Empty;
            ElectricalResistancePurity = string.Empty;

            MindResistancePurity = string.Empty;
            MobilityResistancePurity = string.Empty;
            TraumaResistancePurity = string.Empty;
            DisruptionResistancePurity = string.Empty;

            LearningPurity = string.Empty;
            XPPenalty = string.Empty;

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
            XPTooltip = $"XP: {dbBeast.XP} / {BeastMastery.GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent)}";

            var fp = Stat.GetMaxFP(level.FP, level.Stats[AbilityType.Willpower], 0);
            if (fp < 0)
                fp = 0;

            var stm = Stat.GetMaxStamina(level.STM, level.Stats[AbilityType.Might], 0);
            if (stm < 0)
                stm = 0;

            HP = $"{level.HP}";
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

            var physicalDefense = BeastResistanceCalculator.CalculateDefenseBonus(level, dbBeast, CombatDamageType.Physical);
            var forceDefense = BeastResistanceCalculator.CalculateDefenseBonus(level, dbBeast, CombatDamageType.Force);
            var fireResistance = $"{BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Fire)}";
            var poisonResistance = $"{BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Poison)}";
            var electricalResistance = $"{BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Electrical)}";
            var iceResistance = $"{BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Ice)}";
            PhysicalDefense = $"{physicalDefense}";
            ForceDefense = $"{forceDefense}";
            ElementalResistance = $"{fireResistance}/{poisonResistance}/{electricalResistance}/{iceResistance}";

            Role = roleDetails.Name;

            var mindResistance = BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Mind);
            var mobilityResistance = BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Mobility);
            var traumaResistance = BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Trauma);
            var disruptionResistance = BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, ResistanceType.Disruption);

            StatusResistance = $"{mindResistance}/{mobilityResistance}/{traumaResistance}/{disruptionResistance}";

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
            FireResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Fire)}%";
            IceResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Ice)}%";
            PoisonResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Poison)}%";
            ElectricalResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Electrical)}%";

            MindResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Mind)}%";
            MobilityResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Mobility)}%";
            TraumaResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Trauma)}%";
            DisruptionResistancePurity = $"{BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Disruption)}%";

            LearningPurity = $"{dbBeast.LearningPurity}%";
            XPPenalty = $"{dbBeast.XPPenaltyPercent}%";

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
            var playerId = GetObjectUUID(Player);

            ClearInstructions();
            if (_selectedBeastIndex <= -1)
                return;

            var beast = GetAssociate(AssociateType.Henchman, Player);
            if (BeastMastery.IsPlayerBeast(beast))
            {
                Instructions = "Dismiss your active beast first.";
                return;
            }

            var dbQuery = new DBQuery<Beast>()
                .AddFieldSearch(nameof(Beast.OwnerPlayerId), playerId, false);
            var beastCount = DB.SearchCount(dbQuery);
            var perkLevel = Perk.GetPerkLevel(Player, PerkType.Stabling) + 1;
            if (perkLevel < beastCount)
            {
                Instructions = "Stabling perk level too low. Purchase the perk and try again.";
                return;
            }

            var beastNameColors = new GuiBindingList<GuiColor>();
            for (var index = 0; index < BeastNames.Count; index++)
            {
                beastNameColors.Add(GuiColor.White);
            }
            BeastNameColors = beastNameColors;

            var beastId = _beastIds[_selectedBeastIndex];
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

        private void CreateDNAItem(Beast dbBeast)
        {
            const int PurityMaxId = 1000;
            var beastDetail = BeastMastery.GetBeastDetail(dbBeast.Type);
            var dna = CreateItemOnObject(BeastMastery.DNAResref, Player);
            var percentage = (float)dbBeast.Level / (float)BeastMastery.MaxLevel;

            var attackPurity = (int)(dbBeast.AttackPurity * percentage) * 10;
            var accuracyPurity = (int)(dbBeast.AccuracyPurity * percentage) * 10;
            var evasionPurity = (int)(dbBeast.EvasionPurity * percentage) * 10;
            var learningPurity = (int)(dbBeast.LearningPurity * percentage) * 10;

            var physicalPurity = (int)(dbBeast.DefensePurities[CombatDamageType.Physical] * percentage) * 10;
            var forcePurity = (int)(dbBeast.DefensePurities[CombatDamageType.Force] * percentage) * 10;
            var firePurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Fire) * percentage) * 10;
            var icePurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Ice) * percentage) * 10;
            var electricalPurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Electrical) * percentage) * 10;
            var poisonPurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Poison) * percentage) * 10;

            var mindResistancePurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Mind) * percentage) * 10;
            var mobilityResistancePurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Mobility) * percentage) * 10;
            var traumaResistancePurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Trauma) * percentage) * 10;
            var disruptionResistancePurity = (int)(BeastResistanceCalculator.GetResistancePurity(dbBeast, ResistanceType.Disruption) * percentage) * 10;

            var xpPenalty = (int)(dbBeast.XPPenaltyPercent * percentage) * 10;

            var itemProperties = new List<ItemProperty>
            {
                ItemPropertyCustom(ItemPropertyType.DNAType, (int)dbBeast.Type),

                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AttackPurity, attackPurity > PurityMaxId ? PurityMaxId : attackPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AccuracyPurity, accuracyPurity> PurityMaxId ? PurityMaxId : accuracyPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.EvasionPurity, evasionPurity> PurityMaxId ? PurityMaxId : evasionPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.LearningPurity, learningPurity> PurityMaxId ? PurityMaxId : learningPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.PhysicalDefensePurity, physicalPurity > PurityMaxId ? PurityMaxId : physicalPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.ForceDefensePurity, forcePurity > PurityMaxId ? PurityMaxId : forcePurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.FireResistancePurity, firePurity > PurityMaxId ? PurityMaxId : firePurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.PoisonResistancePurity, poisonPurity > PurityMaxId ? PurityMaxId : poisonPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.ElectricalResistancePurity, electricalPurity > PurityMaxId ? PurityMaxId : electricalPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.IceResistancePurity, icePurity > PurityMaxId ? PurityMaxId : icePurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.MindResistancePurity, mindResistancePurity > PurityMaxId ? PurityMaxId : mindResistancePurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.MobilityResistancePurity, mobilityResistancePurity > PurityMaxId ? PurityMaxId : mobilityResistancePurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.TraumaResistancePurity, traumaResistancePurity > PurityMaxId ? PurityMaxId : traumaResistancePurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.DisruptionResistancePurity, disruptionResistancePurity > PurityMaxId ? PurityMaxId : disruptionResistancePurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.XPPenalty, xpPenalty > PurityMaxId ? PurityMaxId : xpPenalty),
            };

            foreach (var ip in itemProperties)
            {
                BiowareXP2.IPSafeAddItemProperty(dna, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }

            SetName(dna, $"Beast DNA: {beastDetail.Name}");
        }

        public Action OnClickReleaseBeast() => () =>
        {
            ShowModal($"WARNING: Releasing a beast will permanently remove it forever. A sample of the beast's DNA will be added to your inventory. Its purities will be based upon its level. This action is irreversible. Are you sure you want to release this beast?",
                () =>
                {
                    if (_selectedBeastIndex <= -1)
                        return;

                    var beastId = _beastIds[_selectedBeastIndex];
                    var dbBeast = DB.Get<Beast>(beastId);
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

                    CreateDNAItem(dbBeast);

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
