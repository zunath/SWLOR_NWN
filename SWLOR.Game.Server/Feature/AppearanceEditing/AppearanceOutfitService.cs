using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>
    /// Saved outfits: the Outfits tab manages the wardrobe, and the quick wear row under the equipment body map swaps between
    /// saved looks. Wearing keeps the previous look for one undo.
    /// </summary>
    public class AppearanceOutfitService
    {
        private IDBService DB { get; }
        private IWardrobeService Wardrobe { get; }

        public AppearanceOutfitService(IDBService dB, IWardrobeService wardrobe)
        {
            DB = dB;
            Wardrobe = wardrobe;
        }

        public void InitializeOutfits(IAppearanceEditorHost host)
        {
            host.Session.OutfitIds.Clear();
            host.Session.QuickOutfitIds.Clear();
            host.Session.SelectedOutfitIndex = -1;
            host.Session.UndoLook = null;
            host.IsOutfitsVisible = host.IsPlayerSettingsVisible && host.Session.Target == host.Player;
            host.Set(-1, nameof(AppearanceEditorViewModel.OutfitsTabToggleValue));
            host.IsOutfitsSelected = false;
            host.OutfitSlotNames = new GuiBindingList<string>();
            host.OutfitSlotToggles = new GuiBindingList<bool>();
            host.QuickOutfitOptions = new GuiBindingList<GuiComboEntry>();
            host.QuickOutfitValue = 0;
            host.WardrobeCapacityText = string.Empty;
            host.OutfitDetails = string.Empty;
            host.IsOutfitSelected = false;
            host.IsOutfitRenameEnabled = false;
            host.IsOutfitWearEnabled = false;
            host.IsUndoOutfitEnabled = false;
            host.IsQuickWearEnabled = false;
            host.Set(string.Empty, nameof(AppearanceEditorViewModel.OutfitName));

            if (host.IsOutfitsVisible)
                LoadOutfits(host);
        }

        public void LoadOutfits(IAppearanceEditorHost host)
        {
            var outfits = Wardrobe.GetOutfits(GetObjectUUID(host.Session.Target));
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var quickOptions = new GuiBindingList<GuiComboEntry>();
            var selectedId = host.Session.SelectedOutfitIndex > -1 && host.Session.SelectedOutfitIndex < host.Session.OutfitIds.Count
                ? host.Session.OutfitIds[host.Session.SelectedOutfitIndex]
                : null;
            var quickId = host.QuickOutfitValue >= 0 && host.QuickOutfitValue < host.Session.QuickOutfitIds.Count
                ? host.Session.QuickOutfitIds[host.QuickOutfitValue]
                : null;

            host.Session.OutfitIds.Clear();
            host.Session.QuickOutfitIds.Clear();
            host.Session.SelectedOutfitIndex = -1;
            var quickValue = 0;
            foreach (var outfit in outfits)
            {
                if (outfit.Id == selectedId)
                    host.Session.SelectedOutfitIndex = host.Session.OutfitIds.Count;
                host.Session.OutfitIds.Add(outfit.Id);
                names.Add(outfit.Name);
                toggles.Add(outfit.Id == selectedId);

                if (!Wardrobe.HasClothes(outfit))
                    continue;
                if (outfit.Id == quickId)
                    quickValue = host.Session.QuickOutfitIds.Count;
                quickOptions.Add(new GuiComboEntry(outfit.Name, host.Session.QuickOutfitIds.Count));
                host.Session.QuickOutfitIds.Add(outfit.Id);
            }

            if (quickOptions.Count == 0)
                quickOptions.Add(new GuiComboEntry("No saved outfits", 0));

            host.OutfitSlotNames = names;
            host.OutfitSlotToggles = toggles;
            host.QuickOutfitOptions = quickOptions;
            host.QuickOutfitValue = quickValue;
            host.IsQuickWearEnabled = host.Session.QuickOutfitIds.Count > 0;
            UpdateWardrobeCapacity(host, outfits.Count);
            ShowSelectedOutfit(host);
        }

        private void UpdateWardrobeCapacity(IAppearanceEditorHost host, int outfitCount)
        {
            var limit = Wardrobe.GetSlotLimit(DB.Get<Player>(GetObjectUUID(host.Session.Target)));
            host.WardrobeCapacityText = $"{outfitCount} / {limit} outfits";
        }

        private PlayerOutfit GetSelectedOutfit(IAppearanceEditorHost host)
        {
            return host.Session.SelectedOutfitIndex > -1 && host.Session.SelectedOutfitIndex < host.Session.OutfitIds.Count
                ? DB.Get<PlayerOutfit>(host.Session.OutfitIds[host.Session.SelectedOutfitIndex])
                : null;
        }

        private void ShowSelectedOutfit(IAppearanceEditorHost host)
        {
            var outfit = GetSelectedOutfit(host);
            host.IsOutfitSelected = outfit != null;
            host.IsOutfitWearEnabled = Wardrobe.HasClothes(outfit);
            host.IsOutfitRenameEnabled = false;
            host.Set(outfit?.Name ?? string.Empty, nameof(AppearanceEditorViewModel.OutfitName));
            host.OutfitDetails = outfit == null ? string.Empty : DescribeOutfit(outfit);
        }

        private string DescribeOutfit(PlayerOutfit outfit)
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
        public void RepublishOutfitInputs(IAppearanceEditorHost host)
        {
            if (!host.IsOutfitsVisible)
                return;
            host.Set(host.QuickOutfitValue, nameof(AppearanceEditorViewModel.QuickOutfitValue));
            host.Set(host.OutfitName ?? string.Empty, nameof(AppearanceEditorViewModel.OutfitName));
        }

        public void ClickOutfitSlot(IAppearanceEditorHost host)
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= host.Session.OutfitIds.Count)
                return;

            if (host.Session.SelectedOutfitIndex > -1 && host.Session.SelectedOutfitIndex < host.OutfitSlotToggles.Count)
                host.OutfitSlotToggles[host.Session.SelectedOutfitIndex] = false;
            host.Session.SelectedOutfitIndex = index;
            host.OutfitSlotToggles[index] = true;
            ShowSelectedOutfit(host);

            var quickIndex = host.Session.QuickOutfitIds.IndexOf(host.Session.OutfitIds[index]);
            if (quickIndex > -1)
                host.QuickOutfitValue = quickIndex;
        }

        public void ClickNewOutfit(IAppearanceEditorHost host)
        {
            var playerId = GetObjectUUID(host.Session.Target);
            var count = Wardrobe.CountOutfits(playerId);
            var limit = Wardrobe.GetSlotLimit(DB.Get<Player>(playerId));
            if (count >= limit)
            {
                FloatingTextStringOnCreature(limit < WardrobeService.MaxSlotLimit
                    ? $"Your wardrobe holds {limit} outfits. {WardrobeService.ExpansionHint}"
                    : $"Your wardrobe holds {limit} outfits.", host.Session.Target, false);
                return;
            }

            var outfit = new PlayerOutfit
            {
                Name = $"Outfit #{count + 1}",
                PlayerId = playerId
            };
            DB.Set(outfit);
            host.Session.OutfitIds.Add(outfit.Id);
            host.Session.SelectedOutfitIndex = host.Session.OutfitIds.Count - 1;
            LoadOutfits(host);
        }

        public void ClickDeleteOutfit(IAppearanceEditorHost host)
        {
            if (GetSelectedOutfit(host) == null)
                return;

            host.ShowModal("Are you sure you want to delete this stored outfit?", () =>
            {
                var outfit = GetSelectedOutfit(host);
                if (outfit == null)
                    return;

                DB.Delete<PlayerOutfit>(outfit.Id);
                host.Session.SelectedOutfitIndex = -1;
                LoadOutfits(host);
            });
        }

        public void ClickRenameOutfit(IAppearanceEditorHost host)
        {
            var outfit = GetSelectedOutfit(host);
            var name = Wardrobe.ClampName(host.OutfitName);
            if (outfit == null || string.IsNullOrEmpty(name))
                return;

            outfit.Name = name;
            DB.Set(outfit);
            LoadOutfits(host);
        }

        public void ClickStoreOutfit(IAppearanceEditorHost host)
        {
            var outfit = GetSelectedOutfit(host);
            if (outfit == null)
                return;

            void DoStore()
            {
                var current = GetSelectedOutfit(host);
                if (current == null)
                    return;

                Wardrobe.Store(host.Session.Target, current, out var message);
                FloatingTextStringOnCreature(message, host.Session.Target, false);
                LoadOutfits(host);
            }

            if (Wardrobe.HasClothes(outfit))
                host.ShowModal("Another outfit exists in this slot already. Are you sure you want to overwrite it?", DoStore);
            else
                DoStore();
        }

        public void ClickWearOutfit(IAppearanceEditorHost host) => WearOutfit(host, GetSelectedOutfit(host));

        public void ClickQuickWear(IAppearanceEditorHost host)
        {
            if (host.QuickOutfitValue < 0 || host.QuickOutfitValue >= host.Session.QuickOutfitIds.Count)
                return;
            WearOutfit(host, DB.Get<PlayerOutfit>(host.Session.QuickOutfitIds[host.QuickOutfitValue]));
        }

        public void ClickUndoOutfit(IAppearanceEditorHost host)
        {
            if (host.Session.UndoLook == null)
                return;

            var look = host.Session.UndoLook;
            host.Session.UndoLook = null;
            host.IsUndoOutfitEnabled = false;
            if (Wardrobe.Wear(host.Session.Target, look, out var message))
                message = string.IsNullOrEmpty(message) ? "Restored your previous look." : message;
            FloatingTextStringOnCreature(message, host.Session.Target, false);
            host.RefreshTintMapEditorAfterAppearanceChange(refreshItemParts: true);
        }

        /// <summary>Wears without a prompt; the look it replaces is kept for Undo.</summary>
        private void WearOutfit(IAppearanceEditorHost host, PlayerOutfit outfit)
        {
            if (outfit == null)
                return;

            var previous = Wardrobe.CaptureWornLook(host.Session.Target);
            if (!Wardrobe.Wear(host.Session.Target, outfit, out var message))
            {
                FloatingTextStringOnCreature(message, host.Session.Target, false);
                return;
            }

            host.Session.UndoLook = previous;
            host.IsUndoOutfitEnabled = previous != null;
            FloatingTextStringOnCreature(string.IsNullOrEmpty(message) ? $"Now wearing {outfit.Name}." : message, host.Session.Target, false);
            host.RefreshTintMapEditorAfterAppearanceChange(refreshItemParts: true);
        }
    }
}
