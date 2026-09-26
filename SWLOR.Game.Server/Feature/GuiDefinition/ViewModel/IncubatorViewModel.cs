using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class IncubatorViewModel : GuiViewModelBase<IncubatorViewModel, IncubatorPayload>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>
    {
        public const string PartialElement = "PARTIAL_VIEW";
        public const string NewJobPartial = "NEW_JOB_PARTIAL";
        public const string InProgressJobPartial = "IN_PROGRESS_JOB_PARTIAL";
        public const string StageCompleteJobPartial = "STAGE_COMPLETE_PARTIAL";
        public const string CompleteJobPartial = "COMPLETE_JOB_PARTIAL";

        private const int BaseSecondsBetweenStages = 129600; // 129600 = 36 hours
        private const int NumberOfStages = 3;

        private const string _blank = "Blank";

        private string _dnaItem;
        private string _hydrolaseItem;
        private string _isomeraseItem;
        private string _lyaseItem;

        private EnzymeColorType _lyaseColor;
        private EnzymeColorType _isomeraseColor;
        private EnzymeColorType _hydrolaseColor;

        private BeastType _dnaType;
        private string _incubatorPropertyId;

        private int _attack;
        private int _accuracy;
        private int _evasion;
        private int _learning;
        private int _physicalDefense;
        private int _forceDefense;
        private int _fireResistance;
        private int _poisonResistance;
        private int _electricalResistance;
        private int _iceResistance;
        private int _mindResistance;
        private int _mobilityResistance;
        private int _traumaResistance;
        private int _disruptionResistance;
        private int _xpPenalty;
        private int _mutationChance;

        private int _stageAttack;
        private int _stageAccuracy;
        private int _stageEvasion;
        private int _stageLearning;
        private int _stagePhysicalDefense;
        private int _stageForceDefense;
        private int _stageFireResistance;
        private int _stagePoisonResistance;
        private int _stageElectricalResistance;
        private int _stageIceResistance;
        private int _stageMindResistance;
        private int _stageMobilityResistance;
        private int _stageTraumaResistance;
        private int _stageDisruptionResistance;
        private int _stageXPPenalty;
        private int _stageMutationChance;

        public string DNAItemResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HydrolaseItemResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string IsomeraseItemResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LyaseItemResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsStartJobEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsContinueJobEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCompleteJobEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsErraticGeniusEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CurrentExperimentationStage
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EstimatedTimeToCompletion
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsErraticGeniusChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string MutationChance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DNALabel
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
        public string LearningPurity
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
        public string IceResistancePurity
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

        public string XPPenalty
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ErraticGeniusTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public float JobProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public string JobProgressTime
        {
            get => Get<string>();
            set => Set(value);
        }


        private void SwitchViews()
        {
            var dbJob = GetJob();
            if (dbJob == null)
            {
                ChangePartialView(PartialElement, NewJobPartial);
            }
            else
            {
                var now = DateTime.UtcNow;
                var delta = dbJob.DateCompleted - dbJob.DateStarted;
                var currentDelta = now - dbJob.DateStarted;
                var progressPercentage = (float)currentDelta.Ticks / (float)delta.Ticks;
                JobProgress = progressPercentage > 1f ? 1f : progressPercentage;

                _dnaType = dbJob.BeastDNAType;

                _mutationChance = dbJob.MutationChance;
                _attack = dbJob.AttackPurity;
                _accuracy = dbJob.AccuracyPurity;
                _evasion = dbJob.EvasionPurity;
                _learning = dbJob.LearningPurity;
                _xpPenalty = dbJob.XPPenalty;

                _physicalDefense = dbJob.DefensePurities[CombatDamageType.Physical];
                _forceDefense = dbJob.DefensePurities[CombatDamageType.Force];
                _iceResistance = dbJob.ResistancePurities[ResistanceType.Ice];
                _fireResistance = dbJob.ResistancePurities[ResistanceType.Fire];
                _poisonResistance = dbJob.ResistancePurities[ResistanceType.Poison];
                _electricalResistance = dbJob.ResistancePurities[ResistanceType.Electrical];

                _mindResistance = dbJob.ResistancePurities[ResistanceType.Mind];
                _mobilityResistance = dbJob.ResistancePurities[ResistanceType.Mobility];
                _traumaResistance = dbJob.ResistancePurities[ResistanceType.Trauma];
                _disruptionResistance = dbJob.ResistancePurities[ResistanceType.Disruption];

                if (now >= dbJob.DateCompleted)
                {
                    JobProgressTime = $"STAGE {dbJob.CurrentStage} COMPLETE";
                    IsStartJobEnabled = true;

                    if (dbJob.CurrentStage >= NumberOfStages)
                    {
                        IsCompleteJobEnabled = true;
                        ChangePartialView(PartialElement, CompleteJobPartial);
                    }
                    else
                    {
                        ChangePartialView(PartialElement, StageCompleteJobPartial);
                    }
                }
                else
                {
                    var deltaTime = dbJob.DateCompleted - now;
                    JobProgressTime = $"Stage {dbJob.CurrentStage} Remaining: {Time.GetTimeShortIntervals(deltaTime, false)}";
                    IsStartJobEnabled = false;
                    ChangePartialView(PartialElement, InProgressJobPartial);
                }

            }
        }

        protected override void Initialize(IncubatorPayload initialPayload)
        {
            _incubatorPropertyId = initialPayload.PropertyId;

            ClearStats();
            SwitchViews();

            DNAItemResref = _blank;
            HydrolaseItemResref = _blank;
            IsomeraseItemResref = _blank;
            LyaseItemResref = _blank;

            LoadPlayerStats();
            RefreshAllStats();
            IsErraticGeniusChecked = false;
            RefreshIncubationTime();

            WatchOnClient(model => model.IsErraticGeniusChecked);
        }

        private IncubationJob GetJob()
        {
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), _incubatorPropertyId, false);
            var dbJob = DB.Search(dbQuery)
                .FirstOrDefault();

            return dbJob;
        }

        private string FormatStat(int baseStat, int bonusStat, int additionalBonus)
        {
            var bonusPercentage = BeastMastery.GetIncubationPercentageById(bonusStat);
            if (bonusPercentage > 10f)
                bonusPercentage = 10f;

            bonusPercentage += additionalBonus;

            var baseStatText = BeastMastery.GetIncubationPercentageById(baseStat);

            return $"{baseStatText}% [+{bonusPercentage:0.0###}%]";
        }

        private int GetErraticGeniusBonus()
        {
            var erraticGenius = Perk.GetPerkLevel(Player, PerkType.ErraticGenius);
            var mutationBonus = 0;
            switch (erraticGenius)
            {
                case 1:
                    mutationBonus = 2;
                    break;
                case 2:
                    mutationBonus = 4;
                    break;
                case 3:
                    mutationBonus = 8;
                    break;
            }

            return mutationBonus;
        }

        private void LoadPlayerStats()
        {
            var mutationBonus = GetErraticGeniusBonus();
            ErraticGeniusTooltip = $"Increases mutation chance by {mutationBonus}% if checked.";
            IsErraticGeniusEnabled = mutationBonus > 0;

            if (!IsErraticGeniusEnabled)
                IsErraticGeniusChecked = false;
        }

        private void ToggleStartJob()
        {
            var job = GetJob();
            if (job != null)
            {
                IsStartJobEnabled = false;
                return;
            }

            IsStartJobEnabled = !string.IsNullOrWhiteSpace(_hydrolaseItem) &&
                                !string.IsNullOrWhiteSpace(_lyaseItem) &&
                                !string.IsNullOrWhiteSpace(_isomeraseItem) &&
                                !string.IsNullOrWhiteSpace(_dnaItem);
        }

        private void ToggleContinueJob()
        {
            var job = GetJob();
            if (job == null)
            {
                IsContinueJobEnabled = false;
                return;
            }

            IsContinueJobEnabled = !string.IsNullOrWhiteSpace(_hydrolaseItem) &&
                                   !string.IsNullOrWhiteSpace(_lyaseItem) &&
                                   !string.IsNullOrWhiteSpace(_isomeraseItem);
        }

        private void RefreshAllStats()
        {
            var mutationBonus = 0;

            if (IsErraticGeniusChecked)
            {
                mutationBonus = GetErraticGeniusBonus();
            }

            DNALabel = _dnaType == BeastType.Invalid
                ? "DNA [N/A]"
                : $"DNA [{BeastMastery.GetBeastDetail(_dnaType).Name}]";

            AttackPurity = FormatStat(_attack, _stageAttack, 0);
            AccuracyPurity = FormatStat(_accuracy, _stageAccuracy, 0);
            EvasionPurity = FormatStat(_evasion, _stageEvasion, 0);
            LearningPurity = FormatStat(_learning, _stageLearning, 0);
            PhysicalDefensePurity = FormatStat(_physicalDefense, _stagePhysicalDefense, 0);
            ForceDefensePurity = FormatStat(_forceDefense, _stageForceDefense, 0);
            FireResistancePurity = FormatStat(_fireResistance, _stageFireResistance, 0);
            PoisonResistancePurity = FormatStat(_poisonResistance, _stagePoisonResistance, 0);
            ElectricalResistancePurity = FormatStat(_electricalResistance, _stageElectricalResistance, 0);
            IceResistancePurity = FormatStat(_iceResistance, _stageIceResistance, 0);
            MindResistancePurity = FormatStat(_mindResistance, _stageMindResistance, 0);
            MobilityResistancePurity = FormatStat(_mobilityResistance, _stageMobilityResistance, 0);
            TraumaResistancePurity = FormatStat(_traumaResistance, _stageTraumaResistance, 0);
            DisruptionResistancePurity = FormatStat(_disruptionResistance, _stageDisruptionResistance, 0);
            XPPenalty = FormatStat(_xpPenalty, _stageXPPenalty, 0);
            MutationChance = FormatStat(_mutationChance, _stageMutationChance, mutationBonus);
        }

        private int CalculateIncubationSeconds()
        {
            var social = GetAbilityScore(Player, AbilityType.Social) - 10;
            var socialBonus = 0.5f * (social <= 0 ? 0 : social);
            if (socialBonus > 10)
                socialBonus = 10;

            var timeReductionPercentage = 0.01f * (Perk.GetPerkLevel(Player, PerkType.IncubationProcessing) * 10 + socialBonus);
            var seconds = BaseSecondsBetweenStages - (int)(BaseSecondsBetweenStages * timeReductionPercentage);

            return seconds;
        }

        private void RefreshIncubationTime()
        {
            var seconds = CalculateIncubationSeconds();
            var timespan = TimeSpan.FromSeconds(seconds);
            EstimatedTimeToCompletion = $"Time Required: {Time.GetTimeShortIntervals(timespan, false)}";
        }

        private void RemoveDNA()
        {
            uint item;
            if (!string.IsNullOrWhiteSpace(_dnaItem))
            {
                item = ObjectPlugin.Deserialize(_dnaItem);
                ObjectPlugin.AcquireItem(Player, item);
                _dnaItem = string.Empty;
                DNAItemResref = _blank;
            }
            if (!string.IsNullOrWhiteSpace(_hydrolaseItem))
            {
                item = ObjectPlugin.Deserialize(_hydrolaseItem);
                ObjectPlugin.AcquireItem(Player, item);
                _hydrolaseItem = string.Empty;
                _hydrolaseColor = EnzymeColorType.Invalid;
                HydrolaseItemResref = _blank;
            }
            if (!string.IsNullOrWhiteSpace(_lyaseItem))
            {
                item = ObjectPlugin.Deserialize(_lyaseItem);
                ObjectPlugin.AcquireItem(Player, item);
                _lyaseItem = string.Empty;
                _lyaseColor = EnzymeColorType.Invalid;
                LyaseItemResref = _blank;
            }
            if (!string.IsNullOrWhiteSpace(_isomeraseItem))
            {
                item = ObjectPlugin.Deserialize(_isomeraseItem);
                ObjectPlugin.AcquireItem(Player, item);
                _isomeraseItem = string.Empty;
                _isomeraseColor = EnzymeColorType.Invalid;
                IsomeraseItemResref = _blank;
            }

            IsStartJobEnabled = false;
            IsErraticGeniusChecked = false;
            IsErraticGeniusEnabled = false;

            ClearStats();

            RefreshAllStats();
            ToggleStartJob();
            ToggleContinueJob();
        }

        private void ClearStats()
        {
            _dnaType = BeastType.Invalid;

            _mutationChance = 0;
            _attack = 0;
            _accuracy = 0;
            _evasion = 0;
            _learning = 0;
            _physicalDefense = 0;
            _forceDefense = 0;
            _fireResistance = 0;
            _poisonResistance = 0;
            _electricalResistance = 0;
            _iceResistance = 0;
            _mindResistance = 0;
            _mobilityResistance = 0;
            _traumaResistance = 0;
            _disruptionResistance = 0;
            _xpPenalty = 0;

            _stageMutationChance = 0;
            _stageAttack = 0;
            _stageAccuracy = 0;
            _stageEvasion = 0;
            _stageLearning = 0;
            _stagePhysicalDefense = 0;
            _stageForceDefense = 0;
            _stageFireResistance = 0;
            _stagePoisonResistance = 0;
            _stageElectricalResistance = 0;
            _stageIceResistance = 0;
            _stageMindResistance = 0;
            _stageMobilityResistance = 0;
            _stageTraumaResistance = 0;
            _stageDisruptionResistance = 0;
            _stageXPPenalty = 0;
        }

        public Action OnClickDNA() => () =>
        {
            if (!string.IsNullOrWhiteSpace(_dnaItem))
            {
                ShowModal("Will you remove the DNA from the incubator? All enzymes will also be removed.", () =>
                {
                    RemoveDNA();
                    SwitchViews();
                }, SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a DNA item from your inventory.", item =>
                {
                    if (GetResRef(item) != BeastMastery.DNAResref)
                    {
                        FloatingTextStringOnCreature("Only DNA items may be selected.", Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, Player, false);
                        return;
                    }

                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        var type = GetItemPropertyType(ip);
                        if (type == ItemPropertyType.DNAType)
                        {
                            _dnaType = (BeastType)GetItemPropertySubType(ip);
                        }
                        else if(type == ItemPropertyType.Incubation)
                        {
                            var subType = (IncubationStatType)GetItemPropertySubType(ip);
                            var costId = GetItemPropertyCostTableValue(ip);

                            switch (subType)
                            {
                                case IncubationStatType.MutationChance:
                                    _mutationChance = costId;
                                    break;
                                case IncubationStatType.AttackPurity:
                                    _attack = costId;
                                    break;
                                case IncubationStatType.AccuracyPurity:
                                    _accuracy = costId;
                                    break;
                                case IncubationStatType.EvasionPurity:
                                    _evasion = costId;
                                    break;
                                case IncubationStatType.LearningPurity:
                                    _learning = costId;
                                    break;
                                case IncubationStatType.PhysicalDefensePurity:
                                    _physicalDefense = costId;
                                    break;
                                case IncubationStatType.ForceDefensePurity:
                                _forceDefense = costId;
                                break;
                            case IncubationStatType.FireResistancePurity:
                                _fireResistance = costId;
                                break;
                            case IncubationStatType.PoisonResistancePurity:
                                _poisonResistance = costId;
                                break;
                            case IncubationStatType.ElectricalResistancePurity:
                                _electricalResistance = costId;
                                break;
                            case IncubationStatType.IceResistancePurity:
                                _iceResistance = costId;
                                break;
                                case IncubationStatType.TraumaResistancePurity:
                                    _traumaResistance = costId;
                                    break;
                                case IncubationStatType.MobilityResistancePurity:
                                    _mobilityResistance = costId;
                                    break;
                                case IncubationStatType.MindResistancePurity:
                                    _mindResistance = costId;
                                    break;
                                case IncubationStatType.DisruptionResistancePurity:
                                    _disruptionResistance = costId;
                                    break;
                                case IncubationStatType.XPPenalty:
                                    _xpPenalty = costId;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }

                    DNAItemResref = Item.GetIconResref(item);
                    _dnaItem = ObjectPlugin.Serialize(item);
                    DestroyObject(item);

                    RefreshAllStats();
                    ToggleStartJob();
                    ToggleContinueJob();
                });
            }

        };

        private EnzymeColorType AddItemStats(uint item)
        {
            EnzymeColorType colorType = EnzymeColorType.Invalid;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var ipType = GetItemPropertyType(ip);
                if (ipType == ItemPropertyType.Incubation)
                {
                    var type = (IncubationStatType)GetItemPropertySubType(ip);
                    var costId = GetItemPropertyCostTableValue(ip);

                    switch (type)
                    {
                        case IncubationStatType.MutationChance:
                            _stageMutationChance += costId;
                            break;
                        case IncubationStatType.AttackPurity:
                            _stageAttack += costId;
                            break;
                        case IncubationStatType.AccuracyPurity:
                            _stageAccuracy += costId;
                            break;
                        case IncubationStatType.EvasionPurity:
                            _stageEvasion += costId;
                            break;
                        case IncubationStatType.LearningPurity:
                            _stageLearning += costId;
                            break;
                        case IncubationStatType.PhysicalDefensePurity:
                            _stagePhysicalDefense += costId;
                            break;
                        case IncubationStatType.ForceDefensePurity:
                            _stageForceDefense += costId;
                            break;
                        case IncubationStatType.FireResistancePurity:
                            _stageFireResistance += costId;
                            break;
                        case IncubationStatType.PoisonResistancePurity:
                            _stagePoisonResistance += costId;
                            break;
                        case IncubationStatType.ElectricalResistancePurity:
                            _stageElectricalResistance += costId;
                            break;
                        case IncubationStatType.IceResistancePurity:
                            _stageIceResistance += costId;
                            break;
                        case IncubationStatType.TraumaResistancePurity:
                            _stageTraumaResistance += costId;
                            break;
                        case IncubationStatType.MobilityResistancePurity:
                            _stageMobilityResistance += costId;
                            break;
                        case IncubationStatType.MindResistancePurity:
                            _stageMindResistance += costId;
                            break;
                        case IncubationStatType.DisruptionResistancePurity:
                            _stageDisruptionResistance += costId;
                            break;
                        case IncubationStatType.XPPenalty:
                            _stageXPPenalty += costId;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (ipType == ItemPropertyType.EnzymeColor)
                {
                    colorType = (EnzymeColorType)GetItemPropertySubType(ip);
                }
            }

            RefreshAllStats();

            return colorType;
        }

        private void SubtractItemStats(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = (IncubationStatType)GetItemPropertySubType(ip);
                var costId = GetItemPropertyCostTableValue(ip);

                switch (type)
                {
                    case IncubationStatType.MutationChance:
                        _stageMutationChance -= costId;
                        break;
                    case IncubationStatType.AttackPurity:
                        _stageAttack -= costId;
                        break;
                    case IncubationStatType.AccuracyPurity:
                        _stageAccuracy -= costId;
                        break;
                    case IncubationStatType.EvasionPurity:
                        _stageEvasion -= costId;
                        break;
                    case IncubationStatType.LearningPurity:
                        _stageLearning -= costId;
                        break;
                    case IncubationStatType.PhysicalDefensePurity:
                        _stagePhysicalDefense -= costId;
                        break;
                    case IncubationStatType.ForceDefensePurity:
                        _stageForceDefense -= costId;
                        break;
                    case IncubationStatType.FireResistancePurity:
                        _stageFireResistance -= costId;
                        break;
                    case IncubationStatType.PoisonResistancePurity:
                        _stagePoisonResistance -= costId;
                        break;
                    case IncubationStatType.ElectricalResistancePurity:
                        _stageElectricalResistance -= costId;
                        break;
                    case IncubationStatType.IceResistancePurity:
                        _stageIceResistance -= costId;
                        break;
                    case IncubationStatType.TraumaResistancePurity:
                        _stageTraumaResistance -= costId;
                        break;
                    case IncubationStatType.MobilityResistancePurity:
                        _stageMobilityResistance -= costId;
                        break;
                    case IncubationStatType.MindResistancePurity:
                        _stageMindResistance -= costId;
                        break;
                    case IncubationStatType.DisruptionResistancePurity:
                        _stageDisruptionResistance -= costId;
                        break;
                    case IncubationStatType.XPPenalty:
                        _stageXPPenalty -= costId;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            RefreshAllStats();
        }

        public Action OnClickHydrolase() => () =>
        {
            if (!string.IsNullOrWhiteSpace(_hydrolaseItem))
            {
                ShowModal("Will you remove the Hydrolase enzyme?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_hydrolaseItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    HydrolaseItemResref = _blank;
                    _hydrolaseItem = string.Empty;
                    _hydrolaseColor = EnzymeColorType.Invalid;

                    SubtractItemStats(item);
                    ToggleStartJob();
                    ToggleContinueJob();

                    SwitchViews();
                }, SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a Hydrolase item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(BeastMastery.HydrolaseResrefPrefix))
                    {
                        FloatingTextStringOnCreature("Only Hydrolase items may be selected.", Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, Player, false);
                        return;
                    }

                    HydrolaseItemResref = Item.GetIconResref(item);
                    _hydrolaseItem = ObjectPlugin.Serialize(item);
                    _hydrolaseColor = AddItemStats(item);
                    DestroyObject(item);
                    ToggleStartJob();
                    ToggleContinueJob();
                });
            }
        };

        public Action OnClickLyase() => () =>
        {
            if (!string.IsNullOrWhiteSpace(_lyaseItem))
            {
                ShowModal("Will you remove the Lyase Enzyme?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_lyaseItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    LyaseItemResref = _blank;
                    _lyaseItem = string.Empty;
                    _lyaseColor = EnzymeColorType.Invalid;

                    SubtractItemStats(item);
                    ToggleStartJob();
                    ToggleContinueJob();
                    SwitchViews();
                }, SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a Lyase item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(BeastMastery.LyaseResrefPrefix))
                    {
                        FloatingTextStringOnCreature("Only Lyase items may be selected.", Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, Player, false);
                        return;
                    }

                    LyaseItemResref = Item.GetIconResref(item);
                    _lyaseItem = ObjectPlugin.Serialize(item);
                    _lyaseColor = AddItemStats(item);
                    DestroyObject(item);
                    ToggleStartJob();
                    ToggleContinueJob();
                });
            }
        };

        public Action OnClickIsomerase() => () =>
        {
            if (!string.IsNullOrWhiteSpace(_isomeraseItem))
            {
                ShowModal("Will you remove the Isomerase Enzyme?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_isomeraseItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    IsomeraseItemResref = _blank;
                    _isomeraseItem = string.Empty;
                    _isomeraseColor = EnzymeColorType.Invalid;

                    SubtractItemStats(item);
                    ToggleStartJob();
                    ToggleContinueJob();
                    SwitchViews();
                }, SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select an Isomerase item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(BeastMastery.IsomeraseResrefPrefix))
                    {
                        FloatingTextStringOnCreature("Only Isomerase items may be selected.", Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, Player, false);
                        return;
                    }

                    IsomeraseItemResref = Item.GetIconResref(item);
                    _isomeraseItem = ObjectPlugin.Serialize(item);
                    _isomeraseColor = AddItemStats(item);
                    DestroyObject(item);
                    ToggleStartJob();
                    ToggleContinueJob();
                });
            }
        };

        public Action OnClickErraticGeniusToggled() => () =>
        {
            LoadPlayerStats();
            RefreshAllStats();
        };

        private string ValidateCreateJob()
        {
            var playerId = GetObjectUUID(Player);
            var maxConcurrentJobs = Perk.GetPerkLevel(Player, PerkType.IncubationManagement) + 1;
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.PlayerId), playerId, false);
            var currentJobs = DB.Search(dbQuery).ToList();
            var currentJobCount = currentJobs.Count(x => x.ParentPropertyId != _incubatorPropertyId);

            if (currentJobCount >= maxConcurrentJobs)
            {
                return $"You may only have {maxConcurrentJobs} incubation job(s) active at one time.";
            }

            var job = GetJob();
            if (job == null)
                return string.Empty;

            if (job.CurrentStage > NumberOfStages)
                return "Max stage reached.";

            if (IsErraticGeniusChecked && Perk.GetPerkLevel(Player, PerkType.ErraticGenius) <= 0)
            {
                return "You do not have the Erratic Genius perk purchased and cannot start this job.";
            }


            return string.Empty;
        }

        private void StartJob(IncubationJob job)
        {
            const int MaxStageIncrease = 100; // 10.0%
            const int MaxStat = 1000; // 100.0%
            var incubationSeconds = CalculateIncubationSeconds();
            var now = DateTime.UtcNow;

            var erraticGeniusBonus = (job.CurrentStage <= 0 && IsErraticGeniusChecked ? GetErraticGeniusBonus() : 0) * 10;
            var mutationChance = _mutationChance + (_stageMutationChance > MaxStageIncrease ? MaxStageIncrease : _stageMutationChance) + erraticGeniusBonus;

            var attackPurity = _attack + (_stageAttack > MaxStageIncrease ? MaxStageIncrease : _stageAttack);
            var accuracyPurity = _accuracy + (_stageAccuracy > MaxStageIncrease ? MaxStageIncrease : _stageAccuracy);
            var evasionPurity = _evasion + (_stageEvasion > MaxStageIncrease ? MaxStageIncrease : _stageEvasion);
            var learningPurity = _learning + (_stageLearning > MaxStageIncrease ? MaxStageIncrease : _stageLearning);
            var xpPenalty = _xpPenalty + (_stageXPPenalty > MaxStageIncrease ? MaxStageIncrease : _stageXPPenalty);

            var physicalDefense = _physicalDefense + (_stagePhysicalDefense > MaxStageIncrease ? MaxStageIncrease : _stagePhysicalDefense);
            var forceDefense = _forceDefense + (_stageForceDefense > MaxStageIncrease ? MaxStageIncrease : _stageForceDefense);
            var iceResistance = _iceResistance + (_stageIceResistance > MaxStageIncrease ? MaxStageIncrease : _stageIceResistance);
            var fireResistance = _fireResistance + (_stageFireResistance > MaxStageIncrease ? MaxStageIncrease : _stageFireResistance);
            var poisonResistance = _poisonResistance + (_stagePoisonResistance > MaxStageIncrease ? MaxStageIncrease : _stagePoisonResistance);
            var electricalResistance = _electricalResistance + (_stageElectricalResistance > MaxStageIncrease ? MaxStageIncrease : _stageElectricalResistance);

            var mindResistance = _mindResistance + (_stageMindResistance > MaxStageIncrease ? MaxStageIncrease : _stageMindResistance);
            var mobilityResistance = _mobilityResistance + (_stageMobilityResistance > MaxStageIncrease ? MaxStageIncrease : _stageMobilityResistance);
            var traumaResistance = _traumaResistance + (_stageTraumaResistance > MaxStageIncrease ? MaxStageIncrease : _stageTraumaResistance);
            var disruptionResistance = _disruptionResistance + (_stageDisruptionResistance > MaxStageIncrease ? MaxStageIncrease : _stageDisruptionResistance);

            var validationError = ValidateCreateJob();
            if (string.IsNullOrWhiteSpace(validationError))
            {
                job.CurrentStage++;

                job.MutationChance = mutationChance > MaxStat ? MaxStat : mutationChance;
                job.AttackPurity = attackPurity > MaxStat ? MaxStat : attackPurity;
                job.AccuracyPurity = accuracyPurity > MaxStat ? MaxStat : accuracyPurity;
                job.EvasionPurity = evasionPurity > MaxStat ? MaxStat : evasionPurity;
                job.LearningPurity = learningPurity > MaxStat ? MaxStat : learningPurity;
                job.XPPenalty = xpPenalty > MaxStat ? MaxStat : xpPenalty;

                job.DefensePurities[CombatDamageType.Physical] = physicalDefense > MaxStat ? MaxStat : physicalDefense;
                job.DefensePurities[CombatDamageType.Force] = forceDefense > MaxStat ? MaxStat : forceDefense;

                job.ResistancePurities[ResistanceType.Ice] = iceResistance > MaxStat ? MaxStat : iceResistance;
                job.ResistancePurities[ResistanceType.Fire] = fireResistance > MaxStat ? MaxStat : fireResistance;
                job.ResistancePurities[ResistanceType.Poison] = poisonResistance > MaxStat ? MaxStat : poisonResistance;
                job.ResistancePurities[ResistanceType.Electrical] = electricalResistance > MaxStat ? MaxStat : electricalResistance;
                job.ResistancePurities[ResistanceType.Mind] = mindResistance > MaxStat ? MaxStat : mindResistance;
                job.ResistancePurities[ResistanceType.Mobility] = mobilityResistance > MaxStat ? MaxStat : mobilityResistance;
                job.ResistancePurities[ResistanceType.Trauma] = traumaResistance > MaxStat ? MaxStat : traumaResistance;
                job.ResistancePurities[ResistanceType.Disruption] = disruptionResistance > MaxStat ? MaxStat : disruptionResistance;

                job.ResistancePurities = BeastResistanceCalculator.CreateResistancePurities(job.ResistancePurities);

                if(_lyaseColor != EnzymeColorType.Invalid)
                    job.LyaseColors[_lyaseColor]++;
                if (_hydrolaseColor != EnzymeColorType.Invalid)
                    job.HydrolaseColors[_hydrolaseColor]++;
                if (_isomeraseColor != EnzymeColorType.Invalid)
                    job.IsomeraseColors[_isomeraseColor]++;

                job.DateStarted = now;
                job.DateCompleted = now.AddSeconds(incubationSeconds);

                DB.Set(job);

                _dnaItem = string.Empty;
                _hydrolaseItem = string.Empty;
                _isomeraseItem = string.Empty;
                _lyaseItem = string.Empty;
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);
                FloatingTextStringOnCreature($"Incubation job started!", Player, false);
            }
            else
            {
                SendMessageToPC(Player, $"Unable to start Incubation Job. Reason: {validationError}");
                Log.Write(LogGroup.Incubation, $"Job could not be created on incubator Id {_incubatorPropertyId} due to reason: {validationError}");
            }
        }

        public Action OnClickStartJob() => () =>
        {
            ShowModal($"Are you sure you want to start this job?", () =>
            {
                var job = new IncubationJob
                {
                    ParentPropertyId = _incubatorPropertyId,
                    PlayerId = GetObjectUUID(Player),
                    BeastDNAType = _dnaType
                };
                StartJob(job);

                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickContinueJob() => () =>
        {
            var job = GetJob();
            if (job == null)
                return;

            ShowModal($"Are you sure you want to start this job?", () =>
            {
                StartJob(job);
                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickCancelJob() => () =>
        {
            ShowModal($"Are you sure you want to cancel this job? All DNA and enzyme items will be permanently lost!",
            () =>
            {
                var dbJob = GetJob();
                if (dbJob == null)
                    return;

                DB.Delete<IncubationJob>(dbJob.Id);
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);
                Log.Write(LogGroup.Incubation, $"Player '{GetName(Player)}' ({GetObjectUUID(Player)}) canceled incubation job '{dbJob.Id}' on incubator property Id '{dbJob.ParentPropertyId}'.");
                FloatingTextStringOnCreature($"Incubation job cancelled!", Player, false);

                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickCompleteJob() => () =>
        {
            ShowModal("Are you sure you want to complete this job?", () =>
            {
                var job = GetJob();
                BeastMastery.CreateBeastEgg(job, Player);
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);

                SwitchViews();
            }, SwitchViews);
        };

        public Action OnCloseWindow() => () =>
        {
            RemoveDNA();
        };

        public void Refresh(PerkAcquiredRefreshEvent payload)
        {
            LoadPlayerStats();
            RefreshAllStats();
            RefreshIncubationTime();
        }

        public void Refresh(PerkRefundedRefreshEvent payload)
        {
            LoadPlayerStats();
            RefreshAllStats();
            RefreshIncubationTime();
        }
    }
}
