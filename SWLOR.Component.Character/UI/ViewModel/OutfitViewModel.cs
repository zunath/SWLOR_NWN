using SWLOR.Component.Character.Enums;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public class OutfitViewModel: GuiViewModelBase<OutfitViewModel, IGuiPayload>
    {
        private readonly IDatabaseService _db;

        public OutfitViewModel(IGuiService guiService, IDatabaseService db) : base(guiService)
        {
            _db = db;
        }
        
        private const int MaxOutfits = 25;

        private readonly List<string> _outfitIds = new();

        public GuiBindingList<string> SlotNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> SlotToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedSlotIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool IsSaveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsLoadEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSlotLoaded
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsDeleteEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set
            {
                Set(value);
                IsSaveEnabled = true;
            }
        }

        public string Details
        {
            get => Get<string>();
            set => Set(value);
        }

        private List<PlayerOutfit> GetOutfits()
        {
            var playerId = GetObjectUUID(Player);
            var dbOutfits = _db.Search(new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false));

            return dbOutfits.ToList();
        }

        protected override void Initialize(IGuiPayload initialPayload)
        {
            SelectedSlotIndex = -1;
            IsDeleteEnabled = false;
            Name = string.Empty;

            var dbOutfits = GetOutfits();
            var slotNames = new GuiBindingList<string>();
            var slotToggles = new GuiBindingList<bool>();
            _outfitIds.Clear();

            foreach (var outfit in dbOutfits)
            {
                _outfitIds.Add(outfit.Id);
                slotNames.Add(outfit.Name);
                slotToggles.Add(false);
            }

            SlotNames = slotNames;
            SlotToggles = slotToggles;
            IsSlotLoaded = false;

            WatchOnClient(model => model.Name);
        }

        public Action OnClickSlot() => () =>
        {
            if (SelectedSlotIndex > -1)
            {
                SlotToggles[SelectedSlotIndex] = false;
            }
            SelectedSlotIndex = NuiGetEventArrayIndex();

            var dbOutfit = _db.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);
            IsDeleteEnabled = true;
            IsSlotLoaded = true;
            IsLoadEnabled = !string.IsNullOrWhiteSpace(dbOutfit.Data);

            Name = dbOutfit.Name;
            SlotToggles[SelectedSlotIndex] = true;
            UpdateDetails(dbOutfit);
        };

        private void UpdateDetails(PlayerOutfit detail)
        {
            if (string.IsNullOrWhiteSpace(detail.Data))
            {
                Details = "No outfit is saved to this slot.";
            }
            else
            {
                var details = $"Neck: #{detail.NeckId}\n" +
                              $"Torso: #{detail.TorsoId}\n" +
                              $"Belt: #{detail.BeltId}\n" +
                              $"Pelvis: #{detail.PelvisId}\n" +
                              $"Robe: #{detail.RobeId}\n" +

                              $"Left Bicep: #{detail.LeftBicepId}\n" +
                              $"Left Foot: #{detail.LeftFootId}\n" +
                              $"Left Forearm: #{detail.LeftForearmId}\n" +
                              $"Left Hand: #{detail.LeftHandId}\n" +
                              $"Left Shin: #{detail.LeftShinId}\n" +
                              $"Left Shoulder: #{detail.LeftShoulderId}\n" +
                              $"Left Thigh: #{detail.LeftThighId}\n" +

                              $"Right Bicep: #{detail.RightBicepId}\n" +
                              $"Right Foot: #{detail.RightFootId}\n" +
                              $"Right Forearm: #{detail.RightForearmId}\n" +
                              $"Right Hand: #{detail.RightHandId}\n" +
                              $"Right Shin: #{detail.RightShinId}\n" +
                              $"Right Shoulder: #{detail.RightShoulderId}\n" +
                              $"Right Thigh: #{detail.RightThighId}\n";

                Details = details;
            }
        }

        public Action OnClickSave() => () =>
        {
            var dbOutfit = _db.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);

            if (Name.Length > 32)
                Name = Name.Substring(0, 32);

            dbOutfit.Name = Name;

            _db.Set(dbOutfit);
            IsSaveEnabled = false;
            SlotNames[SelectedSlotIndex] = Name;
        };

        public Action OnClickStoreOutfit() => () =>
        {
            var dbOutfit = _db.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);

            void DoSave()
            {
                var outfit = GetItemInSlot(InventorySlotType.Chest, Player);
                if (!GetIsObjectValid(outfit))
                {
                    FloatingTextStringOnCreature("You do not have any clothes equipped.", Player, false);
                    return;
                }

                dbOutfit.Data = ObjectPlugin.Serialize(outfit);


                dbOutfit.NeckId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Neck);
                dbOutfit.TorsoId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Torso);
                dbOutfit.BeltId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Belt);
                dbOutfit.PelvisId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Pelvis);
                dbOutfit.RobeId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.Robe);

                dbOutfit.LeftBicepId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftBicep);
                dbOutfit.LeftFootId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftFoot);
                dbOutfit.LeftForearmId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftForearm);
                dbOutfit.LeftHandId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftHand);
                dbOutfit.LeftShinId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShin);
                dbOutfit.LeftShoulderId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftShoulder);
                dbOutfit.LeftThighId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.LeftThigh);

                dbOutfit.RightBicepId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightBicep);
                dbOutfit.RightFootId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightFoot);
                dbOutfit.RightForearmId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightForearm);
                dbOutfit.RightHandId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightHand);
                dbOutfit.RightShinId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShin);
                dbOutfit.RightShoulderId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightShoulder);
                dbOutfit.RightThighId = GetItemAppearance(outfit, ItemModelColorType.ArmorModel, (int)ItemAppearanceArmorType.RightThigh);

                _db.Set(dbOutfit);

                IsLoadEnabled = true;
                UpdateDetails(dbOutfit);
            }

            // Nothing is saved to this slot yet.
            if (string.IsNullOrWhiteSpace(dbOutfit.Data))
            {
                DoSave();
            }
            // Something else is here. Prompt the user first.
            else
            {
                ShowModal($"Another outfit exists in this slot already. Are you sure you want to overwrite it?", DoSave);
            }

        };

        public Action OnClickLoadOutfit() => () =>
        {
            ShowModal($"Loading this outfit will overwrite the appearance of your currently equipped clothes. Are you sure you want to do this?",
                () =>
                {
                    var outfit = GetItemInSlot(InventorySlotType.Chest, Player);
                    if (!GetIsObjectValid(outfit))
                    {
                        FloatingTextStringOnCreature("You do not have any clothes equipped.", Player, false);
                        return;
                    }

                    LoadOutfit();
                });
        };

        private int CalculatePerPartColorIndex(ItemAppearanceArmorType armorModel, ItemAppearanceArmorColorType colorChannel)
        {
            return (int)ItemAppearanceArmorColorType.NumColors + (int)armorModel * (int)ItemAppearanceArmorColorType.NumColors + (int)colorChannel;
        }

        private void LoadOutfit()
        {
            var armor = GetItemInSlot(InventorySlotType.Chest, Player);
            var dbOutfit = _db.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);

            // Get the temporary storage placeable and deserialize the outfit into it.
            var tempStorage = GetObjectByTag("OUTFIT_BARREL");
            var deserialized = ObjectPlugin.Deserialize(dbOutfit.Data);
            var copy = CopyItem(armor, tempStorage, true);

            uint CopyColors(ItemAppearanceArmorType part)
            {
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorModel, (int)part, GetItemAppearance(deserialized, ItemModelColorType.ArmorModel, (int)part), true);
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorColor, (int)part, GetItemAppearance(deserialized, ItemModelColorType.ArmorColor, (int)part), true);
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Cloth1), GetItemAppearance(deserialized, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Cloth1)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Cloth2), GetItemAppearance(deserialized, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Cloth2)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Leather1), GetItemAppearance(deserialized, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Leather1)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Leather2), GetItemAppearance(deserialized, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Leather2)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Metal1), GetItemAppearance(deserialized, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Metal1)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Metal2), GetItemAppearance(deserialized, ItemModelColorType.ArmorColor, CalculatePerPartColorIndex(part, ItemAppearanceArmorColorType.Metal2)));
                DestroyObject(copy);

                return copy;
            }

            copy = CopyColors(ItemAppearanceArmorType.LeftBicep);
            copy = CopyColors(ItemAppearanceArmorType.Belt);
            copy = CopyColors(ItemAppearanceArmorType.LeftFoot);
            copy = CopyColors(ItemAppearanceArmorType.LeftForearm);
            copy = CopyColors(ItemAppearanceArmorType.LeftHand);
            copy = CopyColors(ItemAppearanceArmorType.LeftShin);
            copy = CopyColors(ItemAppearanceArmorType.LeftShoulder);
            copy = CopyColors(ItemAppearanceArmorType.LeftThigh);
            copy = CopyColors(ItemAppearanceArmorType.Neck);
            copy = CopyColors(ItemAppearanceArmorType.Pelvis);
            copy = CopyColors(ItemAppearanceArmorType.RightBicep);
            copy = CopyColors(ItemAppearanceArmorType.RightFoot);
            copy = CopyColors(ItemAppearanceArmorType.RightForearm);
            copy = CopyColors(ItemAppearanceArmorType.RightHand);
            copy = CopyColors(ItemAppearanceArmorType.Robe);
            copy = CopyColors(ItemAppearanceArmorType.RightShin);
            copy = CopyColors(ItemAppearanceArmorType.RightShoulder);
            copy = CopyColors(ItemAppearanceArmorType.RightThigh);
            copy = CopyColors(ItemAppearanceArmorType.Torso);

            var final = CopyItem(copy, Player, true);
            DestroyObject(armor);
            DestroyObject(copy);
            DestroyObject(deserialized);

            AssignCommand(Player, () =>
            {
                ActionEquipItem(final, InventorySlotType.Chest);
            });
        }

        public Action OnClickNew() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var outfitCount = _db.SearchCount(new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false));

            if (outfitCount >= MaxOutfits)
            {
                FloatingTextStringOnCreature($"You may only create {MaxOutfits} outfits.", Player, false);
                return;
            }

            var newOutfit = new PlayerOutfit
            {
                Name = $"Outfit #{outfitCount+1}",
                PlayerId = playerId
            };
            
            _db.Set(newOutfit);
            _outfitIds.Add(newOutfit.Id);
            SlotNames.Add(newOutfit.Name);
            SlotToggles.Add(false);
        };

        public Action OnClickDelete() => () =>
        {
            ShowModal("Are you sure you want to delete this stored outfit?", () =>
            {
                if (SelectedSlotIndex < 0)
                    return;

                var outfitId = _outfitIds[SelectedSlotIndex];
                var dbOutfit = _db.Get<PlayerOutfit>(outfitId);
                
                _db.Delete<PlayerOutfit>(dbOutfit.Id);

                _outfitIds.RemoveAt(SelectedSlotIndex);
                SlotNames.RemoveAt(SelectedSlotIndex);
                SlotToggles.RemoveAt(SelectedSlotIndex);

                SelectedSlotIndex = -1;
                IsSlotLoaded = false;
            });
        };

    }
}
