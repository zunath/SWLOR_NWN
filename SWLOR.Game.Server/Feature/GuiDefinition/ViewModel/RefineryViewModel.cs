using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RefineryViewModel: GuiViewModelBase<RefineryViewModel, GuiPayloadBase>
    {
        private class OreDetail
        {
            public int RequiredLevel { get; }
            public string RefinedItemResref { get; }
            public int XPGranted { get; }


            public OreDetail(int requiredLevel, string refinedItemResref, int xpGranted)
            {
                RequiredLevel = requiredLevel;
                RefinedItemResref = refinedItemResref;
                XPGranted = xpGranted;
            }
        }

        private static readonly Dictionary<string, OreDetail> _ores = new Dictionary<string, OreDetail>
        {
            {"raw_veldite", new OreDetail(1, "ref_veldite", 25)},
            {"ore_tilarium", new OreDetail(1, "ref_tilarium", 25)},

            {"raw_scordspar", new OreDetail(2, "ref_scordspar", 50)},
            {"ore_currian", new OreDetail(2, "ref_currian", 50)},


            {"raw_plagionite", new OreDetail(3, "ref_plagionite", 75)},
            {"ore_idailia", new OreDetail(3, "ref_idailia", 75)},

            {"raw_keromber", new OreDetail(4, "ref_keromber", 100)},
            {"ore_barinium", new OreDetail(4, "ref_barinium", 100)},

            {"raw_jasioclase", new OreDetail(5, "ref_jasioclase", 125)},
            {"ore_gostian", new OreDetail(5, "ref_gostian", 125)},

            {"raw_arkoxit", new OreDetail(5, "ref_arkoxit", 150)},

            // Options below are for further expansion down the road.
            {"raw_ochne", new OreDetail(99, "ref_ochne", 175)},
            {"raw_croknor", new OreDetail(99, "ref_croknor", 200)},
            {"raw_hemorgite", new OreDetail(99, "ref_hemorgite", 225)},
            {"raw_bisteiss", new OreDetail(99, "ref_bisteiss", 250)},
        };

        private const int BaseItemsRefinedPerCore = 3;
        public const string PowerCoreIconResref = "iit_midmisc_008";
        private const string PowerCoreTag = "power_core";
        private const float RefiningDelaySeconds = 6f;
        private bool _isRefining;
        private int _powerCoresRequired;

        private List<string> _inputItems;
        private List<int> _inputStackSizes;
        private List<string> _inputItemResrefs;

        private int ItemCount => _inputStackSizes.Sum();

        public GuiBindingList<string> InputItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> InputItemToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> OutputItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string RequiredPowerCores
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCloseEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            IsCloseEnabled = true;
            _isRefining = false;
            _inputItems = new List<string>();
            _inputStackSizes = new List<int>();
            _inputItemResrefs = new List<string>();
            InputItemNames = new GuiBindingList<string>();
            InputItemToggles = new GuiBindingList<bool>();
            OutputItemNames = new GuiBindingList<string>();

            CalculateCoresRequired();
        }

        private void CalculateCoresRequired()
        {
            var refineryManagement = Perk.GetEffectivePerkLevel(Player, PerkType.RefineryManagement);
            var itemsPerCore = BaseItemsRefinedPerCore + refineryManagement;
            _powerCoresRequired = (int)Math.Ceiling(ItemCount / (float)itemsPerCore);
            
            if(_powerCoresRequired == 1)
                RequiredPowerCores = $"{_powerCoresRequired}x Power Core Required";
            else
                RequiredPowerCores = $"{_powerCoresRequired}x Power Cores Required";
        }

        private (List<uint>, List<int>) GetPowerCores()
        {
            var powerCoreItems = new List<uint>();
            var powerCoreCounts = new List<int>();

            for (var item = GetFirstItemInInventory(Player); GetIsObjectValid(item); item = GetNextItemInInventory(Player))
            {
                var tag = GetTag(item);
                if (tag == PowerCoreTag)
                {
                    var stackSize = GetItemStackSize(item);
                    powerCoreItems.Add(item);
                    powerCoreCounts.Add(stackSize);
                }
            }

            return (powerCoreItems, powerCoreCounts);
        }

        public Action OnClickAddItem() => () =>
        {
            bool ValidateItem(uint item)
            {
                if (_isRefining)
                    return false;

                if (GetItemPossessor(item) != Player)
                {
                    SendMessageToPC(Player, "Item must be in your inventory.");
                    return false;
                }

                var resref = GetResRef(item);

                // Not a valid item.
                if (!_ores.ContainsKey(resref))
                {
                    SendMessageToPC(Player, "Only raw materials may be added to the refining list.");
                    return false;
                }

                // Player doesn't have prerequisite level.
                var perkLevel = Perk.GetEffectivePerkLevel(Player, PerkType.Refining);
                if (perkLevel < _ores[resref].RequiredLevel)
                {
                    SendMessageToPC(Player, $"Your Refining perk level must be at least {_ores[resref].RequiredLevel} to refine that item.");
                    return false;
                }

                return true;
            }

            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please select a resource to refine.", (item) =>
            {
                if (!ValidateItem(item))
                    return;

                var resref = GetResRef(item);
                var itemName = Cache.GetItemNameByResref(resref);
                var stackSize = GetItemStackSize(item);
                var serialized = ObjectPlugin.Serialize(item);
                var outputOre = _ores[resref];
                var outputItemName = Cache.GetItemNameByResref(outputOre.RefinedItemResref);

                InputItemNames.Add($"{stackSize}x {itemName}");
                InputItemToggles.Add(false);
                OutputItemNames.Add($"{stackSize}x {outputItemName}");
                _inputItems.Add(serialized);
                _inputStackSizes.Add(stackSize);
                _inputItemResrefs.Add(resref);

                CalculateCoresRequired();

                DestroyObject(item);
            });
        };

        public Action OnClickRemoveItems() => () =>
        {
            if (_isRefining)
                return;

            for (var index = InputItemToggles.Count-1; index >= 0; index--)
            {
                var isToggled = InputItemToggles[index];

                if (!isToggled)
                    continue;

                var serialized = _inputItems[index];
                var item = ObjectPlugin.Deserialize(serialized);
                ObjectPlugin.AcquireItem(Player, item);

                _inputItems.RemoveAt(index);
                _inputStackSizes.RemoveAt(index);
                _inputItemResrefs.RemoveAt(index);
                InputItemToggles.RemoveAt(index);
                InputItemNames.RemoveAt(index);
                OutputItemNames.RemoveAt(index);

                CalculateCoresRequired();
            }
        };

        public Action OnClickRefine() => () =>
        {
            if (_isRefining)
                return;

            if (ItemCount <= 0)
            {
                Instructions = "Please add items.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            var (powerCoreItems, powerCoreCounts) = GetPowerCores();
            var totalPowerCores = powerCoreCounts.Sum();

            if (totalPowerCores < _powerCoresRequired)
            {
                Instructions = "Insufficient power cores!";
                InstructionsColor = GuiColor.Red;
                return;
            }

            Instructions = string.Empty;
            IsCloseEnabled = false;
            _isRefining = true;

            // Flag player as refining so that they can't queue up another item at the same time.
            SetLocalBool(Player, "IS_REFINING", true);

            // Apply immobilization
            var effect = EffectCutsceneImmobilize();
            effect = TagEffect(effect, "REFINING_EFFECT");
            ApplyEffectToObject(DurationType.Temporary, effect, Player, RefiningDelaySeconds);

            // Play an animation
            AssignCommand(Player, () => ActionPlayAnimation(Animation.LoopingGetMid, 1.0f, RefiningDelaySeconds));

            // Display the timing bar and finish the process when the delay elapses.
            PlayerPlugin.StartGuiTimingBar(Player, RefiningDelaySeconds);
            DelayCommand(RefiningDelaySeconds, () =>
            {
                // Recheck power core counts in case someone got rid of them in between the delay.
                (powerCoreItems, powerCoreCounts) = GetPowerCores();
                totalPowerCores = powerCoreCounts.Sum();

                if (totalPowerCores < _powerCoresRequired)
                {
                    Instructions = "Insufficient power cores!";
                    InstructionsColor = GuiColor.Red;
                    return;
                }

                var remainingCores = _powerCoresRequired;
                for (var index = powerCoreItems.Count - 1; index >= 0; index--)
                {
                    var item = powerCoreItems[index];
                    var count = powerCoreCounts[index];

                    // Stack size is greater than amount required.
                    if (count > remainingCores)
                    {
                        count -= remainingCores;
                        SetItemStackSize(item, count);
                        remainingCores = 0;
                        break;
                    }
                    // Stack size is less than or equal to the amount required.
                    else if (count <= remainingCores)
                    {
                        DestroyObject(item);
                        remainingCores -= count;
                    }
                }

                var xp = 0;

                for (var index = _inputItems.Count - 1; index >= 0; index--)
                {
                    var oreResref = _inputItemResrefs[index];
                    var refinedItem = _ores[oreResref];
                    var stackSize = _inputStackSizes[index];

                    // Spawn the refined item onto the player.
                    CreateItemOnObject(refinedItem.RefinedItemResref, Player, stackSize);

                    xp += refinedItem.XPGranted * stackSize;
                }

                DeleteLocalBool(Player, "IS_REFINING");
                Skill.GiveSkillXP(Player, SkillType.Gathering, xp, false, false);

                _inputItemResrefs.Clear();
                _inputItems.Clear();
                _inputStackSizes.Clear();
                InputItemToggles.Clear();
                InputItemNames.Clear();
                OutputItemNames.Clear();

                Instructions = "Success!";
                InstructionsColor = GuiColor.Green;
                _isRefining = false;
                IsCloseEnabled = true;

                CalculateCoresRequired();
            });
        };

        public Action OnClickInputItem() => () =>
        {
            var index = NuiGetEventArrayIndex();
            InputItemToggles[index] = !InputItemToggles[index];
        };

        public Action OnWindowClosed() => () =>
        {
            foreach (var serialized in _inputItems)
            {
                var item = ObjectPlugin.Deserialize(serialized);
                ObjectPlugin.AcquireItem(Player, item);
            }

            _inputItemResrefs.Clear();
            _inputItems.Clear();
            _inputStackSizes.Clear();
            InputItemToggles.Clear();
            InputItemNames.Clear();
            OutputItemNames.Clear();
        };

    }
}
