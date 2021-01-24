using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class OutfitDialog: DialogBase
    {
        private const int MaxSlots = 20;

        private class Model
        {
            public int ConfirmingDeleteSlot { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string SavePageId = "SAVE_PAGE";
        private const string LoadPageId = "LOAD_PAGE";
        private const string DeletePageId = "DELETE_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddBackAction((previous, next) =>
                {
                    var model = GetDataModel<Model>();
                    model.ConfirmingDeleteSlot = 0;
                })
                .AddPage(MainPageId, MainPageInit)
                .AddPage(SavePageId, SavePageInit)
                .AddPage(LoadPageId, LoadPageInit)
                .AddPage(DeletePageId, DeletePageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = "Please select an option.";

            page.AddResponse("Save Appearance", () =>
            {
                ChangePage(SavePageId);
            });

            page.AddResponse("Load Appearance", () =>
            {
                ChangePage(LoadPageId);
            });

            page.AddResponse("Delete Appearance", () =>
            {
                ChangePage(DeletePageId);
            });
        }

        private void SavePageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            page.Header = "Please select a slot to save the outfit in.\n\n" +
                          "Red slots are unused. Green slots contain stored appearances. Selecting a green slot will overwrite whatever is in that slot.";

            for (var x = 1; x <= MaxSlots; x++)
            {
                var slot = x; // Copy due to variable changing in outer scope
                var text = dbPlayer.SavedOutfits.ContainsKey(slot) 
                    ? ColorToken.Green($"Slot #{x}") 
                    : ColorToken.Red($"Slot #{x}");

                page.AddResponse(text, () =>
                {
                    var armor = GetItemInSlot(InventorySlot.Chest, player);

                    if (!GetIsObjectValid(armor))
                    {
                        FloatingTextStringOnCreature(ColorToken.Red("You do not have any armor equipped. Appearance failed to save."), player, false);
                        return;
                    }

                    var serialized = Object.Serialize(armor);
                    dbPlayer.SavedOutfits[slot] = serialized;

                    DB.Set(playerId, dbPlayer);
                });
            }
        }

        private void LoadPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            page.Header = "Please select an outfit to load. The appearance of your currently equipped armor will be overwritten.";

            for(var x = 1; x <= MaxSlots; x++)
            {
                if (!dbPlayer.SavedOutfits.ContainsKey(x)) continue;

                var outfit = dbPlayer.SavedOutfits[x];
                page.AddResponse($"Slot #{x}", () =>
                {
                    var armor = GetItemInSlot(InventorySlot.Chest, player);
                    if (!GetIsObjectValid(armor))
                    {
                        FloatingTextStringOnCreature(ColorToken.Red("You do not have any armor equipped. Appearance failed to load."), player, false);
                        return;
                    }

                    // Get the temporary storage placeable and deserialize the outfit into it.
                    var tempStorage = GetObjectByTag("OUTFIT_BARREL");
                    var deserialized = Object.Deserialize(outfit);
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

                    var final = CopyItem(copy, player, true);
                    DestroyObject(armor);
                    DestroyObject(copy);
                    DestroyObject(deserialized);

                    AssignCommand(player, () =>
                    {
                        ActionEquipItem(final, InventorySlot.Chest);
                    });
                });
            }
        }

        private void DeletePageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();
            var model = GetDataModel<Model>();

            page.Header = "Please select an outfit appearance to delete.";

            for (int x = 1; x <= MaxSlots; x++)
            {
                // Record exists in DB.
                if (dbPlayer.SavedOutfits.ContainsKey(x))
                {
                    var slot = x; // Copy due to variable changing in outer scope

                    // Are we confirming the delete?
                    if (model.ConfirmingDeleteSlot == x)
                    {
                        page.AddResponse(ColorToken.Red($"CONFIRM DELETE - Slot #{x}"), () =>
                        {
                            model.ConfirmingDeleteSlot = 0;
                            dbPlayer.SavedOutfits.Remove(slot);

                            DB.Set(playerId, dbPlayer);
                        });
                    }
                    else
                    {
                        page.AddResponse($"Slot #{x}", () =>
                        {
                            model.ConfirmingDeleteSlot = slot;
                        });
                    }
                }
            }
        }
    }
}
