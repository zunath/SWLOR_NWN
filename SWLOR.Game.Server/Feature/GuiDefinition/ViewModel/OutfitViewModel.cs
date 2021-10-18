using System;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class OutfitViewModel: GuiViewModelBase<OutfitViewModel>
    {
        private const int MaxOutfits = 25;

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

        public Action OnLoadWindow() => () =>
        {
            SelectedSlotIndex = -1;
            IsDeleteEnabled = false;
            Name = string.Empty;

            var playerId = GetObjectUUID(Player);
            var dbOutfits = DB.Get<PlayerOutfit>(playerId) ?? new PlayerOutfit();
            var slotNames = new GuiBindingList<string>();
            var slotToggles = new GuiBindingList<bool>();

            foreach (var outfit in dbOutfits.Outfits)
            {
                slotNames.Add(outfit.Name);
                slotToggles.Add(false);
            }

            SlotNames = slotNames;
            SlotToggles = slotToggles;
            IsSlotLoaded = false;

            WatchOnClient(model => model.Name);
        };

        public Action OnClickSlot() => () =>
        {
            if (SelectedSlotIndex > -1)
            {
                SlotToggles[SelectedSlotIndex] = false;
            }

            var playerId = GetObjectUUID(Player);
            var dbOutfit = DB.Get<PlayerOutfit>(playerId) ?? new PlayerOutfit();
            SelectedSlotIndex = NuiGetEventArrayIndex();
            IsDeleteEnabled = true;
            IsSlotLoaded = true;
            IsLoadEnabled = !string.IsNullOrWhiteSpace(dbOutfit.Outfits[SelectedSlotIndex].Data);

            Name = dbOutfit.Outfits[SelectedSlotIndex].Name;
            SlotToggles[SelectedSlotIndex] = true;
            UpdateDetails(dbOutfit.Outfits[SelectedSlotIndex]);
        };

        private void UpdateDetails(PlayerOutfitDetail detail)
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
            var playerId = GetObjectUUID(Player);
            var dbOutfit = DB.Get<PlayerOutfit>(playerId) ?? new PlayerOutfit();

            if (Name.Length > 32)
                Name = Name.Substring(0, 32);

            dbOutfit.Outfits[SelectedSlotIndex].Name = Name;

            DB.Set(playerId, dbOutfit);
            IsSaveEnabled = false;
            SlotNames[SelectedSlotIndex] = Name;
        };

        public Action OnClickStoreOutfit() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbOutfit = DB.Get<PlayerOutfit>(playerId) ?? new PlayerOutfit();

            void DoSave()
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, Player);
                if (!GetIsObjectValid(outfit))
                {
                    FloatingTextStringOnCreature("You do not have any clothes equipped.", Player, false);
                    return;
                }

                dbOutfit.Outfits[SelectedSlotIndex].Data = ObjectPlugin.Serialize(outfit);


                dbOutfit.Outfits[SelectedSlotIndex].NeckId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
                dbOutfit.Outfits[SelectedSlotIndex].TorsoId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso);
                dbOutfit.Outfits[SelectedSlotIndex].BeltId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt);
                dbOutfit.Outfits[SelectedSlotIndex].PelvisId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis);
                dbOutfit.Outfits[SelectedSlotIndex].RobeId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);

                dbOutfit.Outfits[SelectedSlotIndex].LeftBicepId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep);
                dbOutfit.Outfits[SelectedSlotIndex].LeftFootId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot);
                dbOutfit.Outfits[SelectedSlotIndex].LeftForearmId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm);
                dbOutfit.Outfits[SelectedSlotIndex].LeftHandId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand);
                dbOutfit.Outfits[SelectedSlotIndex].LeftShinId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin);
                dbOutfit.Outfits[SelectedSlotIndex].LeftShoulderId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder);
                dbOutfit.Outfits[SelectedSlotIndex].LeftThighId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh);

                dbOutfit.Outfits[SelectedSlotIndex].RightBicepId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep);
                dbOutfit.Outfits[SelectedSlotIndex].RightFootId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot);
                dbOutfit.Outfits[SelectedSlotIndex].RightForearmId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm);
                dbOutfit.Outfits[SelectedSlotIndex].RightHandId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand);
                dbOutfit.Outfits[SelectedSlotIndex].RightShinId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin);
                dbOutfit.Outfits[SelectedSlotIndex].RightShoulderId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder);
                dbOutfit.Outfits[SelectedSlotIndex].RightThighId = GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh);

                DB.Set(playerId, dbOutfit);

                IsLoadEnabled = true;
                UpdateDetails(dbOutfit.Outfits[SelectedSlotIndex]);
            }

            // Nothing is saved to this slot yet.
            if (string.IsNullOrWhiteSpace(dbOutfit.Outfits[SelectedSlotIndex].Data))
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

        private void LoadOutfit()
        {
            var armor = GetItemInSlot(InventorySlot.Chest, Player);
            var playerId = GetObjectUUID(Player);
            var dbOutfit = DB.Get<PlayerOutfit>(playerId) ?? new PlayerOutfit();
            var outfit = dbOutfit.Outfits[SelectedSlotIndex];

            // Get the temporary storage placeable and deserialize the outfit into it.
            var tempStorage = GetObjectByTag("OUTFIT_BARREL");
            var deserialized = ObjectPlugin.Deserialize(outfit.Data);
            var copy = CopyItem(armor, tempStorage, true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftBicep, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftBicep), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Belt, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Belt), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftFoot, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftFoot), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftForearm, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftForearm), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftHand, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftHand), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShin, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShin), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShoulder, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShoulder), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftThigh, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftThigh), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Neck, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Neck), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Pelvis, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Pelvis), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightBicep, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightBicep), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightFoot, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightFoot), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightForearm, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightForearm), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightHand, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightHand), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Robe, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Robe), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShin, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShin), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShoulder, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShoulder), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightThigh, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightThigh), true);

            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), true);
            copy = CopyItemAndModify(copy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Torso, (int)GetItemAppearance(deserialized, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Torso), true);

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
            var dbOutfit = DB.Get<PlayerOutfit>(playerId) ?? new PlayerOutfit();

            if (dbOutfit.Outfits.Count >= MaxOutfits)
            {
                FloatingTextStringOnCreature($"You may only create {MaxOutfits} outfits.", Player, false);
                return;
            }

            var newOutfit = new PlayerOutfitDetail
            {
                Name = $"Outfit #{dbOutfit.Outfits.Count+1}"
            };

            dbOutfit.Outfits.Add(newOutfit);

            DB.Set(playerId, dbOutfit);
            SlotNames.Add(newOutfit.Name);
            SlotToggles.Add(false);
        };

        public Action OnClickDelete() => () =>
        {
            ShowModal("Are you sure you want to delete this stored outfit?", () =>
            {
                var playerId = GetObjectUUID(Player);
                var dbOutfit = DB.Get<PlayerOutfit>(playerId) ?? new PlayerOutfit();

                dbOutfit.Outfits.RemoveAt(SelectedSlotIndex);
                DB.Set(playerId, dbOutfit);

                SlotNames.RemoveAt(SelectedSlotIndex);
                SlotToggles.RemoveAt(SelectedSlotIndex);

                SelectedSlotIndex = -1;
                IsSlotLoaded = false;
            });
        };
    }
}
