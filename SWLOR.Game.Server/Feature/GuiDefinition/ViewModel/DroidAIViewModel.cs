using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
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

        private bool _closed;
        private int _programmingSession;

        protected override void Initialize(DroidAIPayload initialPayload)
        {
            _controller = initialPayload.ControllerItem;
            _closed = false;
            _programmingSession++;
            RefreshPerks();
            WatchOnClient(model => model.AvailablePerkSelections);
            WatchOnClient(model => model.ActivePerkSelections);
        }

        private void RefreshPerks()
        {
            var droid = Droid.LoadConstructedDroid(_controller);
            var stats = Droid.LoadDroidItemPropertyDetails(_controller);
            if (DroidInstructions.Normalize(droid, stats.Tier, stats.AISlots))
                Droid.SaveInstructions(_controller, droid);

            _availableDroidPerks = droid.LearnedPerks.Where(perk =>
                Perk.GetPerkLevelTier(perk.Perk, perk.Level) <= stats.Tier &&
                !droid.ActivePerks.Any(active => active.Perk == perk.Perk && active.Level == perk.Level)).ToList();
            _activeDroidPerks = droid.ActivePerks;
            var availableNames = new GuiBindingList<string>();
            var availableSelections = new GuiBindingList<bool>();
            var activeNames = new GuiBindingList<string>();
            var activeSelections = new GuiBindingList<bool>();
            foreach (var perk in _availableDroidPerks)
            {
                availableNames.Add(GetInstructionName(perk));
                availableSelections.Add(false);
            }
            foreach (var perk in _activeDroidPerks)
            {
                activeNames.Add(GetInstructionName(perk));
                activeSelections.Add(false);
            }
            DroidName = droid.Name;
            AvailablePerkNames = availableNames;
            AvailablePerkSelections = availableSelections;
            ActivePerkNames = activeNames;
            ActivePerkSelections = activeSelections;
            var slots = DroidInstructions.GetSlots(droid.ActivePerks);
            AISlots = $"{slots} / {stats.AISlots}";
            AISlotsColor = slots > stats.AISlots ? GuiColor.Red : GuiColor.White;
        }

        private static string GetInstructionName(DroidPerk perk)
        {
            var detail = Perk.GetPerkDetails(perk.Perk);
            return $"{detail.Name} {perk.Level} [{detail.PerkLevels[perk.Level].DroidAISlots}]";
        }

        private bool CanEdit()
        {
            return !_closed && GetIsObjectValid(_controller) && GetItemPossessor(_controller) == Player &&
                   !GetIsObjectValid(Droid.GetDroid(Player)) &&
                   Perk.GetPerkLevel(Player, PerkType.DroidAssembly) >= Droid.LoadDroidItemPropertyDetails(_controller).Tier;
        }

        public Action AddInstructionDisk() => () =>
        {
            if (!CanEdit())
                return;
            var session = _programmingSession;
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please select an instruction disc from your inventory.", item =>
            {
                if (session != _programmingSession || !CanEdit())
                    return;
                var error = Droid.GetInstructionDiscValidationError(Player, item);
                if (!string.IsNullOrEmpty(error))
                {
                    SendMessageToPC(Player, ColorToken.Red(error));
                    return;
                }

                var droid = Droid.LoadConstructedDroid(_controller);
                var stats = Droid.LoadDroidItemPropertyDetails(_controller);
                var uploaded = new List<DroidPerk>();
                for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(property) != ItemPropertyType.DroidInstruction)
                        continue;
                    var perk = new DroidPerk((PerkType)GetItemPropertySubType(property), GetItemPropertyCostTableValue(property));
                    if (!DroidInstructions.TryGetLevel(perk, out _))
                    {
                        SendMessageToPC(Player, ColorToken.Red("This instruction disc does not contain a supported droid ability."));
                        return;
                    }
                    var tier = Perk.GetPerkLevelTier(perk.Perk, perk.Level);
                    if (tier > stats.Tier)
                    {
                        SendMessageToPC(Player, ColorToken.Red($"This instruction requires a tier {tier} droid."));
                        return;
                    }
                    if (droid.LearnedPerks.Concat(uploaded).Any(known => known.Perk == perk.Perk && known.Level == perk.Level))
                    {
                        SendMessageToPC(Player, ColorToken.Red("That instruction disc has already been uploaded to this droid."));
                        return;
                    }
                    uploaded.Add(perk);
                }
                if (uploaded.Count == 0)
                    return;
                droid.LearnedPerks.AddRange(uploaded);
                Droid.SaveConstructedDroid(_controller, droid);
                Item.ReduceItemStack(item, 1);
                RefreshPerks();
                SendMessageToPC(Player, ColorToken.Green("Instruction disc uploaded to droid successfully."));
            });
        };

        public Action CloseWindow() => () =>
        {
            _closed = true;
            SetItemCursedFlag(_controller, false);
        };

        public Action AddToActivePerks() => () =>
        {
            if (!CanEdit())
                return;
            var droid = Droid.LoadConstructedDroid(_controller);
            var stats = Droid.LoadDroidItemPropertyDetails(_controller);
            var selected = _availableDroidPerks.Where((_, index) => index < AvailablePerkSelections.Count && AvailablePerkSelections[index])
                .GroupBy(perk => perk.Perk).Select(group => group.OrderByDescending(perk => perk.Level).First());
            foreach (var perk in selected)
            {
                if (!DroidInstructions.TryGetLevel(perk, out var level) ||
                    Perk.GetPerkLevelTier(perk.Perk, perk.Level) > stats.Tier ||
                    !droid.LearnedPerks.Any(known => known.Perk == perk.Perk && known.Level == perk.Level))
                    continue;
                var slots = DroidInstructions.GetSlots(droid.ActivePerks.Where(active => active.Perk != perk.Perk));
                if (slots + level.DroidAISlots > stats.AISlots)
                    continue;
                droid.ActivePerks.RemoveAll(active => active.Perk == perk.Perk);
                droid.ActivePerks.Add(perk);
            }
            Droid.SaveInstructions(_controller, droid);
            RefreshPerks();
        };

        public Action RemoveFromActivePerks() => () =>
        {
            if (!CanEdit())
                return;
            var droid = Droid.LoadConstructedDroid(_controller);
            foreach (var perk in _activeDroidPerks.Where((_, index) => index < ActivePerkSelections.Count && ActivePerkSelections[index]))
                droid.ActivePerks.RemoveAll(active => active.Perk == perk.Perk && active.Level == perk.Level);
            Droid.SaveInstructions(_controller, droid);
            RefreshPerks();
        };
    }
}
