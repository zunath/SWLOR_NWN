using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class OutfitViewModel: GuiViewModelBase<OutfitViewModel, GuiPayloadBase>
    {
        private const int MaxOutfits = 25;

        private List<string> _outfitIds = new();

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
            var dbOutfits = DB.Search(new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false));

            return dbOutfits.ToList();
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
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

            var dbOutfit = DB.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);
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
            var dbOutfit = DB.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);

            if (Name.Length > 32)
                Name = Name.Substring(0, 32);

            dbOutfit.Name = Name;

            DB.Set(dbOutfit);
            IsSaveEnabled = false;
            SlotNames[SelectedSlotIndex] = Name;
        };

        public Action OnClickStoreOutfit() => () =>
        {
            var dbOutfit = DB.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);

            void DoSave()
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, Player);
                if (!GetIsObjectValid(outfit))
                {
                    FloatingTextStringOnCreature("You do not have any clothes equipped.", Player, false);
                    return;
                }

                dbOutfit.Data = ObjectPlugin.Serialize(outfit);


                dbOutfit.NeckId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
                dbOutfit.TorsoId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso);
                dbOutfit.BeltId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt);
                dbOutfit.PelvisId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis);
                dbOutfit.RobeId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);

                dbOutfit.LeftBicepId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep);
                dbOutfit.LeftFootId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot);
                dbOutfit.LeftForearmId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm);
                dbOutfit.LeftHandId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand);
                dbOutfit.LeftShinId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin);
                dbOutfit.LeftShoulderId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder);
                dbOutfit.LeftThighId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh);

                dbOutfit.RightBicepId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep);
                dbOutfit.RightFootId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot);
                dbOutfit.RightForearmId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm);
                dbOutfit.RightHandId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand);
                dbOutfit.RightShinId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin);
                dbOutfit.RightShoulderId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder);
                dbOutfit.RightThighId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh);

                DB.Set(dbOutfit);

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
                    var outfit = GetItemInSlot(InventorySlot.Chest, Player);
                    if (!GetIsObjectValid(outfit))
                    {
                        FloatingTextStringOnCreature("You do not have any clothes equipped.", Player, false);
                        return;
                    }

                    LoadOutfit();
                });
        };

        private int CalculatePerPartColorIndex(AppearanceArmor armorModel, AppearanceArmorColor colorChannel)
        {
            return (int)AppearanceArmorColor.NumColors + (int)armorModel * (int)AppearanceArmorColor.NumColors + (int)colorChannel;
        }

        private void LoadOutfit()
        {
            var armor = GetItemInSlot(InventorySlot.Chest, Player);
            var dbOutfit = DB.Get<PlayerOutfit>(_outfitIds[SelectedSlotIndex]);

            // Get the temporary storage placeable and deserialize the outfit into it.
            var tempStorage = GetObjectByTag("OUTFIT_BARREL");
            var deserialized = ObjectPlugin.Deserialize(dbOutfit.Data);
            var copy = CopyItem(armor, tempStorage, true);

            uint CopyColors(AppearanceArmor part)
            {
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)part, GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)part), true);
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)part, GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)part), true);
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Cloth1), GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Cloth1)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Cloth2), GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Cloth2)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Leather1), GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Leather1)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Leather2), GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Leather2)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Metal1), GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Metal1)));
                DestroyObject(copy);
                copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Metal2), GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, CalculatePerPartColorIndex(part, AppearanceArmorColor.Metal2)));
                DestroyObject(copy);

                return copy;
            }

            copy = CopyColors(AppearanceArmor.LeftBicep);
            copy = CopyColors(AppearanceArmor.Belt);
            copy = CopyColors(AppearanceArmor.LeftFoot);
            copy = CopyColors(AppearanceArmor.LeftForearm);
            copy = CopyColors(AppearanceArmor.LeftHand);
            copy = CopyColors(AppearanceArmor.LeftShin);
            copy = CopyColors(AppearanceArmor.LeftShoulder);
            copy = CopyColors(AppearanceArmor.LeftThigh);
            copy = CopyColors(AppearanceArmor.Neck);
            copy = CopyColors(AppearanceArmor.Pelvis);
            copy = CopyColors(AppearanceArmor.RightBicep);
            copy = CopyColors(AppearanceArmor.RightFoot);
            copy = CopyColors(AppearanceArmor.RightForearm);
            copy = CopyColors(AppearanceArmor.RightHand);
            copy = CopyColors(AppearanceArmor.Robe);
            copy = CopyColors(AppearanceArmor.RightShin);
            copy = CopyColors(AppearanceArmor.RightShoulder);
            copy = CopyColors(AppearanceArmor.RightThigh);
            copy = CopyColors(AppearanceArmor.Torso);

            var final = CopyItem(copy, Player, true);
            DestroyObject(armor);
            DestroyObject(copy);
            DestroyObject(deserialized);

            AssignCommand(Player, () =>
            {
                ActionEquipItem(final, InventorySlot.Chest);
            });
        }

        public Action OnClickNew() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var outfitCount = DB.SearchCount(new DBQuery<PlayerOutfit>()
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
            
            DB.Set(newOutfit);
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
                var dbOutfit = DB.Get<PlayerOutfit>(outfitId);
                
                DB.Delete<PlayerOutfit>(dbOutfit.Id);

                _outfitIds.RemoveAt(SelectedSlotIndex);
                SlotNames.RemoveAt(SelectedSlotIndex);
                SlotToggles.RemoveAt(SelectedSlotIndex);

                SelectedSlotIndex = -1;
                IsSlotLoaded = false;
            });
        };

    }
}
