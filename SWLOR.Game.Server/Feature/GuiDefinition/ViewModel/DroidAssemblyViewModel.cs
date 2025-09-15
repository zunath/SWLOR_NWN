using System;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
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
        private int _oneHanded;
        private int _twoHanded;
        private int _martialArts;
        private int _ranged;

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

        public string OneHanded
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TwoHanded
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MartialArts
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Ranged
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
            _oneHanded = 0;
            _twoHanded = 0;
            _martialArts = 0;
            _ranged = 0;

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
            OneHanded = $"1-Handed: {_oneHanded}";
            TwoHanded = $"2-Handed: {_twoHanded}";
            MartialArts = $"Martial Arts: {_martialArts}";
            Ranged = $"Ranged: {_ranged}";
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
            _oneHanded += part.OneHanded;
            _twoHanded += part.TwoHanded;
            _martialArts += part.MartialArts;
            _ranged += part.Ranged;

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
            _oneHanded -= part.OneHanded;
            _twoHanded -= part.TwoHanded;
            _martialArts -= part.MartialArts;
            _ranged -= part.Ranged;

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

                var ipOneHanded = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.OneHanded, _oneHanded);
                var ipTwoHanded = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.TwoHanded, _twoHanded);
                var ipMartialArts = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.MartialArts, _martialArts);
                var ipRanged = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Ranged, _ranged);

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
                BiowareXP2.IPSafeAddItemProperty(controller, ipOneHanded, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTwoHanded, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipMartialArts, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipRanged, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

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
