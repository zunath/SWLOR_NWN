using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RefineryViewModel: GuiViewModelBase<RefineryViewModel, GuiPayloadBase>
    {
        private class OreDetail
        {
            /// <summary>The ore's tier, 1 to 5. It gates the ore by Gathering rank and sets the level its refining XP is measured at.</summary>
            public int RequiredLevel { get; }
            public string RefinedItemResref { get; }

            public OreDetail(int requiredLevel, string refinedItemResref)
            {
                RequiredLevel = requiredLevel;
                RefinedItemResref = refinedItemResref;
            }
        }

        private static readonly Dictionary<string, OreDetail> _ores = new Dictionary<string, OreDetail>
        {
            {"raw_veldite", new OreDetail(1, "ref_veldite")},
            {"ore_tilarium", new OreDetail(1, "ref_tilarium")},

            {"raw_scordspar", new OreDetail(2, "ref_scordspar")},
            {"ore_currian", new OreDetail(2, "ref_currian")},


            {"raw_plagionite", new OreDetail(3, "ref_plagionite")},
            {"ore_idailia", new OreDetail(3, "ref_idailia")},

            {"raw_keromber", new OreDetail(4, "ref_keromber")},
            {"ore_barinium", new OreDetail(4, "ref_barinium")},

            {"raw_jasioclase", new OreDetail(5, "ref_jasioclase")},
            {"ore_gostian", new OreDetail(5, "ref_gostian")},

            {"raw_arkoxit", new OreDetail(5, "ref_arkoxit")},
            {"ore_arda", new OreDetail(5, "ref_arda")},

            // Options below are for further expansion down the road.
            {"raw_ochne", new OreDetail(99, "ref_ochne")},
            {"raw_croknor", new OreDetail(99, "ref_croknor")},
            {"raw_hemorgite", new OreDetail(99, "ref_hemorgite")},
            {"raw_bisteiss", new OreDetail(99, "ref_bisteiss")},
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
            // Ores that did not fit when the window last closed are still listed.
            _inputItems ??= new List<string>();
            _inputStackSizes ??= new List<int>();
            _inputItemResrefs ??= new List<string>();
            InputItemNames = new GuiBindingList<string>();
            InputItemToggles = new GuiBindingList<bool>();
            OutputItemNames = new GuiBindingList<string>();
            for (var index = 0; index < _inputItems.Count; index++)
                AddInputRow(_inputItemResrefs[index], _inputStackSizes[index]);

            CalculateCoresRequired();
        }

        private void AddInputRow(string resref, int stackSize)
        {
            InputItemNames.Add($"{stackSize}x {Cache.GetItemNameByResref(resref)}");
            InputItemToggles.Add(false);
            OutputItemNames.Add($"{stackSize}x {Cache.GetItemNameByResref(_ores[resref].RefinedItemResref)}");
        }

        /// <summary>
        /// Returns the listed ores, or only the selected ones, to the player. An ore without room stays listed.
        /// </summary>
        /// <returns>The number of ores that stayed listed.</returns>
        private int ReturnInputs(bool selectedOnly)
        {
            var kept = 0;
            for (var index = _inputItems.Count - 1; index >= 0; index--)
            {
                if (selectedOnly && !InputItemToggles[index])
                    continue;

                if (!Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(_inputItems[index])))
                {
                    kept++;
                    continue;
                }

                _inputItems.RemoveAt(index);
                _inputStackSizes.RemoveAt(index);
                _inputItemResrefs.RemoveAt(index);
                InputItemToggles.RemoveAt(index);
                InputItemNames.RemoveAt(index);
                OutputItemNames.RemoveAt(index);
            }

            CalculateCoresRequired();
            return kept;
        }

        private void CalculateCoresRequired()
        {
            var refineryManagement = CraftRankMilestones.GetRefineryManagementLevel(Player);
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

                // Player doesn't have prerequisite Gathering rank.
                var refiningTier = CraftRankMilestones.GetRefiningTier(Player);
                if (refiningTier < _ores[resref].RequiredLevel)
                {
                    SendMessageToPC(Player, $"Your Gathering rank must be at least {CraftRankMilestones.RequiredRankForTier(_ores[resref].RequiredLevel)} to refine that item.");
                    return false;
                }

                return true;
            }

            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please select a resource to refine.", (item) =>
            {
                if (!ValidateItem(item))
                    return;

                var resref = GetResRef(item);
                var stackSize = GetItemStackSize(item);
                var serialized = ObjectPlugin.Serialize(item);

                AddInputRow(resref, stackSize);
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

            if (ReturnInputs(true) > 0)
            {
                Instructions = Item.InventoryFullShortMessage;
                InstructionsColor = GuiColor.Red;
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
            effect = TagEffect(effect, PlayerActivityEffectTag.RefiningImmobilize);
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

                // Refining pays the same rank-difference XP harvesting does: GetDeltaXP(ore level - rank) for each unit, where an ore's
                // level is the level of the vein of its tier (5, 15, 25, 35, 45), so cheap ore stops training high ranks.
                var gatheringRank = Skill.GetCreatureSkillRank(Player, SkillType.Gathering);

                for (var index = _inputItems.Count - 1; index >= 0; index--)
                {
                    var oreResref = _inputItemResrefs[index];
                    var refinedItem = _ores[oreResref];
                    var stackSize = _inputStackSizes[index];

                    // Spawn the refined item onto the player.
                    CreateItemOnObject(refinedItem.RefinedItemResref, Player, stackSize);

                    xp += GatheringLevels.XpPerUnit(refinedItem.RequiredLevel, gatheringRank) * stackSize;
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

        public override Action OnWindowClosed() => () =>
        {
            if (ReturnInputs(false) > 0)
                SendMessageToPC(Player, Item.ItemsHeldMessage);
        };

    }
}
