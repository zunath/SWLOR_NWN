using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class ModifyItemAppearance: ConversationBase
    {
        private class Model
        {
            public NWItem TargetItem { get; set; }
            public ItemAppearanceType ItemTypeID { get; set; }
            public int Index { get; set; }
            public InventorySlot InventorySlotID { get; set; }
        }


        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "What would you like to modify?",
                "Save/Load Outfits",
                "Main Weapon",
                "Off-Hand Weapon",
                "Armor",
                "Helmet");

            DialogPage weaponPartPage = new DialogPage(
                "Which weapon part would you like to modify?",
                "Top",
                "Middle",
                "Bottom"); 

            DialogPage armorPartPage = new DialogPage(
                "Which armor part would you like to modify?",
                "Neck",
                "Torso",
                "Belt",
                "Pelvis",
                "Robe",
                "Right Thigh",
                "Right Shin",
                "Right Foot",
                "Left Thigh",
                "Left Shin",
                "Left Foot",
                "Right Shoulder",
                "Right Bicep",
                "Right Forearm",
                "Right Glove",
                "Left Shoulder",
                "Left Bicep",
                "Left Forearm",
                "Left Glove",
                "Helmet");

            DialogPage helmetPartPage = new DialogPage(
                "Would you like to modify your helmet?",
                "Helmet");

            DialogPage partPage = new DialogPage(
                "Please select a new model.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("WeaponPartPage", weaponPartPage);
            dialog.AddPage("ArmorPartPage", armorPartPage);
            dialog.AddPage("HelmetPartPage", helmetPartPage);
            dialog.AddPage("PartPage", partPage);
            return dialog;
        }

        public override void Initialize()
        {
            SetCommandable(false, GetPC());
            LoadMainPage();
        }
        
        private bool IsArmorValid()
        {
            NWPlayer player = GetPC();
            NWItem armor = player.Chest;

            bool canModifyArmor = armor.IsValid && !armor.IsPlot && !armor.IsCursed;
            return canModifyArmor;
        }

        private bool IsHelmetValid()
        {
            NWPlayer player = GetPC();
            NWItem helmet = player.Head;

            bool canModifyHelmet = helmet.IsValid && !helmet.IsPlot && !helmet.IsCursed;
            return canModifyHelmet;
        }

        private bool IsMainValid()
        {
            NWPlayer player = GetPC();
            NWItem main = player.RightHand;
            Player pc = DataService.Player.GetByID(player.GlobalID);
            
            bool canModifyMain = main.IsValid && 
                                 !main.IsPlot && 
                                 !main.IsCursed &&
                                 // https://github.com/zunath/SWLOR_NWN/issues/942#issue-467176236
                                 main.CustomItemType != CustomItemType.Lightsaber && 
                                 main.CustomItemType != CustomItemType.Saberstaff &&
                                 main.GetLocalBool("LIGHTSABER") == false &&
                                 !(pc.ModeDualPistol && main.CustomItemType == CustomItemType.BlasterPistol);

            if (canModifyMain)
            {
                var mainModelTypeID = (ItemAppearanceType)Convert.ToInt32(Get2DAString("baseitems", "ModelType", (int)main.BaseItemType));
                canModifyMain = mainModelTypeID == ItemAppearanceType.WeaponModel;
            }

            return canModifyMain;
        }

        private bool IsOffHandValid()
        {
            NWPlayer player = GetPC();
            NWItem offHand = player.LeftHand;
            Player pc = DataService.Player.GetByID(player.GlobalID);

            bool canModifyOffHand = offHand.IsValid && !offHand.IsPlot && !offHand.IsCursed &&
                                    // https://github.com/zunath/SWLOR_NWN/issues/942#issue-467176236
                                    offHand.CustomItemType != CustomItemType.Lightsaber && offHand.GetLocalBool("LIGHTSABER") == false &&
                                    !(pc.ModeDualPistol && offHand.CustomItemType == CustomItemType.BlasterPistol);

            if (canModifyOffHand)
            {
                var offHandModelTypeID = (ItemAppearanceType)Convert.ToInt32(Get2DAString("baseitems", "ModelType", (int)offHand.BaseItemType));
                canModifyOffHand = offHandModelTypeID == ItemAppearanceType.WeaponModel;
            }

            return canModifyOffHand;
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "WeaponPartPage":
                    WeaponPartResponses(responseID);
                    break;
                case "ArmorPartPage":
                    ArmorPartResponses(responseID);
                    break;
                case "HelmetPartPage":
                    HelmetPartResponses(responseID);
                    break;
                case "PartPage":
                    PartResponses(responseID);
                    break;
            }

        }

        private void LoadMainPage()
        {
            // Currently disabling weapons as we need to do some work to identify valid models.
            // Only armor will be working for now.
            SetResponseVisible("MainPage", 2, IsMainValid());
            SetResponseVisible("MainPage", 3, IsOffHandValid());
            SetResponseVisible("MainPage", 4, IsArmorValid());
            SetResponseVisible("MainPage", 5, IsHelmetValid());
        }

        private void MainResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            var player = GetPC();

            switch (responseID)
            {
                case 1: // Save/Load Outfits
                    SetCommandable(true, player);
                    SwitchConversation("Outfit");
                    break;
                case 2: // Main Hand

                    if (!IsMainValid())
                    {
                        player.FloatingText("Invalid weapon equipped in main hand.");
                        LoadMainPage();
                    }
                    else
                    {
                        model.TargetItem = player.RightHand;
                        model.InventorySlotID = InventorySlot.RightHand;
                        ChangePage("WeaponPartPage");
                    }
                    
                    break;
                case 3: // Off Hand

                    if (!IsOffHandValid())
                    {
                        player.FloatingText("Invalid weapon equipped in off hand.");
                        LoadMainPage();
                    }
                    else
                    {
                        model.TargetItem = player.LeftHand;
                        model.InventorySlotID = InventorySlot.LeftHand;
                        ChangePage("WeaponPartPage");
                    }

                    break;
                case 4: // Armor
                    if (!IsArmorValid())
                    {
                        player.FloatingText("Invalid armor equipped.");
                        LoadMainPage();
                    }
                    else
                    {
                        model.TargetItem = player.Chest;
                        model.InventorySlotID = InventorySlot.Chest;
                        ChangePage("ArmorPartPage");
                    }

                    break;
                case 5: // Helmet
                    if (!IsHelmetValid())
                    {
                        player.FloatingText("Invalid helmet equipped.");
                        LoadMainPage();
                    }
                    else
                    {
                        model.TargetItem = player.Head;
                        model.InventorySlotID = InventorySlot.Head;
                        ChangePage("HelmetPartPage");
                    }

                    break;
            }
        }

        private void WeaponPartResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            model.ItemTypeID = ItemAppearanceType.WeaponModel;
            int[] parts = { 0 };

            switch (responseID)
            {
                case 1: // Top
                    model.Index = 2;

                    // WEAPON CRAFTING RESTRICTIONS GO HERE    
                    switch (model.TargetItem.BaseItemType)
                    {
                        case BaseItem.GreatAxe:
                            parts = new[] { 1, 2, 3, 4, 5, 7, 11, 12, 13, 14, 15, 16, 17, 19, 21, 24, 25 };
                            break;
                        case BaseItem.BattleAxe:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 17, 19, 20, 21, 23, 24, 25 };
                            break;
                        // parts 20 (lightfoil blade) and 24 (wind fire wheel?) excluded
                        case BaseItem.BastardSword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 11, 14, 15, 16, 17, 18, 21, 22, 25 };
                            break;
                        // parts 19 (lightfoil blade) excluded
                        case BaseItem.Dagger:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 16, 20, 21, 22};
                            break;
                        // parts 16 (lightfoil blade) excluded
                        case BaseItem.GreatSword:
                            parts = new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 19, 20, 21, 22, 23, 24 };
                            break;
                        // parts 21 (lightfoil blade) and 24 (cosmic blade) excluded
                        case BaseItem.Longsword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 17, 18, 19, 20, 22, 23, 25};
                            break;
                        // parts 14 (lightfoil blade) excluded
                        case BaseItem.Rapier:
                            parts = new[] { 1, 2, 3, 4, 11, 12, 13 };
                            break;
                        // parts 23 (lightfoil blade) excluded
                        case BaseItem.Katana:
                            parts = new[] { 2, 3, 4 };
                            break;
                        // parts 20 (lightfoil blade) and 24 (cosmic blade) excluded
                        case BaseItem.ShortSword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 25 };
                            break;
                        case BaseItem.Club:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 9, 11, 12, 13, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.LightMace:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 11, 12, 13, 14, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.MorningStar:
                            parts = new[] { 1, 2, 3, 4, 6 };
                            break;
                        case BaseItem.QuarterStaff:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 11, 23, 25 };
                            break;
                        case BaseItem.DoubleAxe:
                            parts = new[] { 1, 2, 3, 6, 8, 11, 13, 14 };
                            break;
                        case BaseItem.TwoBladedSword:
                            parts = new[] { 1, 2, 3 };
                            break;
                        case BaseItem.Kukri:
                            parts = new[] { 1 };
                            break;
                        // parts 9 (electric effect) excluded
                        case BaseItem.Halberd:
                            parts = new[] { 1, 2, 3, 4, 7, 10, 15, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.ShortSpear:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 14, 18, 24, 25 };
                            break;
                        // Shortbow = Blaster Pistol
                        case BaseItem.ShortBow:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 18, 19, 21, 22 };
                            break;
                        // Shortbow = Dual Blaster Pistol
                        case BaseItem.Sling:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 18, 19, 21, 22 };
                            break;
                        // Light Crossbow = Blaster Rifle
                        case BaseItem.LightCrossbow:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                            break;
                        case BaseItem.ThrowingAxe:
                            parts = new[] { 1, 2, 3, 4 };
                            break;
                    }
                    break;
                case 2: // Middle
                    model.Index = 1;

                    // WEAPON CRAFTING RESTRICTIONS GO HERE
                    switch (model.TargetItem.BaseItemType)
                    {
                        case BaseItem.GreatAxe:
                            parts = new[] { 1, 2, 3, 4, 5, 7, 11, 12, 13, 25 };
                            break;
                        case BaseItem.BattleAxe:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 9, 10, 11, 12, 15, 16, 18, 24 };
                            break;
                        case BaseItem.BastardSword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.Dagger:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 16, 20, 21, 22 };
                            break;
                        case BaseItem.GreatSword:
                            parts = new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.Longsword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 22, 23, 24, 25 };
                            break;
                        case BaseItem.Rapier:
                            parts = new[] { 1, 2, 3, 4, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
                            break;
                        case BaseItem.Katana:
                            parts = new[] { 1, 2, 3, 4, 5, 8, 11, 12, 13, 24, 25 };
                            break;
                        case BaseItem.ShortSword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.Club:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 21, 22, 23, 25 };
                            break;
                        case BaseItem.LightMace:
                            parts = new[] { 1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 25 };
                            break;
                        case BaseItem.MorningStar:
                            parts = new[] { 1, 2, 3, 4 };
                            break;
                        case BaseItem.QuarterStaff:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 11, 23, 25 };
                            break;
                        case BaseItem.DoubleAxe:
                            parts = new[] { 1, 2, 3, 6, 8, 11, 12 };
                            break;
                        case BaseItem.TwoBladedSword:
                            parts = new[] { 1, 2, 3 };
                            break;
                        case BaseItem.Kukri:
                            parts = new[] { 1 };
                            break;
                        case BaseItem.Halberd:
                            parts = new[] { 1, 2, 3, 4, 7, 9, 10, 15, 21, 22, 23 };
                            break;
                        case BaseItem.ShortSpear:
                            parts = new[] { 1, 2, 3, 4, 6, 7, 11, 14, 18, 24, 25 };
                            break;
                        // Shortbow = Blaster Pistol
                        case BaseItem.ShortBow:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                            break;
                        // Light Crossbow = Blaster Rifle
                        case BaseItem.LightCrossbow:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24, 25 };
                            break;
                        case BaseItem.ThrowingAxe:
                            parts = new[] { 1, 2, 3, 4 };
                            break;
                    }
                    break;
                case 3: // Bottom
                    model.Index = 0;

                    // WEAPON CRAFTING RESTRICTIONS GO HERE
                    switch (model.TargetItem.BaseItemType)
                    {
                        case BaseItem.GreatAxe:
                            parts = new[] { 1, 2, 3, 4, 5, 7, 11, 12, 13, 14, 15, 16, 17, 18, 24, 25 };
                            break;
                        case BaseItem.BattleAxe:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 9, 10, 11, 12, 13, 14, 15, 16, 24 };
                            break;
                        case BaseItem.BastardSword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 24, 25 };
                            break;
                        case BaseItem.Dagger:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 16, 20, 21, 22 };
                            break;
                        case BaseItem.GreatSword:
                            parts = new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 19, 20, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.Longsword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.Rapier:
                            parts = new[] { 1, 2, 3, 4, 11, 12, 13, 14, 15, 16 };
                            break;
                        case BaseItem.Katana:
                            parts = new[] { 1, 2, 3, 4, 11, 12, 24, 25 };
                            break;
                        case BaseItem.ShortSword:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 9, 10, 11, 12, 13, 21, 22, 23, 24, 25 };
                            break;
                        case BaseItem.Club:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 7, 9, 11, 12, 13, 21, 22, 23, 25 };
                            break;
                        case BaseItem.LightMace:
                            parts = new[] { 1, 2, 3, 4, 5, 11, 25 };
                            break;
                        case BaseItem.MorningStar:
                            parts = new[] { 1, 2, 3, 4 };
                            break;
                        case BaseItem.QuarterStaff:
                            parts = new[] { 1, 2, 3, 4, 5, 6, 23 };
                            break;
                        case BaseItem.DoubleAxe:
                            parts = new[] { 1, 2, 3, 6, 8, 11, 12, 13, 14, 15, 16, 17, 18 };
                            break;
                        case BaseItem.TwoBladedSword:
                            parts = new[] { 1, 2, 3 };
                            break;
                        case BaseItem.Kukri:
                            parts = new[] { 1 };
                            break;
                        case BaseItem.Halberd:
                            parts = new[] { 1, 2, 3, 4, 7, 9, 10, 15, 21, 22, 23 };
                            break;
                        case BaseItem.ShortSpear:
                            parts = new[] { 1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 14, 18, 24, 25 };
                            break;
                        // Shortbow = Blaster Pistol
                        case BaseItem.ShortBow:
                            parts = new[] { 1, 2, 3, 4, 10, 11, 12, 13, 14, 15, 16, 18, 19, 21, 23, 24, 25 };
                            break;
                        // Light Crossbow = Blaster Rifle
                        case BaseItem.LightCrossbow:
                            parts = new[] { 1, 2, 3, 4 };
                            break;
                        case BaseItem.ThrowingAxe:
                            parts = new[] { 1, 2, 3, 4 };
                            break;
                    }
                    break;
            }

            ClearPageResponses("PartPage");
            foreach (var part in parts)
            {
                AddResponseToPage("PartPage", "Part #" + part, true, part);
            }

            ChangePage("PartPage");
        }

        private void ArmorPartResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            model.ItemTypeID = ItemAppearanceType.ArmorModel;
            int[] parts = {0};

            switch (responseID)
            {
                case 1: // Neck
                    model.Index = (int)ItemAppearance.ArmorModel_Neck;
                    parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10, 26, 30, 31, 32, 50, 63, 95, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 121, 122, 123, 124, 125, 126, 127, 128, 129, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 198, 199, 201, 203, 250, 254, 257, 258, 259 };
                    break;
                case 2: // Torso
                    model.Index = (int)ItemAppearance.ArmorModel_Torso;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 72, 75, 76, 77, 78, 79, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 163, 166, 167, 168, 171, 172, 173, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 193, 196, 197, 198, 199, 200, 201, 202, 203, 210, 212, 219, 220, 221, 222, 247, 248, 249, 250, 253, 258, 259 };
                    break;
                case 3: // Belt
                    model.Index = (int)ItemAppearance.ArmorModel_Belt;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 23, 24, 25, 26, 30, 31, 32, 63, 70, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 188, 189, 190, 191, 192, 198, 218, 219, 220, 221 };
                    break;
                case 4: // Pelvis
                    model.Index = (int)ItemAppearance.ArmorModel_Pelvis;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42, 50, 63, 75, 101, 102, 103, 104, 105, 106, 108, 109, 110, 111, 117, 122, 123, 140, 141, 142, 143, 144, 146, 151, 153, 154, 155, 156, 157, 158, 161, 163, 164, 165, 166, 186, 198, 199, 201, 202, 203, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 250, 253, 257, 258, 259 };
                    break;
                case 5: // Robe
                    model.Index = (int)ItemAppearance.ArmorModel_Robe;
                    parts = new[] { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 121, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 169, 170, 173, 174, 182, 183, 184, 185, 186, 187, 190, 191, 192, 193, 194, 195, 196, 197, 198, 200, 201, 202, 203, 204, 205, 206, 221, 222, 223, 226, 227, 230, 234, 235, 236, 247, 248, 249, 250, 252, 253, 254, 259 };
                    break;
                case 6: // Right Thigh
                    model.Index = (int)ItemAppearance.ArmorModel_RightThigh;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 23, 24, 30, 31, 50, 51, 52, 53, 54, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 118, 121, 122, 123, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 186, 198, 199, 201, 202, 203, 220, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                    break;
                case 7: // Right Shin
                    model.Index = (int)ItemAppearance.ArmorModel_RightShin;
                    parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 26, 27, 30, 50, 51, 54, 55, 56, 57, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 121, 128, 129, 130, 131, 132, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 160, 161, 162, 164, 165, 166, 186, 198, 199, 201, 202, 203, 219, 220, 221, 222, 223, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258 };
                    break;
                case 8: // Right Foot
                    model.Index = (int)ItemAppearance.ArmorModel_RightFoot;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 18, 30, 50, 51, 52, 63, 75, 80, 81, 82, 83, 101, 102, 103, 104, 105, 106, 107, 110, 116, 117, 118, 121, 122, 123, 124, 145, 146, 151, 152, 154, 155, 156, 157, 158, 160, 186, 190, 198, 199, 200, 201, 202, 203, 205, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                    break;
                case 9: // Left Thigh
                    model.Index = (int)ItemAppearance.ArmorModel_LeftThigh;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 23, 24, 30, 31, 50, 51, 52, 53, 54, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 118, 121, 122, 123, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 186, 198, 199, 201, 202, 203, 220, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                    break;
                case 10: // Left Shin
                    model.Index = (int)ItemAppearance.ArmorModel_LeftShin;
                    parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 26, 27, 30, 50, 51, 54, 55, 56, 57, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 121, 128, 129, 130, 131, 132, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 160, 161, 162, 164, 165, 166, 186, 198, 199, 201, 202, 203, 219, 220, 221, 222, 223, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258 };
                    break;
                case 11: // Left Foot
                    model.Index = (int)ItemAppearance.ArmorModel_LeftFoot;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 18, 30, 50, 51, 52, 63, 75, 80, 81, 82, 83, 101, 102, 103, 104, 105, 106, 107, 110, 116, 117, 118, 121, 122, 123, 124, 145, 146, 151, 152, 154, 155, 156, 157, 158, 160, 186, 190, 198, 199, 200, 201, 202, 203, 205, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259 };
                    break;
                case 12: // Right Shoulder
                    model.Index = (int)ItemAppearance.ArmorModel_RightShoulder;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 54, 55, 56, 57, 58, 100, 101, 102, 122, 123, 140, 141, 185, 186, 197, 198, 199, 219, 220, 221, 222, 249, 250 };
                    break;
                case 13: // Right Bicep
                    model.Index = (int)ItemAppearance.ArmorModel_RightBicep;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 20, 21, 22, 30, 31, 32, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 68, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 182, 183, 186, 198, 199, 201, 202, 203, 246, 247, 248, 249, 250, 257, 258, 259 };
                    break;
                case 14: // Right Forearm
                    model.Index = (int)ItemAppearance.ArmorModel_RightForearm;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 28, 30, 40, 41, 54, 55, 56, 57, 58, 63, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 140, 141, 142, 143, 144, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 167, 168, 169, 186, 198, 199, 200, 201, 203, 215, 219, 220, 221, 244, 245, 246, 247, 250, 257, 258, 259 };
                    break;
                case 15: // Right Glove
                    model.Index = (int)ItemAppearance.ArmorModel_RightHand;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 63, 75, 100, 101, 109, 110, 111, 113, 121, 122, 150, 151, 152, 153, 154, 155, 186, 192, 193, 194, 195, 196, 198, 201, 203, 215, 245, 246, 250, 257, 258, 259 };
                    break;
                case 16: // Left Shoulder
                    model.Index = (int)ItemAppearance.ArmorModel_LeftShoulder;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 54, 55, 56, 57, 58, 100, 101, 102, 122, 123, 140, 141, 185, 186, 197, 198, 199, 219, 220, 221, 222, 249, 250 };
                    break;
                case 17: // Left Bicep
                    model.Index = (int)ItemAppearance.ArmorModel_LeftBicep;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 20, 21, 22, 30, 31, 32, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 68, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 182, 183, 186, 198, 199, 201, 202, 203, 246, 247, 248, 249, 250, 257, 258, 259 };
                    break;
                case 18: // Left Forearm
                    model.Index = (int)ItemAppearance.ArmorModel_LeftForearm;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 28, 30, 40, 41, 54, 55, 56, 57, 58, 63, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 140, 141, 142, 143, 144, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 167, 168, 169, 186, 198, 199, 200, 201, 203, 215, 219, 220, 221, 244, 245, 246, 247, 250, 257, 258, 259 };
                    break;
                case 19: // Left Glove
                    model.Index = (int)ItemAppearance.ArmorModel_LeftHand;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 63, 75, 100, 101, 109, 110, 111, 113, 121, 122, 150, 151, 152, 153, 154, 155, 186, 192, 193, 194, 195, 196, 198, 201, 203, 215, 245, 246, 250, 257, 258, 259 };
                    break;
                case 20: // Helmet
                    if (model.TargetItem.BaseItemType == BaseItem.Helmet)
                    { 
                        model.Index = 0;
                        /* parts excluded for helmets:
                         * 13          = sith mask
                         * 22          = alien head
                         * 23 - 26     = alien heads
                         * 27          = sith mask
                         * 39 - 44     = creature heads
                         * 80 - 82     = alien heads
                         * 84 - 94     = invalid
                         * 96 - 98     = alien heads
                         * 99 - 100    = invalid
                         * 125 - 142   = invalid
                         * 143 - 152   = creature or alien head
                         * 155 - 165   = invalid
                         * 168 - 173   = invalid
                         * 182         = invalid
                         * 189 - 210   = invalid
                         * 209         = sith mask
                         * 211         = sith mask
                         * 213 - 249   = invalid
                         * 251 - 255   = invalid
                         */                         
                        parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 83, 95, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 153, 154, 166, 167, 174, 175, 176, 177, 178, 179, 180, 181, 183, 184, 185, 186, 187, 188, 212, 250 };
                    }
                    break;
            }


            ClearPageResponses("PartPage");
            foreach (var part in parts)
            {
                AddResponseToPage("PartPage", "Part #" + part, true, part);
            }

            ChangePage("PartPage");
        }

        private void HelmetPartResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            model.ItemTypeID = 0;
            int[] parts = { 0 };

            switch (responseID)
            {
                case 1: // Helmet
                    /* parts excluded for helmets:
                        *  13          = sith mask
                        *  22          = alien head
                        *  39 - 44     = creature heads
                        *  96 - 98     = alien heads
                        *  143 - 152   = creature or alien head
                        *  209         = sith mask
                        *  211         = sith mask
                        */
                    parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 210, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255 };
                    break;
            }

            ClearPageResponses("PartPage");
            foreach (var part in parts)
            {
                AddResponseToPage("PartPage", "Part #" + part, true, part);
            }

            ChangePage("PartPage");
        }

        private void PartResponses(int responseID)
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            int partID = (int)GetResponseByID("PartPage", responseID).CustomData;
            NWItem item = model.TargetItem;
            var slotID = model.InventorySlotID;
            var type = model.ItemTypeID;
            int index = model.Index;

            NWItem copy = CopyItemAndModify(item, type, index, partID, true);
            item.Destroy();
            model.TargetItem = copy;

            player.AssignCommand(() =>
            {
                SetCommandable(true, player);
                ActionEquipItem(copy, slotID);
                SetCommandable(false, player);
            });
        }


        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            SetCommandable(true, GetPC());
        }

        public override void EndDialog()
        {
            SetCommandable(true, GetPC());
        }
    }
}
