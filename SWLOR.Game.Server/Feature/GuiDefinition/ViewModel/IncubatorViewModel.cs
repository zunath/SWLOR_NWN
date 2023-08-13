using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
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

        private const int BaseSecondsBetweenStages = 30; // 129600 = 36 hours
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
        private int _fireDefense;
        private int _poisonDefense;
        private int _electricalDefense;
        private int _iceDefense;
        private int _fortitude;
        private int _reflex;
        private int _will;
        private int _xpPenalty;
        private int _mutationChance;

        private int _stageAttack;
        private int _stageAccuracy;
        private int _stageEvasion;
        private int _stageLearning;
        private int _stagePhysicalDefense;
        private int _stageForceDefense;
        private int _stageFireDefense;
        private int _stagePoisonDefense;
        private int _stageElectricalDefense;
        private int _stageIceDefense;
        private int _stageFortitude;
        private int _stageReflex;
        private int _stageWill;
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
        public string FireDefensePurity
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
        public string IceDefensePurity
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
                _iceDefense = dbJob.DefensePurities[CombatDamageType.Ice];
                _fireDefense = dbJob.DefensePurities[CombatDamageType.Fire];
                _poisonDefense = dbJob.DefensePurities[CombatDamageType.Poison];
                _electricalDefense = dbJob.DefensePurities[CombatDamageType.Electrical];

                _fortitude = dbJob.SavingThrowPurities[SavingThrow.Fortitude];
                _reflex = dbJob.SavingThrowPurities[SavingThrow.Reflex];
                _will = dbJob.SavingThrowPurities[SavingThrow.Will];

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
            FireDefensePurity = FormatStat(_fireDefense, _stageFireDefense, 0);
            PoisonDefensePurity = FormatStat(_poisonDefense, _stagePoisonDefense, 0);
            ElectricalDefensePurity = FormatStat(_electricalDefense, _stageElectricalDefense, 0);
            IceDefensePurity = FormatStat(_iceDefense, _stageIceDefense, 0);
            FortitudePurity = FormatStat(_fortitude, _stageFortitude, 0);
            ReflexPurity = FormatStat(_reflex, _stageReflex, 0);
            WillPurity = FormatStat(_will, _stageWill, 0);
            XPPenalty = FormatStat(_xpPenalty, _stageXPPenalty, 0);
            MutationChance = FormatStat(_mutationChance, _stageMutationChance, mutationBonus);
        }

        private int CalculateIncubationSeconds()
        {
            var incubationProcessingBonus = 0.01f * (Perk.GetPerkLevel(Player, PerkType.IncubationProcessing) * 10);
            var seconds = BaseSecondsBetweenStages - (int)(BaseSecondsBetweenStages * incubationProcessingBonus);

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
            _fireDefense = 0;
            _poisonDefense = 0;
            _electricalDefense = 0;
            _iceDefense = 0;
            _fortitude = 0;
            _reflex = 0;
            _will = 0;
            _xpPenalty = 0;

            _stageMutationChance = 0;
            _stageAttack = 0;
            _stageAccuracy = 0;
            _stageEvasion = 0;
            _stageLearning = 0;
            _stagePhysicalDefense = 0;
            _stageForceDefense = 0;
            _stageFireDefense = 0;
            _stagePoisonDefense = 0;
            _stageElectricalDefense = 0;
            _stageIceDefense = 0;
            _stageFortitude = 0;
            _stageReflex = 0;
            _stageWill = 0;
            _stageXPPenalty = 0;
        }

        public Action OnClickDNA() => () =>
        {
            if (!string.IsNullOrWhiteSpace(_dnaItem))
            {
                ShowModal("Will you remove the DNA from the incubator? All enzymes will also be removed.", RemoveDNA);
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
                        var type = (IncubationStatType)GetItemPropertySubType(ip);
                        var costId = GetItemPropertyCostTableValue(ip);

                        switch (type)
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
                            case IncubationStatType.FireDefensePurity:
                                _fireDefense = costId;
                                break;
                            case IncubationStatType.PoisonDefensePurity:
                                _poisonDefense = costId;
                                break;
                            case IncubationStatType.ElectricalDefensePurity:
                                _electricalDefense = costId;
                                break;
                            case IncubationStatType.IceDefensePurity:
                                _iceDefense = costId;
                                break;
                            case IncubationStatType.FortitudePurity:
                                _fortitude = costId;
                                break;
                            case IncubationStatType.ReflexPurity:
                                _reflex = costId;
                                break;
                            case IncubationStatType.WillPurity:
                                _will = costId;
                                break;
                            case IncubationStatType.XPPenalty:
                                _xpPenalty = costId;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.DNAType)
                        {
                            _dnaType = (BeastType)GetItemPropertySubType(ip);
                            break;
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
                        case IncubationStatType.FireDefensePurity:
                            _stageFireDefense += costId;
                            break;
                        case IncubationStatType.PoisonDefensePurity:
                            _stagePoisonDefense += costId;
                            break;
                        case IncubationStatType.ElectricalDefensePurity:
                            _stageElectricalDefense += costId;
                            break;
                        case IncubationStatType.IceDefensePurity:
                            _stageIceDefense += costId;
                            break;
                        case IncubationStatType.FortitudePurity:
                            _stageFortitude += costId;
                            break;
                        case IncubationStatType.ReflexPurity:
                            _stageReflex += costId;
                            break;
                        case IncubationStatType.WillPurity:
                            _stageWill += costId;
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
                    case IncubationStatType.FireDefensePurity:
                        _stageFireDefense -= costId;
                        break;
                    case IncubationStatType.PoisonDefensePurity:
                        _stagePoisonDefense -= costId;
                        break;
                    case IncubationStatType.ElectricalDefensePurity:
                        _stageElectricalDefense -= costId;
                        break;
                    case IncubationStatType.IceDefensePurity:
                        _stageIceDefense -= costId;
                        break;
                    case IncubationStatType.FortitudePurity:
                        _stageFortitude -= costId;
                        break;
                    case IncubationStatType.ReflexPurity:
                        _stageReflex -= costId;
                        break;
                    case IncubationStatType.WillPurity:
                        _stageWill -= costId;
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
                });
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
                });
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
                });
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
            var erraticGeniusBonus = job.CurrentStage <= 0 && IsErraticGeniusChecked ? GetErraticGeniusBonus() : 0;
            var mutationBonus = _stageMutationChance + erraticGeniusBonus;

            var mutationChance = _mutationChance + (mutationBonus > MaxStageIncrease ? MaxStageIncrease : mutationBonus);
            var attackPurity = _attack + (_stageAttack > MaxStageIncrease ? MaxStageIncrease : _stageAttack);
            var accuracyPurity = _accuracy + (_stageAccuracy > MaxStageIncrease ? MaxStageIncrease : _stageAccuracy);
            var evasionPurity = _evasion + (_stageEvasion > MaxStageIncrease ? MaxStageIncrease : _stageEvasion);
            var learningPurity = _learning + (_stageLearning > MaxStageIncrease ? MaxStageIncrease : _stageLearning);
            var xpPenalty = _xpPenalty + (_stageXPPenalty > MaxStageIncrease ? MaxStageIncrease : _stageXPPenalty);

            var physicalDefense = _physicalDefense + (_stagePhysicalDefense > MaxStageIncrease ? MaxStageIncrease : _stagePhysicalDefense);
            var forceDefense = _forceDefense + (_stageForceDefense > MaxStageIncrease ? MaxStageIncrease : _stageForceDefense);
            var iceDefense = _iceDefense + (_stageIceDefense > MaxStageIncrease ? MaxStageIncrease : _stageIceDefense);
            var fireDefense = _fireDefense + (_stageFireDefense > MaxStageIncrease ? MaxStageIncrease : _stageFireDefense);
            var poisonDefense = _poisonDefense + (_stagePoisonDefense > MaxStageIncrease ? MaxStageIncrease : _stagePoisonDefense);
            var electricalDefense = _electricalDefense + (_stageElectricalDefense > MaxStageIncrease ? MaxStageIncrease : _stageElectricalDefense);

            var fortitudePurity = _fortitude + (_stageFortitude > MaxStageIncrease ? MaxStageIncrease : _stageFortitude);
            var reflexPurity = _reflex + (_stageReflex > MaxStageIncrease ? MaxStageIncrease : _stageReflex);
            var willPurity = _will + (_stageWill > MaxStageIncrease ? MaxStageIncrease : _stageWill);

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
                job.DefensePurities[CombatDamageType.Ice] = iceDefense > MaxStat ? MaxStat : iceDefense;
                job.DefensePurities[CombatDamageType.Fire] = fireDefense > MaxStat ? MaxStat : fireDefense;
                job.DefensePurities[CombatDamageType.Poison] = poisonDefense > MaxStat ? MaxStat : poisonDefense;
                job.DefensePurities[CombatDamageType.Electrical] = electricalDefense > MaxStat ? MaxStat : electricalDefense;

                job.SavingThrowPurities[SavingThrow.Fortitude] = fortitudePurity > MaxStat ? MaxStat : fortitudePurity;
                job.SavingThrowPurities[SavingThrow.Reflex] = reflexPurity > MaxStat ? MaxStat : reflexPurity;
                job.SavingThrowPurities[SavingThrow.Will] = willPurity > MaxStat ? MaxStat : willPurity;

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
            });
        };

        public Action OnClickContinueJob() => () =>
        {
            var job = GetJob();
            if (job == null)
                return;

            ShowModal($"Are you sure you want to start this job?", () =>
            {
                StartJob(job);
            });
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
            });
        };

        public Action OnClickCompleteJob() => () =>
        {
            ShowModal("Are you sure you want to complete this job?", () =>
            {
                var job = GetJob();
                BeastMastery.CreateBeastEgg(job, Player);
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);
            });
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
