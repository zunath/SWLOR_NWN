using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Saved outfits: the Outfits tab manages the wardrobe, and the quick wear row under the
    /// equipment body map swaps between saved looks. Wearing keeps the previous look for one undo.
    /// </summary>
    public partial class AppearanceEditorViewModel
    {
        public const string OutfitsPartial = "OUTFITS_PARTIAL";

        private readonly List<string> _outfitIds = new();
        private readonly List<string> _quickOutfitIds = new();
        private int _selectedOutfitIndex = -1;
        private PlayerOutfit _undoLook;

        public int OutfitsTabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                OutfitsToggles.HandleClientChange(value, SelectEditorTab);
            }
        }

        public bool IsOutfitsSelected { get => Get<bool>(); set => Set(value); }

        /// <summary>Outfits belong to the player, so they appear only while players edit themselves.</summary>
        public bool IsOutfitsVisible { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> OutfitSlotNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> OutfitSlotToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string WardrobeCapacityText { get => Get<string>(); set => Set(value); }
        public bool IsOutfitSelected { get => Get<bool>(); set => Set(value); }
        public string OutfitDetails { get => Get<string>(); set => Set(value); }
        public bool IsOutfitRenameEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsOutfitWearEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsUndoOutfitEnabled { get => Get<bool>(); set => Set(value); }

        public string OutfitName
        {
            get => Get<string>();
            set
            {
                Set(value);
                IsOutfitRenameEnabled = _selectedOutfitIndex > -1;
            }
        }

        public GuiBindingList<GuiComboEntry> QuickOutfitOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int QuickOutfitValue { get => Get<int>(); set => Set(value); }
        public bool IsQuickWearEnabled { get => Get<bool>(); set => Set(value); }

        private bool IsPartEditorHidden => IsSettingsSelected || IsOutfitsSelected;

        private void InitializeOutfits()
        {
            _outfitIds.Clear();
            _quickOutfitIds.Clear();
            _selectedOutfitIndex = -1;
            _undoLook = null;
            IsOutfitsVisible = IsPlayerSettingsVisible && _target == Player;
            Set(-1, nameof(OutfitsTabToggleValue));
            IsOutfitsSelected = false;
            OutfitSlotNames = new GuiBindingList<string>();
            OutfitSlotToggles = new GuiBindingList<bool>();
            QuickOutfitOptions = new GuiBindingList<GuiComboEntry>();
            QuickOutfitValue = 0;
            WardrobeCapacityText = string.Empty;
            OutfitDetails = string.Empty;
            IsOutfitSelected = false;
            IsOutfitRenameEnabled = false;
            IsOutfitWearEnabled = false;
            IsUndoOutfitEnabled = false;
            IsQuickWearEnabled = false;
            Set(string.Empty, nameof(OutfitName));

            if (IsOutfitsVisible)
                LoadOutfits();
        }

        private void LoadOutfits()
        {
            var outfits = Wardrobe.GetOutfits(GetObjectUUID(_target));
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var quickOptions = new GuiBindingList<GuiComboEntry>();
            var selectedId = _selectedOutfitIndex > -1 && _selectedOutfitIndex < _outfitIds.Count
                ? _outfitIds[_selectedOutfitIndex]
                : null;
            var quickId = QuickOutfitValue >= 0 && QuickOutfitValue < _quickOutfitIds.Count
                ? _quickOutfitIds[QuickOutfitValue]
                : null;

            _outfitIds.Clear();
            _quickOutfitIds.Clear();
            _selectedOutfitIndex = -1;
            var quickValue = 0;
            foreach (var outfit in outfits)
            {
                if (outfit.Id == selectedId)
                    _selectedOutfitIndex = _outfitIds.Count;
                _outfitIds.Add(outfit.Id);
                names.Add(outfit.Name);
                toggles.Add(outfit.Id == selectedId);

                if (!Wardrobe.HasClothes(outfit))
                    continue;
                if (outfit.Id == quickId)
                    quickValue = _quickOutfitIds.Count;
                quickOptions.Add(new GuiComboEntry(outfit.Name, _quickOutfitIds.Count));
                _quickOutfitIds.Add(outfit.Id);
            }

            if (quickOptions.Count == 0)
                quickOptions.Add(new GuiComboEntry("No saved outfits", 0));

            OutfitSlotNames = names;
            OutfitSlotToggles = toggles;
            QuickOutfitOptions = quickOptions;
            QuickOutfitValue = quickValue;
            IsQuickWearEnabled = _quickOutfitIds.Count > 0;
            UpdateWardrobeCapacity(outfits.Count);
            ShowSelectedOutfit();
        }

        private void UpdateWardrobeCapacity(int outfitCount)
        {
            var limit = Wardrobe.GetSlotLimit(DB.Get<Player>(GetObjectUUID(_target)));
            WardrobeCapacityText = $"{outfitCount} / {limit} outfits";
        }

        private PlayerOutfit GetSelectedOutfit()
        {
            return _selectedOutfitIndex > -1 && _selectedOutfitIndex < _outfitIds.Count
                ? DB.Get<PlayerOutfit>(_outfitIds[_selectedOutfitIndex])
                : null;
        }

        private void ShowSelectedOutfit()
        {
            var outfit = GetSelectedOutfit();
            IsOutfitSelected = outfit != null;
            IsOutfitWearEnabled = Wardrobe.HasClothes(outfit);
            IsOutfitRenameEnabled = false;
            Set(outfit?.Name ?? string.Empty, nameof(OutfitName));
            OutfitDetails = outfit == null ? string.Empty : DescribeOutfit(outfit);
        }

        private static string DescribeOutfit(PlayerOutfit outfit)
        {
            if (!Wardrobe.HasClothes(outfit))
                return "No outfit is saved to this slot.\n\nWear the clothes and helmet you want, then choose Save Current Look.";

            return $"Helmet: {(Wardrobe.HasHelmet(outfit) ? $"#{outfit.HelmetId}" : "None saved")}\n\n" +
                   $"Neck: #{outfit.NeckId}\n" +
                   $"Torso: #{outfit.TorsoId}\n" +
                   $"Belt: #{outfit.BeltId}\n" +
                   $"Pelvis: #{outfit.PelvisId}\n" +
                   $"Robe: #{outfit.RobeId}\n" +
                   $"Shoulders: #{outfit.LeftShoulderId} / #{outfit.RightShoulderId}\n" +
                   $"Biceps: #{outfit.LeftBicepId} / #{outfit.RightBicepId}\n" +
                   $"Forearms: #{outfit.LeftForearmId} / #{outfit.RightForearmId}\n" +
                   $"Hands: #{outfit.LeftHandId} / #{outfit.RightHandId}\n" +
                   $"Thighs: #{outfit.LeftThighId} / #{outfit.RightThighId}\n" +
                   $"Shins: #{outfit.LeftShinId} / #{outfit.RightShinId}\n" +
                   $"Feet: #{outfit.LeftFootId} / #{outfit.RightFootId}\n\n" +
                   "Pairs are listed left / right.";
        }

        /// <summary>Input values are not re-sent after a layout swap, so push them again (R7).</summary>
        private void RepublishOutfitInputs()
        {
            if (!IsOutfitsVisible)
                return;
            Set(QuickOutfitValue, nameof(QuickOutfitValue));
            Set(OutfitName ?? string.Empty, nameof(OutfitName));
        }

        public Action OnClickOutfitSlot() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _outfitIds.Count)
                return;

            if (_selectedOutfitIndex > -1 && _selectedOutfitIndex < OutfitSlotToggles.Count)
                OutfitSlotToggles[_selectedOutfitIndex] = false;
            _selectedOutfitIndex = index;
            OutfitSlotToggles[index] = true;
            ShowSelectedOutfit();

            var quickIndex = _quickOutfitIds.IndexOf(_outfitIds[index]);
            if (quickIndex > -1)
                QuickOutfitValue = quickIndex;
        };

        public Action OnClickNewOutfit() => () =>
        {
            var playerId = GetObjectUUID(_target);
            var count = Wardrobe.CountOutfits(playerId);
            var limit = Wardrobe.GetSlotLimit(DB.Get<Player>(playerId));
            if (count >= limit)
            {
                FloatingTextStringOnCreature(limit < Wardrobe.MaxSlotLimit
                    ? $"Your wardrobe holds {limit} outfits. {Wardrobe.ExpansionHint}"
                    : $"Your wardrobe holds {limit} outfits.", _target, false);
                return;
            }

            var outfit = new PlayerOutfit
            {
                Name = $"Outfit #{count + 1}",
                PlayerId = playerId
            };
            DB.Set(outfit);
            _outfitIds.Add(outfit.Id);
            _selectedOutfitIndex = _outfitIds.Count - 1;
            LoadOutfits();
        };

        public Action OnClickDeleteOutfit() => () =>
        {
            if (GetSelectedOutfit() == null)
                return;

            ShowModal("Are you sure you want to delete this stored outfit?", () =>
            {
                var outfit = GetSelectedOutfit();
                if (outfit == null)
                    return;

                DB.Delete<PlayerOutfit>(outfit.Id);
                _selectedOutfitIndex = -1;
                LoadOutfits();
            });
        };

        public Action OnClickRenameOutfit() => () =>
        {
            var outfit = GetSelectedOutfit();
            var name = Wardrobe.ClampName(OutfitName);
            if (outfit == null || string.IsNullOrEmpty(name))
                return;

            outfit.Name = name;
            DB.Set(outfit);
            LoadOutfits();
        };

        public Action OnClickStoreOutfit() => () =>
        {
            var outfit = GetSelectedOutfit();
            if (outfit == null)
                return;

            void DoStore()
            {
                var current = GetSelectedOutfit();
                if (current == null)
                    return;

                Wardrobe.Store(_target, current, out var message);
                FloatingTextStringOnCreature(message, _target, false);
                LoadOutfits();
            }

            if (Wardrobe.HasClothes(outfit))
                ShowModal("Another outfit exists in this slot already. Are you sure you want to overwrite it?", DoStore);
            else
                DoStore();
        };

        public Action OnClickWearOutfit() => () => WearOutfit(GetSelectedOutfit());

        public Action OnClickQuickWear() => () =>
        {
            if (QuickOutfitValue < 0 || QuickOutfitValue >= _quickOutfitIds.Count)
                return;
            WearOutfit(DB.Get<PlayerOutfit>(_quickOutfitIds[QuickOutfitValue]));
        };

        public Action OnClickUndoOutfit() => () =>
        {
            if (_undoLook == null)
                return;

            var look = _undoLook;
            _undoLook = null;
            IsUndoOutfitEnabled = false;
            if (Wardrobe.Wear(_target, look, out var message))
                message = string.IsNullOrEmpty(message) ? "Restored your previous look." : message;
            FloatingTextStringOnCreature(message, _target, false);
            RefreshTintMapEditorAfterAppearanceChange(refreshItemParts: true);
        };

        /// <summary>Wears without a prompt; the look it replaces is kept for Undo.</summary>
        private void WearOutfit(PlayerOutfit outfit)
        {
            if (outfit == null)
                return;

            var previous = Wardrobe.CaptureWornLook(_target);
            if (!Wardrobe.Wear(_target, outfit, out var message))
            {
                FloatingTextStringOnCreature(message, _target, false);
                return;
            }

            _undoLook = previous;
            IsUndoOutfitEnabled = previous != null;
            FloatingTextStringOnCreature(string.IsNullOrEmpty(message) ? $"Now wearing {outfit.Name}." : message, _target, false);
            RefreshTintMapEditorAfterAppearanceChange(refreshItemParts: true);
        }
    }
}
