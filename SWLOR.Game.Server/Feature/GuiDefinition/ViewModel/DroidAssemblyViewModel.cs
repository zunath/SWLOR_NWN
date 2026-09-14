using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DroidAssemblyViewModel : GuiViewModelBase<DroidAssemblyViewModel, GuiPayloadBase>
    {
        private const string BlankTexture = "Blank";

        private string _cpuItem;
        private string _headItem;
        private string _bodyItem;
        private string _armsItem;
        private string _legsItem;

        private int _tier;
        private int _level;
        private int _aiSlots;
        private int _hp;
        private int _stamina;
        private int _might;
        private int _vitality;
        private int _perception;
        private int _agility;
        private int _willpower;
        private int _social;
        private int _vibroblade;
        private int _vibroknife;
        private int _lightsaber;
        private int _heavyVibroblade;
        private int _spear;
        private int _twinBlade;
        private int _saberstaff;
        private int _katar;
        private int _staff;
        private int _pistol;
        private int _rifle;
        private int _throwing;
        private int _armor;
        private int _fireResistance;
        private int _poisonResistance;
        private int _electricalResistance;
        private int _iceResistance;
        private int _mindResistance;
        private int _mobilityResistance;
        private int _traumaResistance;
        private int _disruptionResistance;

        public string Error
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool ProcessNotStarted
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsBuildInProgress
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCPUSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public int PersonalityIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public string Tier
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Level
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AISlots
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Stamina
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

        public string Vibroblade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Vibroknife
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Lightsaber
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HeavyVibroblade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Spear
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TwinBlade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Saberstaff
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Katar
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Staff
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Pistol
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Rifle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Throwing
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Armor
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FireResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PoisonResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ElectricalResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string IceResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MindResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MobilityResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TraumaResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DisruptionResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CPUResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HeadResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BodyResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string ArmsResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LegsResref
        {
            get => Get<string>();
            set => Set(value);
        }


        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Reset();

            WatchOnClient(model => model.Name);
            WatchOnClient(model => model.PersonalityIndex);
        }

        private void Reset()
        {
            Error = string.Empty;
            Name = string.Empty;
            ProcessNotStarted = true;
            IsBuildInProgress = false;
            IsCPUSelected = false;
            PersonalityIndex = 0;

            _tier = 0;
            _level = 0;
            _aiSlots = 0;
            _hp = 0;
            _stamina = 0;
            _might = 0;
            _vitality = 0;
            _perception = 0;
            _agility = 0;
            _willpower = 0;
            _social = 0;
            _vibroblade = 0;
            _vibroknife = 0;
            _lightsaber = 0;
            _heavyVibroblade = 0;
            _spear = 0;
            _twinBlade = 0;
            _saberstaff = 0;
            _katar = 0;
            _staff = 0;
            _pistol = 0;
            _rifle = 0;
            _throwing = 0;
            _armor = 0;
            _fireResistance = 0;
            _poisonResistance = 0;
            _electricalResistance = 0;
            _iceResistance = 0;
            _mindResistance = 0;
            _mobilityResistance = 0;
            _traumaResistance = 0;
            _disruptionResistance = 0;

            RefreshStats();

            CPUResref = BlankTexture;
            HeadResref = BlankTexture;
            BodyResref = BlankTexture;
            ArmsResref = BlankTexture;
            LegsResref = BlankTexture;

            if (!string.IsNullOrWhiteSpace(_cpuItem))
            {
                var item = ObjectPlugin.Deserialize(_cpuItem);
                ObjectPlugin.AcquireItem(Player, item);
            }

            if (!string.IsNullOrWhiteSpace(_headItem))
            {
                var item = ObjectPlugin.Deserialize(_headItem);
                ObjectPlugin.AcquireItem(Player, item);
            }

            if (!string.IsNullOrWhiteSpace(_bodyItem))
            {
                var item = ObjectPlugin.Deserialize(_bodyItem);
                ObjectPlugin.AcquireItem(Player, item);
            }

            if (!string.IsNullOrWhiteSpace(_armsItem))
            {
                var item = ObjectPlugin.Deserialize(_armsItem);
                ObjectPlugin.AcquireItem(Player, item);
            }

            if (!string.IsNullOrWhiteSpace(_legsItem))
            {
                var item = ObjectPlugin.Deserialize(_legsItem);
                ObjectPlugin.AcquireItem(Player, item);
            }

            _cpuItem = string.Empty;
            _headItem = string.Empty;
            _bodyItem = string.Empty;
            _armsItem = string.Empty;
            _legsItem = string.Empty;
        }

        private void ShowError(string text)
        {
            Error = text;
        }

        private void ClearError()
        {
            Error = string.Empty;
        }

        private void RefreshStats()
        {
            ClearError();

            Tier = $"Tier: {_tier}";
            Level = $"Level: {_level}";
            AISlots = $"AI Slots: {_aiSlots}";
            HP = $"HP: {_hp}";
            Stamina = $"STM: {_stamina}";
            Might = $"MGT: {_might}";
            Perception = $"PER: {_perception}";
            Vitality = $"VIT: {_vitality}";
            Willpower = $"WIL: {_willpower}";
            Agility = $"AGI: {_agility}";
            Social = $"SOC: {_social}";
            Vibroblade = $"Vibroblade: {_vibroblade}";
            Vibroknife = $"Vibroknife: {_vibroknife}";
            Lightsaber = $"Lightsaber: {_lightsaber}";
            HeavyVibroblade = $"Heavy Vibroblade: {_heavyVibroblade}";
            Spear = $"Spear: {_spear}";
            TwinBlade = $"Twin Blade: {_twinBlade}";
            Saberstaff = $"Saberstaff: {_saberstaff}";
            Katar = $"Katar: {_katar}";
            Staff = $"Staff: {_staff}";
            Pistol = $"Pistol: {_pistol}";
            Rifle = $"Rifle: {_rifle}";
            Throwing = $"Throwing: {_throwing}";
            Armor = $"Armor: {_armor}";
            FireResistance = $"Fire RES: {_fireResistance}";
            PoisonResistance = $"Poison RES: {_poisonResistance}";
            ElectricalResistance = $"Elec. RES: {_electricalResistance}";
            IceResistance = $"Ice RES: {_iceResistance}";
            MindResistance = $"Mind RES: {_mindResistance}";
            MobilityResistance = $"Mob. RES: {_mobilityResistance}";
            TraumaResistance = $"Trauma RES: {_traumaResistance}";
            DisruptionResistance = $"Disr. RES: {_disruptionResistance}";
        }

        private void AdjustResistances(IReadOnlyDictionary<ResistanceType, int> resistances, int multiplier)
        {
            foreach (var (type, value) in resistances)
            {
                var adjusted = value * multiplier;
                switch (type)
                {
                    case ResistanceType.Fire:
                        _fireResistance += adjusted;
                        break;
                    case ResistanceType.Poison:
                        _poisonResistance += adjusted;
                        break;
                    case ResistanceType.Electrical:
                        _electricalResistance += adjusted;
                        break;
                    case ResistanceType.Ice:
                        _iceResistance += adjusted;
                        break;
                    case ResistanceType.Mind:
                        _mindResistance += adjusted;
                        break;
                    case ResistanceType.Mobility:
                        _mobilityResistance += adjusted;
                        break;
                    case ResistanceType.Trauma:
                        _traumaResistance += adjusted;
                        break;
                    case ResistanceType.Disruption:
                        _disruptionResistance += adjusted;
                        break;
                }
            }
        }

        private void AddPart(DroidPartItemPropertyDetails part, uint item)
        {
            var assemblyLevel = Perk.GetPerkLevel(Player, PerkType.DroidAssembly);

            var serialized = ObjectPlugin.Serialize(item);
            var icon = Item.GetIconResref(item);
            switch (part.PartType)
            {
                case DroidPartItemPropertySubType.CPU:

                    if (assemblyLevel < part.Tier)
                    {
                        ShowError($"Droid Assembly too low. (Required: {part.Tier})");
                        return;
                    }

                    _cpuItem = serialized;
                    _level = part.Level;
                    _tier = part.Tier;
                    IsCPUSelected = true;
                    CPUResref = icon;
                    break;
                case DroidPartItemPropertySubType.Head:
                    if (part.Tier > _tier)
                    {
                        ShowError($"Head part tier must be less than or equal to CPU tier ({_tier}).");
                        return;
                    }
                    _headItem = serialized;
                    HeadResref = icon;
                    break;
                case DroidPartItemPropertySubType.Body:
                    if (part.Tier > _tier)
                    {
                        ShowError($"Body part tier must be less than or equal to CPU tier ({_tier}).");
                        return;
                    }
                    _bodyItem = serialized;
                    BodyResref = icon;
                    break;
                case DroidPartItemPropertySubType.Arms:
                    if (part.Tier > _tier)
                    {
                        ShowError($"Arms part tier must be less than or equal to CPU tier ({_tier}).");
                        return;
                    }
                    _armsItem = serialized;
                    ArmsResref = icon;
                    break;
                case DroidPartItemPropertySubType.Legs:
                    if (part.Tier > _tier)
                    {
                        ShowError($"Legs part tier must be less than or equal to CPU tier ({_tier}).");
                        return;
                    }
                    _legsItem = serialized;
                    LegsResref = icon;
                    break;
            }
            DestroyObject(item);

            _aiSlots += part.AISlots;
            _hp += part.HP;
            _stamina += part.STM;
            _might += part.MGT;
            _perception += part.PER;
            _vitality += part.VIT;
            _willpower += part.WIL;
            _agility += part.AGI;
            _social += part.SOC;
            _vibroblade += part.Vibroblade;
            _vibroknife += part.Vibroknife;
            _lightsaber += part.Lightsaber;
            _heavyVibroblade += part.HeavyVibroblade;
            _spear += part.Spear;
            _twinBlade += part.TwinBlade;
            _saberstaff += part.Saberstaff;
            _katar += part.Katar;
            _staff += part.Staff;
            _pistol += part.Pistol;
            _rifle += part.Rifle;
            _throwing += part.Throwing;
            _armor += part.Armor;
            AdjustResistances(part.Resistances, 1);

            RefreshStats();
        }

        private void RemovePart(DroidPartItemPropertyDetails part)
        {
            switch (part.PartType)
            {
                case DroidPartItemPropertySubType.CPU:
                    _cpuItem = string.Empty;
                    _level = 0;
                    _tier = 0;
                    IsCPUSelected = false;
                    CPUResref = BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Head:
                    _headItem = string.Empty;
                    HeadResref = BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Body:
                    _bodyItem = string.Empty;
                    BodyResref = BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Arms:
                    _armsItem = string.Empty;
                    ArmsResref = BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Legs:
                    _legsItem = string.Empty;
                    LegsResref = BlankTexture;
                    break;
            }

            _aiSlots -= part.AISlots;
            _hp -= part.HP;
            _stamina -= part.STM;
            _might -= part.MGT;
            _perception -= part.PER;
            _vitality -= part.VIT;
            _willpower -= part.WIL;
            _agility -= part.AGI;
            _social -= part.SOC;
            _vibroblade -= part.Vibroblade;
            _vibroknife -= part.Vibroknife;
            _lightsaber -= part.Lightsaber;
            _heavyVibroblade -= part.HeavyVibroblade;
            _spear -= part.Spear;
            _twinBlade -= part.TwinBlade;
            _saberstaff -= part.Saberstaff;
            _katar -= part.Katar;
            _staff -= part.Staff;
            _pistol -= part.Pistol;
            _rifle -= part.Rifle;
            _throwing -= part.Throwing;
            _armor -= part.Armor;
            AdjustResistances(part.Resistances, -1);

            RefreshStats();
        }

        public Action OnCloseWindow() => () =>
        {
            Reset();
        };

        public Action OnClickCPU() => () =>
        {
            ClearError();

            if (!string.IsNullOrWhiteSpace(_cpuItem))
            {
                ShowModal("Will you remove the CPU part?", () =>
                {
                    if (!string.IsNullOrWhiteSpace(_headItem) ||
                        !string.IsNullOrWhiteSpace(_bodyItem) ||
                        !string.IsNullOrWhiteSpace(_armsItem) ||
                        !string.IsNullOrWhiteSpace(_legsItem))
                    {
                        ShowError("Remove all parts first.");
                    }
                    else
                    {
                        var item = ObjectPlugin.Deserialize(_cpuItem);
                        ObjectPlugin.AcquireItem(Player, item);
                        CPUResref = BlankTexture;
                        var part = Droid.LoadDroidPartItemPropertyDetails(item);

                        RemovePart(part);
                    }
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a CPU part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        ShowError(error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.CPU)
                    {
                        ShowError("Select a CPU part.");
                        return;
                    }

                    AddPart(part, item);
                });
            }
        };

        public Action OnClickHead() => () =>
        {
            ClearError();

            if (!string.IsNullOrWhiteSpace(_headItem))
            {
                ShowModal("Will you remove the Head part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_headItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    HeadResref = BlankTexture;
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    RemovePart(part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a Head part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        ShowError(error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Head)
                    {
                        ShowError("Select a Head part.");
                        return;
                    }

                    AddPart(part, item);
                });
            }
        };
        public Action OnClickBody() => () =>
        {
            ClearError();

            if (!string.IsNullOrWhiteSpace(_bodyItem))
            {
                ShowModal("Will you remove the Body part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_bodyItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    BodyResref = BlankTexture;
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    RemovePart(part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a Body part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        ShowError(error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Body)
                    {
                        ShowError("Select a Body part.");
                        return;
                    }

                    AddPart(part, item);
                });
            }
        };
        public Action OnClickArms() => () =>
        {
            ClearError();

            if (!string.IsNullOrWhiteSpace(_armsItem))
            {
                ShowModal("Will you remove the Arms part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_armsItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    ArmsResref = BlankTexture;
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    RemovePart(part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select an Arms part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        ShowError(error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Arms)
                    {
                        ShowError("Select an Arms part.");
                        return;
                    }

                    AddPart(part, item);
                });
            }
        };
        public Action OnClickLegs() => () =>
        {
            ClearError();

            if (!string.IsNullOrWhiteSpace(_legsItem))
            {
                ShowModal("Will you remove the Legs part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_legsItem);
                    ObjectPlugin.AcquireItem(Player, item);
                    LegsResref = BlankTexture;
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    RemovePart(part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a Legs part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        ShowError(error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Legs)
                    {
                        ShowError("Select a Legs part.");
                        return;
                    }

                    AddPart(part, item);
                });
            }
        };

        public Action OnClickNewDroid() => () =>
        {
            ClearError();

            IsBuildInProgress = true;
            ProcessNotStarted = false;
        };

        public Action OnClickReset() => () =>
        {
            ClearError();

            ShowModal("Are you sure you want to reset everything?", () =>
            {
                Reset();
            });
        };

        public Action OnClickConstruct() => () =>
        {
            if (ProcessNotStarted)
                return;

            ClearError();

            if (string.IsNullOrWhiteSpace(_cpuItem) ||
                string.IsNullOrWhiteSpace(_headItem) ||
                string.IsNullOrWhiteSpace(_bodyItem) ||
                string.IsNullOrWhiteSpace(_armsItem) ||
                string.IsNullOrWhiteSpace(_legsItem))
            {
                ShowError("Missing required part!");
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowError("Enter droid name.");
                return;
            }

            if (PersonalityIndex == 0)
            {
                ShowError("Select droid personality.");
                return;
            }

            ShowModal("You are about to construct the droid. Are you sure you want to continue?", () =>
            {
                var controller = CreateItemOnObject(Droid.DroidControlItemResref, Player);
                SetName(controller, $"Droid Controller: {Name}");

                var constructedDroid = Droid.LoadConstructedDroid(controller);
                constructedDroid.Name = Name;

                var ipPersonality = ItemPropertyCustom(ItemPropertyType.DroidPersonality, PersonalityIndex);
                var ipTier = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, _tier);
                var ipAISlots = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.AISlots, _aiSlots);

                var ipHP = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.HP, _hp); ;
                var ipSTM = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.STM, _stamina); ;

                var ipAgility = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.AGI, _agility);
                var ipMight = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.MGT, _might);
                var ipPerception = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.PER, _perception);
                var ipVitality = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.VIT, _vitality);
                var ipWillpower = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.WIL, _willpower);
                var ipSocial = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.SOC, _social);

                var ipVibroblade = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroblade, _vibroblade);
                var ipVibroknife = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroknife, _vibroknife);
                var ipLightsaber = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Lightsaber, _lightsaber);
                var ipHeavyVibroblade = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.HeavyVibroblade, _heavyVibroblade);
                var ipSpear = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Spear, _spear);
                var ipTwinBlade = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinBlade, _twinBlade);
                var ipSaberstaff = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Saberstaff, _saberstaff);
                var ipKatar = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Katar, _katar);
                var ipStaff = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Staff, _staff);
                var ipPistol = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Pistol, _pistol);
                var ipRifle = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Rifle, _rifle);
                var ipThrowing = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Throwing, _throwing);
                var ipArmor = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Armor, _armor);
                var ipFireResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire, _fireResistance);
                var ipPoisonResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistancePoison, _poisonResistance);
                var ipElectricalResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceElectrical, _electricalResistance);
                var ipIceResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceIce, _iceResistance);
                var ipMindResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceMind, _mindResistance);
                var ipMobilityResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceMobility, _mobilityResistance);
                var ipTraumaResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceTrauma, _traumaResistance);
                var ipDisruptionResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceDisruption, _disruptionResistance);

                BiowareXP2.IPSafeAddItemProperty(controller, ipPersonality, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTier, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipAISlots, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipHP, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipSTM, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipAgility, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipMight, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipPerception, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipVitality, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipWillpower, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipSocial, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipVibroblade, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipVibroknife, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipLightsaber, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipHeavyVibroblade, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipSpear, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTwinBlade, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipSaberstaff, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipKatar, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipStaff, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipPistol, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipRifle, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipThrowing, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipArmor, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipFireResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipPoisonResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipElectricalResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipIceResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipMindResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipMobilityResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTraumaResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipDisruptionResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

                constructedDroid.SerializedCPU = _cpuItem;
                constructedDroid.SerializedHead = _headItem;
                constructedDroid.SerializedBody = _bodyItem;
                constructedDroid.SerializedArms = _armsItem;
                constructedDroid.SerializedLegs = _legsItem;

                Droid.SaveConstructedDroid(controller, constructedDroid);

                _cpuItem = string.Empty;
                _headItem = string.Empty;
                _bodyItem = string.Empty;
                _armsItem = string.Empty;
                _legsItem = string.Empty;

                Reset();
            });
        };

    }
}
