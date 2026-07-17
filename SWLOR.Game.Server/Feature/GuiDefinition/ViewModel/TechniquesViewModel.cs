using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using PlayerEntity = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TechniquesViewModel : GuiViewModelBase<TechniquesViewModel, TechniquesPayload>
    {
        private const string NoSelectionText = "Select a technique to view its details.";

        private List<FeatType> _unequippedFeats;
        private List<FeatType> _equippedFeats;
        private int _selectedUnequippedIndex = -1;
        private int _selectedEquippedIndex = -1;

        public GuiBindingList<string> UnequippedNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> UnequippedSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> EquippedNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> EquippedSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string SlotsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor SlotsColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string SelectedDetails
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsEquipEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsUnequipEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public TechniquesViewModel()
        {
            _unequippedFeats = new List<FeatType>();
            _equippedFeats = new List<FeatType>();
        }

        protected override void Initialize(TechniquesPayload initialPayload)
        {
            SelectedDetails = NoSelectionText;
            IsEquipEnabled = false;
            IsUnequipEnabled = false;

            LoadLists();
        }

        private void LoadLists()
        {
            ClearSelections();

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<PlayerEntity>(playerId);
            var learnedFeats = Mimicry.GetLearnedTechniques(dbPlayer);
            var equippedFeats = Mimicry.GetEquippedTechniques(dbPlayer);

            var unequippedNames = new GuiBindingList<string>();
            var unequippedSelections = new GuiBindingList<bool>();
            var equippedNames = new GuiBindingList<string>();
            var equippedSelections = new GuiBindingList<bool>();

            _unequippedFeats = new List<FeatType>();
            _equippedFeats = new List<FeatType>();

            var equippedFeatSet = equippedFeats.Select(x => x.Feat).ToHashSet();

            foreach (var (feat, detail) in learnedFeats)
            {
                if (equippedFeatSet.Contains(feat))
                    continue;

                _unequippedFeats.Add(feat);
                unequippedNames.Add(BuildRowText(detail));
                unequippedSelections.Add(false);
            }

            foreach (var (feat, detail) in equippedFeats)
            {
                _equippedFeats.Add(feat);
                equippedNames.Add(BuildRowText(detail));
                equippedSelections.Add(false);
            }

            UnequippedNames = unequippedNames;
            UnequippedSelections = unequippedSelections;
            EquippedNames = equippedNames;
            EquippedSelections = equippedSelections;

            RefreshSlots(dbPlayer);
        }

        private static string BuildRowText(AbilityDetail detail)
        {
            var slotLabel = detail.MimicrySlotCost == 1 ? "slot" : "slots";
            return $"{detail.Name} (T{detail.MimicryTier} / {detail.MimicrySlotCost} {slotLabel})";
        }

        private void RefreshSlots(PlayerEntity dbPlayer)
        {
            var used = Mimicry.GetUsedSlots(dbPlayer);
            var max = Mimicry.GetMaxSlots(dbPlayer);

            SlotsText = $"Slots: {used} / {max}";
            SlotsColor = used >= max ? GuiColor.Red : GuiColor.White;
        }

        private void ClearSelections()
        {
            // The bound selection lists are replaced wholesale by LoadLists (the only caller),
            // so only the index and UI state resets matter here.
            _selectedUnequippedIndex = -1;
            _selectedEquippedIndex = -1;
            IsEquipEnabled = false;
            IsUnequipEnabled = false;
            SelectedDetails = NoSelectionText;
        }

        private void ShowDetails(FeatType feat)
        {
            var detail = Mimicry.GetTechniqueDetail(feat);

            var text = $"{detail.Name}\n\n";

            var description = GetTechniqueDescription(feat);
            if (!string.IsNullOrWhiteSpace(description))
                text += $"{description}\n\n";

            text += $"Tier: {detail.MimicryTier}\n" +
                    $"Slot Cost: {detail.MimicrySlotCost}\n";

            if (detail.IsMimicryTrait)
            {
                // Traits are passive: they are never cast, so stamina/recast do not apply.
                text += "Type: Passive Trait";
            }
            else
            {
                var staminaCost = detail.Requirements
                    .OfType<AbilityRequirementStamina>()
                    .Select(x => x.RequiredSTM)
                    .DefaultIfEmpty(0)
                    .First();

                var recastSeconds = detail.RecastDelay != null
                    ? detail.RecastDelay(Player)
                    : 0f;

                text += $"Stamina Cost: {staminaCost}\n" +
                        $"Recast: {recastSeconds:0.#}s";
            }

            SelectedDetails = text;
        }

        // Techniques carry no description in their AbilityDetail; the player-facing text lives in the
        // feat.2da DESCRIPTION strref (a custom TLK entry). Resolve it on demand for the details pane.
        private static string GetTechniqueDescription(FeatType feat)
        {
            var strRefText = Get2DAString("feat", "DESCRIPTION", (int)feat);
            if (!int.TryParse(strRefText, out var strRef) || strRef <= 0)
                return string.Empty;

            return GetStringByStrRef(strRef);
        }

        public Action OnSelectUnequipped() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _unequippedFeats.Count)
                return;

            if (_selectedEquippedIndex > -1 && _selectedEquippedIndex < EquippedSelections.Count)
            {
                EquippedSelections[_selectedEquippedIndex] = false;
                _selectedEquippedIndex = -1;
            }

            if (_selectedUnequippedIndex > -1 && _selectedUnequippedIndex < UnequippedSelections.Count)
            {
                UnequippedSelections[_selectedUnequippedIndex] = false;
            }

            _selectedUnequippedIndex = index;
            UnequippedSelections[index] = true;

            ShowDetails(_unequippedFeats[index]);

            IsEquipEnabled = true;
            IsUnequipEnabled = false;
        };

        public Action OnSelectEquipped() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _equippedFeats.Count)
                return;

            if (_selectedUnequippedIndex > -1 && _selectedUnequippedIndex < UnequippedSelections.Count)
            {
                UnequippedSelections[_selectedUnequippedIndex] = false;
                _selectedUnequippedIndex = -1;
            }

            if (_selectedEquippedIndex > -1 && _selectedEquippedIndex < EquippedSelections.Count)
            {
                EquippedSelections[_selectedEquippedIndex] = false;
            }

            _selectedEquippedIndex = index;
            EquippedSelections[index] = true;

            ShowDetails(_equippedFeats[index]);

            IsEquipEnabled = false;
            IsUnequipEnabled = true;
        };

        public Action OnClickEquip() => () =>
        {
            if (_selectedUnequippedIndex < 0 || _selectedUnequippedIndex >= _unequippedFeats.Count)
                return;

            var feat = _unequippedFeats[_selectedUnequippedIndex];

            // EquipTechnique validates and reports failures to the player itself.
            if (Mimicry.EquipTechnique(Player, feat))
            {
                LoadLists();
            }
        };

        public Action OnClickUnequip() => () =>
        {
            if (_selectedEquippedIndex < 0 || _selectedEquippedIndex >= _equippedFeats.Count)
                return;

            var feat = _equippedFeats[_selectedEquippedIndex];

            Mimicry.UnequipTechnique(Player, feat);

            LoadLists();
        };
    }
}
