using System;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using BaseItem = SWLOR.Game.Server.Core.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class ModifyItemAppearanceDialog: DialogBase
    {
        private class Model
        {
            public uint TargetItem { get; set; }
            public ItemAppearanceType ItemTypeID { get; set; }
            public int Index { get; set; }
            public InventorySlot InventorySlotID { get; set; }
            public int[] PartSelection { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string WeaponPartPageId = "WEAPON_PART_PAGE";
        private const string ArmorPartPageId = "ARMOR_PART_PAGE";
        private const string PartSelectionPageId = "PART_SELECTION_PAGE";


        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(() => SetCommandable(false, GetPC()))
                .AddBackAction((beforeMovePage, afterMovePage) => SetCommandable(true, GetPC()))
                .AddEndAction(() => SetCommandable(true, GetPC()))
                .AddPage(MainPageId, MainPageInit)
                .AddPage(WeaponPartPageId, WeaponPartPageInit)
                .AddPage(ArmorPartPageId, ArmorPartPageInit)
                .AddPage(PartSelectionPageId, PartSelectionPageInit);


            return builder.Build();
        }

        private bool IsItemValid(uint item)
        {
            var type = GetBaseItemType(item);
            if (type == BaseItem.Invalid) return false;

            return GetIsObjectValid(item) &&
                   !GetPlotFlag(item) &&
                   !GetItemCursedFlag(item) &&
                   type != BaseItem.Lightsaber &&
                   type != BaseItem.Saberstaff &&
                   !GetLocalBool(item, "LIGHTSABER");
        }

        private bool IsWeaponValid(uint item)
        {
            var baseItemType = GetBaseItemType(item);
            if (baseItemType == BaseItem.Invalid) return false;

            var modelTypeString = Get2DAString("baseitems", "ModelType", (int) baseItemType);
            if (!int.TryParse(modelTypeString, out var mainModelTypeId))
            {
                return false;
            }

            var modelType = (ItemAppearanceType)mainModelTypeId;
            return IsItemValid(item) && modelType == ItemAppearanceType.WeaponModel;
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            page.Header = "What would you like to modify?";

            page.AddResponse("Save/Load Outfits", () =>
            {
                SetCommandable(true, player);
                SwitchConversation(nameof(OutfitDialog));
            });

            // Main Hand Appearance
            if (IsWeaponValid(GetItemInSlot(InventorySlot.RightHand, player)))
            {
                page.AddResponse("Main Weapon", () =>
                {
                    if (!IsWeaponValid(GetItemInSlot(InventorySlot.RightHand, player)))
                    {
                        FloatingTextStringOnCreature("Invalid weapon equipped in main hand.", player, false);
                    }
                    else
                    {
                        model.TargetItem = GetItemInSlot(InventorySlot.RightHand, player);
                        model.InventorySlotID = InventorySlot.RightHand;
                        ChangePage(WeaponPartPageId);
                    }
                });
            }

            // Off-Hand Appearance
            if (IsWeaponValid(GetItemInSlot(InventorySlot.LeftHand, player)))
            {
                page.AddResponse("Off-Hand Weapon", () =>
                {
                    if (!IsWeaponValid(GetItemInSlot(InventorySlot.LeftHand, player)))
                    {
                        FloatingTextStringOnCreature("Invalid weapon equipped in off hand.", player, false);
                    }
                    else
                    {
                        model.TargetItem = GetItemInSlot(InventorySlot.LeftHand, player);
                        model.InventorySlotID = InventorySlot.LeftHand;
                        ChangePage(WeaponPartPageId);
                    }
                });
            }

            // Armor Appearance
            if (IsItemValid(GetItemInSlot(InventorySlot.Chest, player)))
            {
                page.AddResponse("Armor", () =>
                {
                    if (!IsItemValid(GetItemInSlot(InventorySlot.Chest, player)))
                    {
                        FloatingTextStringOnCreature("Invalid armor equipped.", player, false);
                    }
                    else
                    {
                        model.TargetItem = GetItemInSlot(InventorySlot.Chest, player);
                        model.InventorySlotID = InventorySlot.Chest;
                        ChangePage(ArmorPartPageId);
                    }
                });
            }

            // Helmet Appearance
            if (IsItemValid(GetItemInSlot(InventorySlot.Head, player)))
            {
                page.AddResponse("Helmet", () =>
                {
                    if (!IsItemValid(GetItemInSlot(InventorySlot.Head, player)))
                    {
                        FloatingTextStringOnCreature("Invalid helmet equipped.", player, false);
                    }
                    else
                    {
                        model.TargetItem = GetItemInSlot(InventorySlot.Head, player);
                        model.InventorySlotID = InventorySlot.Head;
                        model.ItemTypeID = ItemAppearanceType.SimpleModel;

                        /* parts excluded for helmets:
                            *  13          = sith mask
                            *  22          = alien head
                            *  39 - 44     = creature heads
                            *  96 - 98     = alien heads
                            *  143 - 152   = creature or alien head
                            *  209         = sith mask
                            *  211         = sith mask
                            */
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 210, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255 };

                        ChangePage(PartSelectionPageId);
                    }
                });
            }
        }

        private void WeaponPartPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            model.ItemTypeID = ItemAppearanceType.WeaponModel;

            page.Header = "Which weapon part would you like to modify?";

            page.AddResponse("Top", () =>
            {
                model.Index = 2;

                switch (GetBaseItemType(model.TargetItem))
                {
                    case BaseItem.GreatAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 7, 11, 12, 13, 14, 15, 16, 17, 19, 21, 24, 25 };
                        break;
                    case BaseItem.BattleAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 17, 19, 20, 21, 23, 24, 25 };
                        break;
                    // parts 20 (lightfoil blade) and 24 (wind fire wheel?) excluded
                    case BaseItem.BastardSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 11, 14, 15, 16, 17, 18, 21, 22, 25 };
                        break;
                    // parts 19 (lightfoil blade) excluded
                    case BaseItem.Dagger:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 16, 20, 21, 22 };
                        break;
                    // parts 16 (lightfoil blade) excluded
                    case BaseItem.GreatSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 19, 20, 21, 22, 23, 24 };
                        break;
                    // parts 21 (lightfoil blade) and 24 (cosmic blade) excluded
                    case BaseItem.Longsword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 17, 18, 19, 20, 22, 23, 25 };
                        break;
                    // parts 14 (lightfoil blade) excluded
                    case BaseItem.Rapier:
                        model.PartSelection = new[] { 1, 2, 3, 4, 11, 12, 13 };
                        break;
                    // parts 23 (lightfoil blade) excluded
                    case BaseItem.Katana:
                        model.PartSelection = new[] { 2, 3, 4 };
                        break;
                    // parts 20 (lightfoil blade) and 24 (cosmic blade) excluded
                    case BaseItem.ShortSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 25 };
                        break;
                    case BaseItem.Club:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 9, 11, 12, 13, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.LightMace:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 11, 12, 13, 14, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.MorningStar:
                        model.PartSelection = new[] { 1, 2, 3, 4, 6 };
                        break;
                    case BaseItem.QuarterStaff:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 11, 23, 25 };
                        break;
                    case BaseItem.DoubleAxe:
                        model.PartSelection = new[] { 1, 2, 3, 6, 8, 11, 13, 14 };
                        break;
                    case BaseItem.TwoBladedSword:
                        model.PartSelection = new[] { 1, 2, 3 };
                        break;
                    case BaseItem.Kukri:
                        model.PartSelection = new[] { 1 };
                        break;
                    // parts 9 (electric effect) excluded
                    case BaseItem.Halberd:
                        model.PartSelection = new[] { 1, 2, 3, 4, 7, 10, 15, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.ShortSpear:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 14, 18, 24, 25 };
                        break;
                    // Shortbow = Blaster Pistol
                    case BaseItem.Pistol:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 18, 19, 21, 22 };
                        break;
                    // Shortbow = Dual Blaster Pistol
                    case BaseItem.Sling:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 18, 19, 21, 22 };
                        break;
                    // Light Crossbow = Blaster Rifle
                    case BaseItem.Rifle:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                        break;
                    case BaseItem.ThrowingAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4 };
                        break;
                }

                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Middle", () =>
            {
                model.Index = 1;

                switch (GetBaseItemType(model.TargetItem))
                {
                    case BaseItem.GreatAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 7, 11, 12, 13, 25 };
                        break;
                    case BaseItem.BattleAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 9, 10, 11, 12, 15, 16, 18, 24 };
                        break;
                    case BaseItem.BastardSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Dagger:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 16, 20, 21, 22 };
                        break;
                    case BaseItem.GreatSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Longsword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Rapier:
                        model.PartSelection = new[] { 1, 2, 3, 4, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
                        break;
                    case BaseItem.Katana:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 8, 11, 12, 13, 24, 25 };
                        break;
                    case BaseItem.ShortSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Club:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 21, 22, 23, 25 };
                        break;
                    case BaseItem.LightMace:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 25 };
                        break;
                    case BaseItem.MorningStar:
                        model.PartSelection = new[] { 1, 2, 3, 4 };
                        break;
                    case BaseItem.QuarterStaff:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 11, 23, 25 };
                        break;
                    case BaseItem.DoubleAxe:
                        model.PartSelection = new[] { 1, 2, 3, 6, 8, 11, 12 };
                        break;
                    case BaseItem.TwoBladedSword:
                        model.PartSelection = new[] { 1, 2, 3 };
                        break;
                    case BaseItem.Kukri:
                        model.PartSelection = new[] { 1 };
                        break;
                    case BaseItem.Halberd:
                        model.PartSelection = new[] { 1, 2, 3, 4, 7, 9, 10, 15, 21, 22, 23 };
                        break;
                    case BaseItem.ShortSpear:
                        model.PartSelection = new[] { 1, 2, 3, 4, 6, 7, 11, 14, 18, 24, 25 };
                        break;
                    // Shortbow = Blaster Pistol
                    case BaseItem.Pistol:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    // Light Crossbow = Blaster Rifle
                    case BaseItem.Rifle:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24, 25 };
                        break;
                    case BaseItem.ThrowingAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4 };
                        break;
                }

                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Bottom", () =>
            {
                model.Index = 0;

                switch (GetBaseItemType(model.TargetItem))
                {
                    case BaseItem.GreatAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 7, 11, 12, 13, 25 };
                        break;
                    case BaseItem.BattleAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 9, 10, 11, 12, 15, 16, 18, 24 };
                        break;
                    case BaseItem.BastardSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Dagger:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 16, 20, 21, 22 };
                        break;
                    case BaseItem.GreatSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Longsword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Rapier:
                        model.PartSelection = new[] { 1, 2, 3, 4, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
                        break;
                    case BaseItem.Katana:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 8, 11, 12, 13, 24, 25 };
                        break;
                    case BaseItem.ShortSword:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    case BaseItem.Club:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 21, 22, 23, 25 };
                        break;
                    case BaseItem.LightMace:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 25 };
                        break;
                    case BaseItem.MorningStar:
                        model.PartSelection = new[] { 1, 2, 3, 4 };
                        break;
                    case BaseItem.QuarterStaff:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 11, 23, 25 };
                        break;
                    case BaseItem.DoubleAxe:
                        model.PartSelection = new[] { 1, 2, 3, 6, 8, 11, 12 };
                        break;
                    case BaseItem.TwoBladedSword:
                        model.PartSelection = new[] { 1, 2, 3 };
                        break;
                    case BaseItem.Kukri:
                        model.PartSelection = new[] { 1 };
                        break;
                    case BaseItem.Halberd:
                        model.PartSelection = new[] { 1, 2, 3, 4, 7, 9, 10, 15, 21, 22, 23 };
                        break;
                    case BaseItem.ShortSpear:
                        model.PartSelection = new[] { 1, 2, 3, 4, 6, 7, 11, 14, 18, 24, 25 };
                        break;
                    // Shortbow = Blaster Pistol
                    case BaseItem.Pistol:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                        break;
                    // Light Crossbow = Blaster Rifle
                    case BaseItem.Rifle:
                        model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24, 25 };
                        break;
                    case BaseItem.ThrowingAxe:
                        model.PartSelection = new[] { 1, 2, 3, 4 };
                        break;
                }

                ChangePage(PartSelectionPageId);
            });
        }

        private void ArmorPartPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            model.ItemTypeID = ItemAppearanceType.ArmorModel;

            page.AddResponse("Neck", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_Neck;
                model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10, 26, 30, 31, 32, 50, 63, 95, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 121, 122, 123, 124, 125, 126, 127, 128, 129, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 198, 199, 201, 203, 250, 254, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Torso", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_Torso;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 72, 75, 76, 77, 78, 79, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 163, 166, 167, 168, 171, 172, 173, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 193, 196, 197, 198, 199, 200, 201, 202, 203, 210, 212, 219, 220, 221, 222, 247, 248, 249, 250, 253, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Belt", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_Belt;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 23, 24, 25, 26, 30, 31, 32, 63, 70, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 188, 189, 190, 191, 192, 198, 218, 219, 220, 221 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Pelvis", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_Pelvis;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42, 50, 63, 75, 101, 102, 103, 104, 105, 106, 108, 109, 110, 111, 117, 122, 123, 140, 141, 142, 143, 144, 146, 151, 153, 154, 155, 156, 157, 158, 161, 163, 164, 165, 166, 186, 198, 199, 201, 202, 203, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 250, 253, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Robe", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_Robe;
                model.PartSelection = new[] { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 121, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 169, 170, 173, 174, 182, 183, 184, 185, 186, 187, 190, 191, 192, 193, 194, 195, 196, 197, 198, 200, 201, 202, 203, 204, 205, 206, 221, 222, 223, 226, 227, 230, 234, 235, 236, 247, 248, 249, 250, 252, 253, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Right Thigh", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_RightThigh;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 23, 24, 30, 31, 50, 51, 52, 53, 54, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 118, 121, 122, 123, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 186, 198, 199, 201, 202, 203, 220, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Right Shin", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_RightShin;
                model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 26, 27, 30, 50, 51, 54, 55, 56, 57, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 121, 128, 129, 130, 131, 132, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 160, 161, 162, 164, 165, 166, 186, 198, 199, 201, 202, 203, 219, 220, 221, 222, 223, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Right Foot", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_RightFoot;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 18, 30, 50, 51, 52, 63, 75, 80, 81, 82, 83, 101, 102, 103, 104, 105, 106, 107, 110, 116, 117, 118, 121, 122, 123, 124, 145, 146, 151, 152, 154, 155, 156, 157, 158, 160, 186, 190, 198, 199, 200, 201, 202, 203, 205, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Left Thigh", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_LeftThigh;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 23, 24, 30, 31, 50, 51, 52, 53, 54, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 118, 121, 122, 123, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 186, 198, 199, 201, 202, 203, 220, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Left Shin", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_LeftShin;
                model.PartSelection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 26, 27, 30, 50, 51, 54, 55, 56, 57, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 121, 128, 129, 130, 131, 132, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 160, 161, 162, 164, 165, 166, 186, 198, 199, 201, 202, 203, 219, 220, 221, 222, 223, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Left Foot", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_LeftFoot;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 18, 30, 50, 51, 52, 63, 75, 80, 81, 82, 83, 101, 102, 103, 104, 105, 106, 107, 110, 116, 117, 118, 121, 122, 123, 124, 145, 146, 151, 152, 154, 155, 156, 157, 158, 160, 186, 190, 198, 199, 200, 201, 202, 203, 205, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Right Shoulder", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_RightShoulder;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 54, 55, 56, 57, 58, 100, 101, 102, 122, 123, 140, 141, 185, 186, 197, 198, 199, 219, 220, 221, 222, 249, 250 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Right Bicep", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_RightBicep;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 20, 21, 22, 30, 31, 32, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 68, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 182, 183, 186, 198, 199, 201, 202, 203, 246, 247, 248, 249, 250, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Right Forearm", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_RightForearm;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 28, 30, 40, 41, 54, 55, 56, 57, 58, 63, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 140, 141, 142, 143, 144, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 167, 168, 169, 186, 198, 199, 200, 201, 203, 215, 219, 220, 221, 244, 245, 246, 247, 250, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Right Glove", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_RightHand;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 63, 75, 100, 101, 109, 110, 111, 113, 121, 122, 150, 151, 152, 153, 154, 155, 186, 192, 193, 194, 195, 196, 198, 201, 203, 215, 245, 246, 250, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Left Shoulder", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_LeftShoulder;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 54, 55, 56, 57, 58, 100, 101, 102, 122, 123, 140, 141, 185, 186, 197, 198, 199, 219, 220, 221, 222, 249, 250 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Left Bicep", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_LeftBicep;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 20, 21, 22, 30, 31, 32, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 68, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 182, 183, 186, 198, 199, 201, 202, 203, 246, 247, 248, 249, 250, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Left Forearm", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_LeftForearm;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 28, 30, 40, 41, 54, 55, 56, 57, 58, 63, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 140, 141, 142, 143, 144, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 167, 168, 169, 186, 198, 199, 200, 201, 203, 215, 219, 220, 221, 244, 245, 246, 247, 250, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });

            page.AddResponse("Left Glove", () =>
            {
                model.Index = (int)ItemAppearance.ArmorModel_LeftHand;
                model.PartSelection = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 63, 75, 100, 101, 109, 110, 111, 113, 121, 122, 150, 151, 152, 153, 154, 155, 186, 192, 193, 194, 195, 196, 198, 201, 203, 215, 245, 246, 250, 257, 258, 259 };
                ChangePage(PartSelectionPageId);
            });
        }

        private void PartSelectionPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            foreach (var part in model.PartSelection)
            {
                page.AddResponse($"Part #{part}", () =>
                {
                    var copy = CopyItemAndModify(model.TargetItem, model.ItemTypeID, model.Index, part, true);
                    DestroyObject(model.TargetItem);
                    model.TargetItem = copy;

                    AssignCommand(player, () =>
                    {
                        SetCommandable(true, player);
                        ActionEquipItem(copy, model.InventorySlotID);
                        SetCommandable(false, player);
                    });
                });
            }
        }
    }
}
