using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DroidAIViewModel : GuiViewModelBase<DroidAIViewModel, DroidAIPayload>
    {
        private uint _controller;
        private List<DroidPerk> _availableDroidPerks;
        private List<DroidPerk> _activeDroidPerks;

        public string DroidName
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> AvailablePerkNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> AvailablePerkSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> ActivePerkNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ActivePerkSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string AISlots
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor AISlotsColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        // One row DTO per perk, replacing the pair of hand-synced parallel
        // GuiBindingList instances Initialize used to build in lockstep for
        // each of the available/active perk lists.
        private sealed class DroidPerkEntry
        {
            public DroidPerk Perk { get; }
            public string Name { get; }
            public bool Selected { get; }

            public DroidPerkEntry(DroidPerk perk, string name, bool selected)
            {
                Perk = perk;
                Name = name;
                Selected = selected;
            }
        }

        private static readonly GuiTableSource<DroidAIViewModel, DroidPerkEntry> AvailablePerksTable =
            new GuiTableSource<DroidAIViewModel, DroidPerkEntry>()
                .Column((m, v) => m.AvailablePerkNames = v, r => r.Name)
                .Column((m, v) => m.AvailablePerkSelections = v, r => r.Selected);

        private static readonly GuiTableSource<DroidAIViewModel, DroidPerkEntry> ActivePerksTable =
            new GuiTableSource<DroidAIViewModel, DroidPerkEntry>()
                .Column((m, v) => m.ActivePerkNames = v, r => r.Name)
                .Column((m, v) => m.ActivePerkSelections = v, r => r.Selected);

        protected override void Initialize(DroidAIPayload initialPayload)
        {
            _controller = initialPayload.ControllerItem;

            var constructedDroid = Droid.LoadConstructedDroid(_controller);
            var controllerStats = Droid.LoadDroidItemPropertyDetails(_controller);

            var availableRows = new List<DroidPerkEntry>();

            foreach (var perk in constructedDroid.LearnedPerks)
            {
                var detail = Perk.GetPerkDetails(perk.Perk);
                var perkTier = Perk.GetPerkLevelTier(perk.Perk, perk.Level);
                var perkLevel = detail.PerkLevels[perk.Level];

                if (perkTier <= controllerStats.Tier && !constructedDroid.ActivePerks.Exists(x => x.Perk == perk.Perk && x.Level == perk.Level))
                {
                    availableRows.Add(new DroidPerkEntry(perk, $"{detail.Name} {perk.Level} [{perkLevel.DroidAISlots}]", false));
                }
            }

            var activeRows = new List<DroidPerkEntry>();

            foreach (var perk in constructedDroid.ActivePerks)
            {
                var detail = Perk.GetPerkDetails(perk.Perk);
                var perkTier = Perk.GetPerkLevelTier(perk.Perk, perk.Level);
                var perkLevel = detail.PerkLevels[perk.Level];

                if (perkTier <= controllerStats.Tier)
                {
                    activeRows.Add(new DroidPerkEntry(perk, $"{detail.Name} {perk.Level} [{perkLevel.DroidAISlots}]", false));
                }
            }

            // Row-index lookups (AddToActivePerks, RemoveFromActivePerks) index
            // into these in lockstep with the bound lists.
            _availableDroidPerks = new List<DroidPerk>();
            foreach (var row in availableRows)
                _availableDroidPerks.Add(row.Perk);

            _activeDroidPerks = new List<DroidPerk>();
            foreach (var row in activeRows)
                _activeDroidPerks.Add(row.Perk);

            DroidName = constructedDroid.Name;
            AvailablePerksTable.Refresh(this, availableRows);
            ActivePerksTable.Refresh(this, activeRows);

            RefreshSlots();

            WatchOnClient(model => model.AvailablePerkSelections);
            WatchOnClient(model => model.ActivePerkSelections);
        }

        private int CalculateAISlots(ConstructedDroid constructedDroid)
        {
            return constructedDroid.ActivePerks.Sum(x =>
            {
                var perkDetail = Perk.GetPerkDetails(x.Perk);
                var perkLevel = perkDetail.PerkLevels[x.Level];
                return perkLevel.DroidAISlots;
            });
        }

        private void RefreshSlots()
        {
            var constructedDroid = Droid.LoadConstructedDroid(_controller);
            var controllerStats = Droid.LoadDroidItemPropertyDetails(_controller);
            var aiSlots = CalculateAISlots(constructedDroid);

            AISlots = $"{aiSlots} / {controllerStats.AISlots}";

            if (aiSlots >= controllerStats.AISlots)
                AISlotsColor = GuiColor.Red;
            else
                AISlotsColor = GuiColor.White;
        }

        public Action AddInstructionDisk() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please select an instruction disc from your inventory.",
                item =>
                {
                    var constructedDroid = Droid.LoadConstructedDroid(_controller);
                    var controllerDetails = Droid.LoadDroidItemPropertyDetails(_controller);

                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        var type = GetItemPropertyType(ip);

                        if (type == ItemPropertyType.DroidInstruction)
                        {
                            var perkType = (PerkType)GetItemPropertySubType(ip);
                            var level = GetItemPropertyCostTableValue(ip);
                            var tier = Perk.GetPerkLevelTier(perkType, level);

                            // Misconfigured disc.
                            if (tier <= 0)
                            {
                                var message = $"Instruction disc '{GetResRef(item)}' has an incorrectly assigned perk/level combination.";
                                Log.Write(LogGroup.Error, message, true);
                                SendMessageToPC(Player, ColorToken.Red(message));
                                return;
                            }


                            // Tier too high.
                            if (tier > controllerDetails.Tier)
                            {
                                SendMessageToPC(Player, ColorToken.Red($"This instruction disc cannot be uploaded because it exceeds the tier of your droid. (Droid tier: {controllerDetails.Tier}, Required: {tier})"));
                                return;
                            }

                            // Already learned.
                            if (constructedDroid.LearnedPerks.Exists(x => x.Perk == perkType && x.Level == level))
                            {
                                SendMessageToPC(Player, ColorToken.Red("That instruction disc has already been uploaded to this droid."));
                                return;
                            }

                            var perk = new DroidPerk(perkType, level);
                            var perkDetail = Perk.GetPerkDetails(perkType);
                            var perkLevel = perkDetail.PerkLevels[level];
                            constructedDroid.LearnedPerks.Add(perk);
                            AvailablePerkNames.Add($"{perkDetail.Name} {level} [{perkLevel.DroidAISlots}]");
                            AvailablePerkSelections.Add(false);
                            _availableDroidPerks.Add(perk);
                        }
                    }

                    Droid.SaveConstructedDroid(_controller, constructedDroid);
                    DestroyObject(item);
                    SendMessageToPC(Player, ColorToken.Green("Instruction disc uploaded to droid successfully."));
                });
        };

        public Action CloseWindow() => () =>
        {
            SetItemCursedFlag(_controller, false);
        };

        public Action AddToActivePerks() => () =>
        {
            var constructedDroid = Droid.LoadConstructedDroid(_controller);
            var controllerDetails = Droid.LoadDroidItemPropertyDetails(_controller);

            for (var index = AvailablePerkSelections.Count-1; index >= 0; index--)
            {
                var name = AvailablePerkNames[index];
                var isSelected = AvailablePerkSelections[index];
                var perk = _availableDroidPerks[index];

                // Not selected - skip it.
                if (!isSelected)
                    continue;

                // Safety check - If the tier has changed since it was uploaded to the droid,
                // prevent it from being added to the active list.
                var tier = Perk.GetPerkLevelTier(perk.Perk, perk.Level);
                if (tier > controllerDetails.Tier)
                    continue;

                var perkDetail = Perk.GetPerkDetails(perk.Perk);
                var perkLevel = perkDetail.PerkLevels[perk.Level];
                var currentAISlotsUsed = CalculateAISlots(constructedDroid);

                // Not enough AI slots - skip it.
                if (currentAISlotsUsed + perkLevel.DroidAISlots > controllerDetails.AISlots)
                    continue;

                AvailablePerkSelections.RemoveAt(index);
                AvailablePerkNames.RemoveAt(index);
                _availableDroidPerks.RemoveAt(index);

                ActivePerkSelections.Add(false);
                ActivePerkNames.Add(name);
                _activeDroidPerks.Add(perk);

                constructedDroid.ActivePerks.Add(perk);

                var ip = ItemPropertyCustom(ItemPropertyType.DroidInstruction, (int)perk.Perk, perk.Level);
                BiowareXP2.IPSafeAddItemProperty(_controller, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }

            Droid.SaveConstructedDroid(_controller, constructedDroid);
            RefreshSlots();
        };

        public Action RemoveFromActivePerks() => () =>
        {
            var constructedDroid = Droid.LoadConstructedDroid(_controller);

            for (var index = ActivePerkSelections.Count - 1; index >= 0; index--)
            {
                var name = ActivePerkNames[index];
                var isSelected = ActivePerkSelections[index];
                var perk = _activeDroidPerks[index];

                // Not selected - skip it.
                if (!isSelected)
                    continue;

                ActivePerkSelections.RemoveAt(index);
                ActivePerkNames.RemoveAt(index);
                _activeDroidPerks.RemoveAt(index);

                AvailablePerkSelections.Add(false);
                AvailablePerkNames.Add(name);
                _availableDroidPerks.Add(perk);

                constructedDroid.ActivePerks.RemoveAll(x => x.Perk == perk.Perk && x.Level == perk.Level);

                BiowareXP2.IPRemoveMatchingItemProperties(_controller, ItemPropertyType.DroidInstruction, DurationType.Invalid, (int)perk.Perk);
            }

            Droid.SaveConstructedDroid(_controller, constructedDroid);
            RefreshSlots();
        };
    }
}
