﻿using System;
using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class ModifyItemAppearance: ConversationBase
    {
        private class Model
        {
            public NWItem TargetItem { get; set; }
            public int ItemTypeID { get; set; }
            public int Index { get; set; }
            public int InventorySlotID { get; set; }
        }


        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "What would you like to modify?",
                "Save/Load Outfits",
                "Main Weapon",
                "Off-Hand Weapon",
                "Armor");

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
                "Left Glove");

            DialogPage partPage = new DialogPage(
                "Please select a new model.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("WeaponPartPage", weaponPartPage);
            dialog.AddPage("ArmorPartPage", armorPartPage);
            dialog.AddPage("PartPage", partPage);
            return dialog;
        }

        public override void Initialize()
        {
            _.SetCommandable(FALSE, GetPC());
            LoadMainPage();
        }
        
        private bool IsArmorValid()
        {
            NWPlayer player = GetPC();
            NWItem armor = player.Chest;

            bool canModifyArmor = armor.IsValid && !armor.IsPlot && !armor.IsCursed;
            return canModifyArmor;
        }

        private bool IsMainValid()
        {
            NWPlayer player = GetPC();
            NWItem main = player.RightHand;

            bool canModifyMain = main.IsValid && !main.IsPlot && !main.IsCursed;
            if (canModifyMain)
            {
                int mainModelTypeID = Convert.ToInt32(_.Get2DAString("baseitems", "ModelType", main.BaseItemType));
                canModifyMain = mainModelTypeID == ITEM_APPR_TYPE_WEAPON_MODEL;
            }

            return canModifyMain;
        }

        private bool IsOffHandValid()
        {
            NWPlayer player = GetPC();
            NWItem offHand = player.LeftHand;
            bool canModifyOffHand = offHand.IsValid && !offHand.IsPlot && !offHand.IsCursed;

            if (canModifyOffHand)
            {
                int offHandModelTypeID = Convert.ToInt32(_.Get2DAString("baseitems", "ModelType", offHand.BaseItemType));
                canModifyOffHand = offHandModelTypeID == ITEM_APPR_TYPE_WEAPON_MODEL;
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
                case "PartPage":
                    PartResponses(responseID);
                    break;
            }

        }

        private void LoadMainPage()
        {
            // Currently disabling weapons as we need to do some work to identify valid models.
            // Only armor will be working for now.
            SetResponseVisible("MainPage", 2, false);
            SetResponseVisible("MainPage", 3, false);
            SetResponseVisible("MainPage", 4, IsArmorValid());
        }

        private void MainResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            var player = GetPC();

            switch (responseID)
            {
                case 1: // Save/Load Outfits
                    _.SetCommandable(TRUE, player);
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
                        model.InventorySlotID = INVENTORY_SLOT_RIGHTHAND;
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
                        model.InventorySlotID = INVENTORY_SLOT_LEFTHAND;
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
                        model.InventorySlotID = INVENTORY_SLOT_CHEST;
                        ChangePage("ArmorPartPage");
                    }

                    break;
            }
        }

        private void WeaponPartResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            model.ItemTypeID = ITEM_APPR_TYPE_WEAPON_MODEL;

            switch (responseID)
            {
                case 1: // Top
                    model.Index = ITEM_APPR_WEAPON_MODEL_TOP;
                    break;
                case 2: // Middle
                    model.Index = ITEM_APPR_WEAPON_MODEL_MIDDLE;
                    break;
                case 3: // Bottom
                    model.Index = ITEM_APPR_WEAPON_MODEL_BOTTOM;
                    break;
            }
        }

        private void ArmorPartResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            model.ItemTypeID = ITEM_APPR_TYPE_ARMOR_MODEL;
            int[] parts = {0};

            switch (responseID)
            {
                case 1: // Neck
                    model.Index = ITEM_APPR_ARMOR_MODEL_NECK;
                    parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10, 26, 30, 31, 32, 50, 63, 95, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 121, 122, 123, 124, 125, 126, 127, 128, 129, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 198, 199, 201, 203, 250, 254, 257, 258, 259, };
                    break;
                case 2: // Torso
                    model.Index = ITEM_APPR_ARMOR_MODEL_TORSO;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 72, 75, 76, 77, 78, 79, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 163, 166, 167, 168, 171, 172, 173, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 193, 196, 197, 198, 199, 200, 201, 202, 203, 210, 212, 219, 220, 221, 250, 253, 258, 259, };
                    break;
                case 3: // Belt
                    model.Index = ITEM_APPR_ARMOR_MODEL_BELT;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 23, 24, 25, 26, 30, 31, 32, 63, 70, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 188, 189, 190, 191, 192, 198, 219, 220, 221, };
                    break;
                case 4: // Pelvis
                    model.Index = ITEM_APPR_ARMOR_MODEL_PELVIS;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42, 50, 63, 75, 101, 102, 103, 104, 105, 106, 108, 109, 110, 111, 117, 122, 123, 140, 141, 142, 143, 144, 146, 151, 153, 154, 155, 156, 157, 158, 161, 163, 164, 165, 166, 186, 198, 199, 201, 202, 203, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 250, 253, 257, 258, 259, };
                    break;
                case 5: // Robe
                    model.Index = ITEM_APPR_ARMOR_MODEL_ROBE;
                    parts = new[] { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 20, 21, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 121, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 169, 170, 171, 173, 174, 182, 183, 184, 185, 186, 187, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 221, 222, 223, 226, 227, 230, 234, 235, 247, 248, 249, 250, 252, 253, 255, 259, };
                    break;
                case 6: // Right Thigh
                    model.Index = ITEM_APPR_ARMOR_MODEL_RTHIGH;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 23, 24, 30, 31, 50, 51, 52, 53, 54, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 118, 121, 122, 123, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 186, 198, 199, 201, 202, 203, 220, 233, 234, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259, };
                    break;
                case 7: // Right Shin
                    model.Index = ITEM_APPR_ARMOR_MODEL_RSHIN;
                    parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 26, 27, 30, 50, 51, 54, 55, 56, 57, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 121, 128, 129, 130, 131, 132, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 160, 161, 162, 164, 165, 166, 186, 198, 199, 201, 202, 203, 219, 220, 221, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, };
                    break;
                case 8: // Right Foot
                    model.Index = ITEM_APPR_ARMOR_MODEL_RFOOT;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 18, 30, 50, 51, 52, 63, 75, 80, 81, 82, 83, 101, 102, 103, 104, 105, 106, 107, 110, 116, 117, 118, 121, 122, 123, 124, 145, 146, 151, 152, 154, 155, 156, 157, 158, 160, 186, 190, 198, 199, 201, 202, 203, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259, };
                    break;
                case 9: // Left Thigh
                    model.Index = ITEM_APPR_ARMOR_MODEL_LTHIGH;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 23, 24, 30, 31, 50, 51, 52, 53, 54, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 118, 121, 122, 123, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 186, 198, 199, 201, 202, 203, 220, 233, 234, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259, };
                    break;
                case 10: // Left Shin
                    model.Index = ITEM_APPR_ARMOR_MODEL_LSHIN;
                    parts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 26, 27, 30, 50, 51, 54, 55, 56, 57, 63, 75, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 102, 103, 104, 105, 106, 107, 108, 109, 110, 116, 117, 121, 128, 129, 130, 131, 132, 140, 141, 142, 143, 146, 151, 152, 153, 154, 155, 156, 157, 158, 160, 161, 162, 164, 165, 166, 186, 198, 199, 201, 202, 203, 219, 220, 221, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, };
                    break;
                case 11: // Left Foot
                    model.Index = ITEM_APPR_ARMOR_MODEL_LFOOT;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 18, 30, 50, 51, 52, 63, 75, 80, 81, 82, 83, 101, 102, 103, 104, 105, 106, 107, 110, 116, 117, 118, 121, 122, 123, 124, 145, 146, 151, 152, 154, 155, 156, 157, 158, 160, 186, 190, 198, 199, 201, 202, 203, 243, 244, 245, 246, 247, 248, 249, 250, 253, 254, 255, 257, 258, 259, };
                    break;
                case 12: // Right Shoulder
                    model.Index = ITEM_APPR_ARMOR_MODEL_RSHOULDER;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 54, 55, 56, 57, 58, 100, 101, 102, 122, 123, 140, 141, 185, 186, 197, 198, 199, 219, 220, 221, 250, };
                    break;
                case 13: // Right Bicep
                    model.Index = ITEM_APPR_ARMOR_MODEL_RBICEP;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 20, 21, 22, 30, 31, 32, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 68, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 182, 183, 186, 198, 199, 201, 202, 203, 250, 257, 258, 259, };
                    break;
                case 14: // Right Forearm
                    model.Index = ITEM_APPR_ARMOR_MODEL_RFOREARM;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 28, 30, 40, 41, 54, 55, 56, 57, 58, 63, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 140, 141, 142, 143, 144, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 167, 168, 169, 186, 198, 199, 200, 201, 202, 203, 215, 219, 220, 221, 250, 257, 258, 259, };
                    break;
                case 15: // Right Glove
                    model.Index = ITEM_APPR_ARMOR_MODEL_RHAND;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 63, 75, 100, 101, 109, 110, 111, 113, 121, 122, 150, 151, 152, 153, 154, 155, 186, 192, 193, 194, 195, 196, 198, 201, 202, 203, 215, 250, 257, 258, 259, };
                    break;
                case 16: // Left Shoulder
                    model.Index = ITEM_APPR_ARMOR_MODEL_LSHOULDER;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 54, 55, 56, 57, 58, 100, 101, 102, 122, 123, 140, 141, 185, 186, 197, 198, 199, 219, 220, 221, 250, };
                    break;
                case 17: // Left Bicep
                    model.Index = ITEM_APPR_ARMOR_MODEL_LBICEP;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 20, 21, 22, 30, 31, 32, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 68, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 140, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 182, 183, 186, 198, 199, 201, 202, 203, 250, 257, 258, 259, };
                    break;
                case 18: // Left Forearm
                    model.Index = ITEM_APPR_ARMOR_MODEL_LFOREARM;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 28, 30, 40, 41, 54, 55, 56, 57, 58, 63, 75, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 140, 141, 142, 143, 144, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 167, 168, 169, 186, 198, 199, 200, 201, 202, 203, 215, 219, 220, 221, 250, 257, 258, 259, };
                    break;
                case 19: // Left Glove
                    model.Index = ITEM_APPR_ARMOR_MODEL_LHAND;
                    parts = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 63, 75, 100, 101, 109, 110, 111, 113, 121, 122, 150, 151, 152, 153, 154, 155, 186, 192, 193, 194, 195, 196, 198, 201, 202, 203, 215, 250, 257, 258, 259, };
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
            int slotID = model.InventorySlotID;
            int type = model.ItemTypeID;
            int index = model.Index;

            player.SetLocalInt("IS_CUSTOMIZING_ITEM", TRUE);
            NWItem copy = _.CopyItemAndModify(item, type, index, partID, TRUE);
            item.Destroy();
            model.TargetItem = copy;

            player.AssignCommand(() =>
            {
                _.SetCommandable(TRUE, player);
                _.ActionEquipItem(copy, slotID);
                _.SetCommandable(FALSE, player);
                player.DeleteLocalInt("IS_CUSTOMIZING_ITEM");
            });
        }


        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            _.SetCommandable(TRUE, GetPC());
        }

        public override void EndDialog()
        {
            _.SetCommandable(TRUE, GetPC());
        }
    }
}
