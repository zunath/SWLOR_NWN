using System;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class IncubatorViewModel : GuiViewModelBase<IncubatorViewModel, GuiPayloadBase>
    {
        private const string _blank = "Blank";
        private const string HydrolaseResrefPrefix = "hydrolase_";
        private const string LyaseResrefPrefix = "lyase_";
        private const string IsomeraseResrefPrefix = "isomerase_";
        private const string DNAResref = "beast_dna";

        private string _dnaItem;
        private string _hydrolaseItem;
        private string _isomeraseItem;
        private string _lyaseItem;

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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            DNAItemResref = _blank;
            HydrolaseItemResref = _blank;
            IsomeraseItemResref = _blank;
            LyaseItemResref = _blank;

            IsErraticGeniusChecked = false;

            WatchOnClient(model => model.IsErraticGeniusChecked);
        }

        private string FormatStat(int baseStat, int bonusStat)
        {
            var bonusPercentage = BeastMastery.GetIncubationPercentageById(bonusStat);
            if (bonusPercentage > 10f)
                bonusPercentage = 10f;

            var baseStatText = BeastMastery.GetIncubationPercentageById(baseStat);

            return $"{baseStatText}% [+{bonusPercentage:0.0###}%]";
        }

        private void RefreshAllStats()
        {
            AttackPurity = FormatStat(_attack, _stageAttack);
            AccuracyPurity = FormatStat(_accuracy, _stageAccuracy);
            EvasionPurity = FormatStat(_evasion, _stageEvasion);
            LearningPurity = FormatStat(_learning, _stageLearning);
            PhysicalDefensePurity = FormatStat(_physicalDefense, _stagePhysicalDefense);
            ForceDefensePurity = FormatStat(_forceDefense, _stageForceDefense);
            FireDefensePurity = FormatStat(_fireDefense, _stageFireDefense);
            PoisonDefensePurity = FormatStat(_poisonDefense, _stagePoisonDefense);
            ElectricalDefensePurity = FormatStat(_electricalDefense, _stageElectricalDefense);
            IceDefensePurity = FormatStat(_iceDefense, _stageIceDefense);
            FortitudePurity = FormatStat(_fortitude, _stageFortitude);
            ReflexPurity = FormatStat(_reflex, _stageReflex);
            WillPurity = FormatStat(_will, _stageWill);
            XPPenalty = FormatStat(_xpPenalty, _stageXPPenalty);
            MutationChance = FormatStat(_mutationChance, _stageMutationChance);
        }

        public Action OnClickDNA() => () =>
        {
            if (!string.IsNullOrWhiteSpace(_dnaItem))
            {
                ShowModal("Will you remove the DNA from the incubator? All enzymes will also be removed.", () =>
                {
                    var item = ObjectPlugin.Deserialize(_dnaItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    _dnaItem = string.Empty;
                    DNAItemResref = _blank;

                    if (!string.IsNullOrWhiteSpace(_hydrolaseItem))
                    {
                        item = ObjectPlugin.Deserialize(_hydrolaseItem);
                        ObjectPlugin.AcquireItem(Player, item);
                        _hydrolaseItem = string.Empty;
                        HydrolaseItemResref = _blank;
                    }
                    if (!string.IsNullOrWhiteSpace(_lyaseItem))
                    {
                        item = ObjectPlugin.Deserialize(_lyaseItem);
                        ObjectPlugin.AcquireItem(Player, item);
                        _lyaseItem = string.Empty;
                        LyaseItemResref = _blank;
                    }
                    if (!string.IsNullOrWhiteSpace(_isomeraseItem))
                    {
                        item = ObjectPlugin.Deserialize(_isomeraseItem);
                        ObjectPlugin.AcquireItem(Player, item);
                        _isomeraseItem = string.Empty;
                        IsomeraseItemResref = _blank;
                    }

                    IsStartJobEnabled = false;
                    IsErraticGeniusChecked = false;
                    IsErraticGeniusEnabled = false;

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

                    RefreshAllStats();
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a DNA item from your inventory.", item =>
                {
                    if (GetResRef(item) != DNAResref)
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

                    DNAItemResref = Item.GetIconResref(item);
                    _dnaItem = ObjectPlugin.Serialize(item);
                    DestroyObject(item);

                    RefreshAllStats();
                });
            }

        };

        private void AddItemStats(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
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

            RefreshAllStats();
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

                    SubtractItemStats(item);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a Hydrolase item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(HydrolaseResrefPrefix))
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
                    AddItemStats(item);
                    DestroyObject(item);
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

                    SubtractItemStats(item);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a Lyase item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(LyaseResrefPrefix))
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
                    AddItemStats(item);
                    DestroyObject(item);
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

                    SubtractItemStats(item);
                });
                
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select an Isomerase item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(IsomeraseResrefPrefix))
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
                    AddItemStats(item);
                    DestroyObject(item);
                });
            }
        };

        public Action OnClickStartJob() => () =>
        {

        };

    }
}
