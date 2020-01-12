using System;
using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Conversation
{
    public class Outfit: ConversationBase
    {
        private const int MaxSaveSlots = 10;

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "Please select an option.",
                "Save Options",
                "Load Options"
            );

            DialogPage savePage = new DialogPage(
                "Which type of item would you like to save?"
            );

            DialogPage loadPage = new DialogPage(
                "Which type of item would you like to load?"
            );

            DialogPage saveOutfitPage = new DialogPage(
                "Please select a slot to save the outfit in.\n\nRed slots are unused. Green slots contain stored appearances. Selecting a green slot will overwrite whatever is in that slot."
            );

            DialogPage saveHelmetPage = new DialogPage(
                "Please select a slot to save the helmet in.\n\nRed slots are unused. Green slots contain stored appearances. Selecting a green slot will overwrite whatever is in that slot."
            );

            DialogPage saveWeaponPage = new DialogPage(
                "Please select a slot to save the weapon in. (Right hand only)\n\nRed slots are unused. Green slots contain stored appearances. Selecting a green slot will overwrite whatever is in that slot."
            );

            DialogPage loadOutfitPage = new DialogPage(
                "Please select an outfit to load."
            );

            DialogPage loadHelmetPage = new DialogPage(
                "Please select a helmet to load."
            );

            DialogPage loadWeaponPage = new DialogPage(
                "Please select a weapon to load. (Right hand only)"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("SavePage", savePage);
            dialog.AddPage("SaveOutfitPage", saveOutfitPage);
            dialog.AddPage("SaveHelmetPage", saveHelmetPage);
            dialog.AddPage("SaveWeaponPage", saveWeaponPage);
            dialog.AddPage("LoadPage", loadPage);            
            dialog.AddPage("LoadOutfitPage", loadOutfitPage);
            dialog.AddPage("LoadHelmetPage", loadHelmetPage);
            dialog.AddPage("LoadWeaponPage", loadWeaponPage);
            return dialog;
        }

        public override void Initialize()
        {
            _.SetCommandable(true, GetPC());
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                {
                    switch (responseID)
                    {
                        case 1: // Save Page
                                ShowSaveOptions();
                                ChangePage("SavePage");
                            break;
                        case 2: // Load Page
                                ShowLoadOptions();
                                ChangePage("LoadPage");
                            break;
                    }
                    break;
                }
                case "SavePage":
                    {
                        switch (responseID)
                        {
                            case 1: // Save Outfit Page
                                ShowSaveOutfitOptions();
                                ChangePage("SaveOutfitPage");
                                break;
                            case 2: // Save Helmet Page
                                ShowSaveHelmetOptions();
                                ChangePage("SaveHelmetPage");
                                break;
                            case 3: // Save Weapon Page
                                ShowSaveWeaponOptions();
                                ChangePage("SaveWeaponPage");
                                break;
                        }
                        break;
                    }
                case "LoadPage":
                    {
                        switch (responseID)
                        {
                            case 1: // Load Outfit
                                ShowLoadOutfitOptions();
                                ChangePage("LoadOutfitPage");
                                break;
                            case 2: // Load Helmet
                                ShowLoadHelmetOptions();
                                ChangePage("LoadHelmetPage");
                                break;
                            case 3: // Load Weapon
                                ShowLoadWeaponOptions(player);
                                ChangePage("LoadWeaponPage");
                                break;
                        }
                        break;
                    }
                    // default base?
                    /*
                case "LoadPage":
                    {
                        switch (responseID)
                        {
                            case 1: // Save Outfit
                                ShowSaveOutfitOptions();
                                ChangePage("SaveOutfitPage");
                                break;
                            case 2: // Load Outfit
                                ShowLoadOutfitOptions();
                                ChangePage("LoadOutfitPage");
                                break;
                        }
                        break;
                    }*/
                case "SaveOutfitPage":
                {
                    HandleSaveOutfit(responseID);
                    break;
                }
                case "SaveHelmetPage":
                {
                    HandleSaveHelmet(responseID);
                    break;
                }
                case "SaveWeaponPage":
                    {
                        HandleSaveWeapon(responseID);
                        break;
                    }
                case "LoadOutfitPage":
                {
                    HandleLoadOutfit(responseID);
                    break;
                }
                case "LoadHelmetPage":
                {
                    HandleLoadHelmet(responseID);
                    break;
                }
                case "LoadWeaponPage":
                    {
                        HandleLoadWeapon(responseID);
                        break;
                    }
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private bool CanModifyClothes()
        {
            NWPlayer oPC = GetPC();
            NWItem oClothes = (_.GetItemInSlot(InventorySlot.Chest, oPC.Object));

            bool canModifyArmor = oClothes.IsValid && !oClothes.IsPlot && !oClothes.IsCursed;
            if (!canModifyArmor)
            {
                return false;
            }

            return true;
        }
        private bool CanModifyHelmet()
        {
            NWPlayer oPC = GetPC();
            NWItem oClothes = (_.GetItemInSlot(InventorySlot.Head, oPC.Object));

            bool canModifyArmor = oClothes.IsValid && !oClothes.IsPlot && !oClothes.IsCursed;
            if (!canModifyArmor)
            {
                return false;
            }

            return true;
        }
        private bool CanModifyWeapon()
        {
            NWPlayer oPC = GetPC();
            NWItem oClothes = (_.GetItemInSlot(InventorySlot.RightHand, oPC.Object));

            bool canModifyArmor = oClothes.IsValid && !oClothes.IsPlot && !oClothes.IsCursed;
            if (!canModifyArmor)
            {
                return false;
            }

            return true;
        }
        private void HandleSaveOutfit(int responseID)
        {
            NWPlayer oPC = GetPC();
            NWItem oClothes = (_.GetItemInSlot(InventorySlot.Chest, oPC.Object));
            
            if (!CanModifyClothes())
            {
                oPC.FloatingText("You cannot save your currently equipped clothes.");
                return;
            }
            
            if (!oClothes.IsValid)
            {
                oPC.FloatingText(ColorTokenService.Red("You do not have clothes equipped"));
                return;
            }

            string clothesData = SerializationService.Serialize(oClothes);
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            dbPlayer.SavedOutfits[responseID] = clothesData;

            DataService.Set(dbPlayer);
            ShowSaveOutfitOptions();
        }
        private void HandleSaveHelmet(int responseID)
        {
            NWPlayer oPC = GetPC();
            NWItem helmet = (_.GetItemInSlot(InventorySlot.Head, oPC.Object));

            if (!CanModifyHelmet())
            {
                oPC.FloatingText("You cannot save your currently equipped helmet.");
                return;
            }

            if (!helmet.IsValid)
            {
                oPC.FloatingText(ColorTokenService.Red("You do not have a helmet equipped"));
                return;
            }

            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            string helmetData = SerializationService.Serialize(helmet);
            dbPlayer.SavedHelmets[responseID] = helmetData;

            DataService.Set(dbPlayer);
            ShowSaveHelmetOptions();
        }
        private void HandleSaveWeapon(int responseID)
        {
            NWPlayer oPC = GetPC();
            NWItem oClothes = (_.GetItemInSlot(InventorySlot.RightHand, oPC.Object));

            if (!CanModifyWeapon())
            {
                oPC.FloatingText("You cannot save your currently equipped weapon.");
                return;
            }

            if (!oClothes.IsValid)
            {
                oPC.FloatingText(ColorTokenService.Red("You do not have a weapon equipped"));
                return;
            }

            string weaponData = SerializationService.Serialize(oClothes);
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            if (!dbPlayer.SavedWeapons.ContainsKey(oPC.RightHand.BaseItemType))
            {
                dbPlayer.SavedWeapons[oPC.RightHand.BaseItemType] = new Dictionary<int, string>();
            }

            dbPlayer.SavedWeapons[oPC.RightHand.BaseItemType][responseID] = weaponData;

            DataService.Set(dbPlayer);
            ShowSaveWeaponOptions();
        }
        private void HandleLoadOutfit(int responseID)
        {
            DialogResponse response = GetResponseByID("LoadOutfitPage", responseID);
            NWPlayer oPC = GetPC();

            if (!CanModifyClothes())
            {
                oPC.FloatingText("You cannot modify your currently equipped clothes.");
                return;
            }

            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            int outfitID = (int)response.CustomData;
            var outfits = dbPlayer.SavedOutfits;
            if (!outfits.ContainsKey(outfitID)) return;

            NWPlaceable oTempStorage = (_.GetObjectByTag("OUTFIT_BARREL"));
            NWItem oClothes = oPC.Chest;
            NWItem storedClothes = SerializationService.DeserializeItem(outfits[outfitID], oTempStorage);
            oClothes.SetLocalString("TEMP_OUTFIT_UUID", oPC.GlobalID.ToString());

            if (storedClothes == null) throw new Exception("Unable to locate stored clothes.");

            NWGameObject oCopy = _.CopyItem(oClothes.Object, oTempStorage.Object, true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel,  ItemApprArmorModel.LeftBicep, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.LeftBicep), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.LeftBicep, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.LeftBicep), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.Belt, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.Belt), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.Belt, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.Belt), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.LeftFoot, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.LeftFoot), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.LeftFoot, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.LeftFoot), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.LeftForearm, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.LeftForearm), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.LeftForearm, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.LeftForearm), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.LeftHand, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.LeftHand), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.LeftHand, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.LeftHand), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.LeftShin, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.LeftShin), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.LeftShin, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.LeftShin), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.LeftShoulder, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.LeftShoulder), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.LeftShoulder, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.LeftShoulder), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.LeftThigh, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.LeftThigh), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.LeftThigh, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.LeftThigh), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.Neck, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.Neck), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.Neck, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.Neck), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.Pelvis, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.Pelvis), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.Pelvis, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.Pelvis), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.RightBicep, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.RightBicep), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.RightBicep, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.RightBicep), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.RightFoot, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.RightFoot), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.RightFoot, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.RightFoot), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.RightForearm, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.RightForearm), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.RightForearm, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.RightForearm), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.RightHand, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.RightHand), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.RightHand, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.RightHand), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.Robe, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.Robe), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.Robe, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.Robe), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.RightShin, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.RightShin), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.RightShin, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.RightShin), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.RightShoulder, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.RightShoulder), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.RightShoulder, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.RightShoulder), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.RightThigh, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.RightThigh), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.RightThigh, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.RightThigh), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorModel, ItemApprArmorModel.Torso, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorModel, ItemApprArmorModel.Torso), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.ArmorColor, ItemApprArmorModel.Torso, _.GetItemAppearance(storedClothes.Object, ItemApprType.ArmorColor, ItemApprArmorModel.Torso), true);

            NWItem oFinal = (_.CopyItem(oCopy, oPC.Object, true));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            _.DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => _.ActionEquipItem(oFinal.Object, InventorySlot.Chest));

            foreach (NWItem item in oTempStorage.InventoryItems)
            {
                if (item.GetLocalString("TEMP_OUTFIT_UUID") == oPC.GlobalID.ToString())
                {
                    item.Destroy();
                }
            }

            ShowLoadOutfitOptions();
        }
        private void HandleLoadHelmet(int responseID)
        {
            DialogResponse response = GetResponseByID("LoadHelmetPage", responseID);
            NWPlayer oPC = GetPC();

            if (!CanModifyHelmet())
            {
                oPC.FloatingText("You cannot modify your currently equipped helmet.");
                return;
            }

            int helmetID = (int)response.CustomData;
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            var helmets = dbPlayer.SavedHelmets;
            if (!helmets.ContainsKey(helmetID)) return;

            NWPlaceable oTempStorage = (_.GetObjectByTag("OUTFIT_BARREL"));
            NWItem oClothes = oPC.Head;
            NWItem storedClothes = SerializationService.DeserializeItem(helmets[helmetID], oTempStorage);
            oClothes.SetLocalString("TEMP_OUTFIT_UUID", oPC.GlobalID.ToString());

            if (storedClothes == null) throw new Exception("Unable to locate stored helmet.");

            NWGameObject oCopy = _.CopyItem(oClothes.Object, oTempStorage.Object, true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.SimpleModel, ItemApprType.SimpleModel, _.GetItemAppearance(storedClothes.Object, ItemApprType.SimpleModel, ItemApprType.SimpleModel), true);

            NWItem oFinal = (_.CopyItem(oCopy, oPC.Object, true));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            _.DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => _.ActionEquipItem(oFinal.Object, InventorySlot.Head));

            foreach (NWItem item in oTempStorage.InventoryItems)
            {
                if (item.GetLocalString("TEMP_OUTFIT_UUID") == oPC.GlobalID.ToString())
                {
                    item.Destroy();
                }
            }

            ShowLoadHelmetOptions();
        }
        private void HandleLoadWeapon(int responseID)
        {
            DialogResponse response = GetResponseByID("LoadWeaponPage", responseID);
            NWPlayer oPC = GetPC();

            if (!CanModifyWeapon())
            {
                oPC.FloatingText("You cannot modify your currently equipped Weapon.");
                return;
            }

            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            int outfitID = (int)response.CustomData;
            var weapons = dbPlayer.SavedWeapons;
            if (!weapons[oPC.RightHand.BaseItemType].ContainsKey(outfitID)) return;

            NWPlaceable oTempStorage = (_.GetObjectByTag("OUTFIT_BARREL"));
            NWItem oClothes = oPC.RightHand;
            NWItem storedClothes = SerializationService.DeserializeItem(weapons[oPC.RightHand.BaseItemType][outfitID], oTempStorage);
            oClothes.SetLocalString("TEMP_OUTFIT_UUID", oPC.GlobalID.ToString());

            if (storedClothes == null) throw new Exception("Unable to locate stored Weapon.");

            NWGameObject oCopy = _.CopyItem(oClothes.Object, oTempStorage.Object, true);

            var baseItemType = GetBaseItemType(oCopy);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.SimpleModel, ItemApprType.SimpleModel, _.GetItemAppearance(storedClothes.Object, ItemApprType.SimpleModel, ItemApprType.SimpleModel), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.WeaponModel, ItemApprWeaponModel.Bottom, _.GetItemAppearance(storedClothes.Object, ItemApprType.WeaponModel, ItemApprWeaponModel.Bottom), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.WeaponColor, ItemApprWeaponColor.Bottom, _.GetItemAppearance(storedClothes.Object, ItemApprType.WeaponColor, ItemApprWeaponColor.Bottom), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.WeaponModel, ItemApprWeaponModel.Middle, _.GetItemAppearance(storedClothes.Object, ItemApprType.WeaponModel, ItemApprWeaponModel.Middle), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.WeaponColor, ItemApprWeaponColor.Middle, _.GetItemAppearance(storedClothes.Object, ItemApprType.WeaponColor, ItemApprWeaponColor.Middle), true);

            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.WeaponModel, ItemApprWeaponModel.Top, _.GetItemAppearance(storedClothes.Object, ItemApprType.WeaponModel, ItemApprWeaponModel.Top), true);
            oCopy = _.CopyItemAndModify(oCopy, ItemApprType.WeaponColor, ItemApprWeaponColor.Top, _.GetItemAppearance(storedClothes.Object, ItemApprType.WeaponColor, ItemApprWeaponColor.Top), true);

            NWItem oFinal = (_.CopyItem(oCopy, oPC.Object, true));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            _.DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => _.ActionEquipItem(oFinal.Object, InventorySlot.RightHand));

            foreach (NWItem item in oTempStorage.InventoryItems)
            {
                if (item.GetLocalString("TEMP_OUTFIT_UUID") == oPC.GlobalID.ToString())
                {
                    item.Destroy();
                }
            }

            ShowLoadWeaponOptions(oPC);
        }
        private void ShowSaveOptions()
        {            
            ClearPageResponses("SavePage");
            AddResponseToPage("SavePage", "Save Outfit");
            AddResponseToPage("SavePage", "Save Helmet");
            AddResponseToPage("SavePage", "Save Weapon");
        }
        private void ShowSaveOutfitOptions()
        {
            ClearPageResponses("SaveOutfitPage");
            var dbPlayer = DataService.Player.GetByID(GetPC().GlobalID);

            for (int x = 1; x <= MaxSaveSlots; x++)
            {
                if (dbPlayer.SavedOutfits.ContainsKey(x))
                {
                    AddResponseToPage("SaveOutfitPage", ColorTokenService.Green($"Save in Slot {x}"));
                }
                else
                {
                    AddResponseToPage("SaveOutfitPage", ColorTokenService.Red($"Save in Slot {x}"));
                }
            }
        }
        private void ShowSaveHelmetOptions()
        {
            ClearPageResponses("SaveHelmetPage");
            var dbPlayer = DataService.Player.GetByID(GetPC().GlobalID);

            for (int x = 1; x <= MaxSaveSlots; x++)
            {
                if (dbPlayer.SavedHelmets.ContainsKey(x))
                {
                    AddResponseToPage("SaveHelmetPage", ColorTokenService.Green($"Save in Slot {x}"));
                }
                else
                {
                    AddResponseToPage("SaveHelmetPage", ColorTokenService.Red($"Save in Slot {x}"));
                }
            }
        }
        private void ShowSaveWeaponOptions()
        {
            ClearPageResponses("SaveWeaponPage");

            var dbPlayer = DataService.Player.GetByID(GetPC().GlobalID);
            NWPlaceable oTempStorage = (_.GetObjectByTag("OUTFIT_BARREL"));

            for (int x = 1; x <= MaxSaveSlots; x++)
            {
                if (dbPlayer.SavedWeapons.ContainsKey(GetPC().RightHand.BaseItemType))
                {
                    if (dbPlayer.SavedWeapons[GetPC().RightHand.BaseItemType].ContainsKey(x))
                    {
                        NWItem storedClothes = SerializationService.DeserializeItem(dbPlayer.SavedWeapons[GetPC().RightHand.BaseItemType][x], oTempStorage);
                        storedClothes.SetLocalString("TEMP_OUTFIT_UUID", GetPC().GlobalID.ToString());
                        AddResponseToPage("SaveWeaponPage", ColorTokenService.Green($"Save in Slot {x}" + " (Type: " + storedClothes.BaseItemType.ToString() + ")"));
                    }
                    else
                    {
                        AddResponseToPage("SaveWeaponPage", ColorTokenService.Red($"Save in Slot {x}"));
                    }
                }
                else
                {
                    AddResponseToPage("SaveWeaponPage", ColorTokenService.Red($"Save in Slot {x}"));
                }

                foreach (NWItem item in oTempStorage.InventoryItems)
                {
                    if (item.GetLocalString("TEMP_OUTFIT_UUID") == GetPC().GlobalID.ToString())
                    {
                        item.Destroy();
                    }
                }
            }
        }
        private void ShowLoadOptions()
        {
            ClearPageResponses("LoadPage");
            AddResponseToPage("LoadPage", "Load Outfit");
            AddResponseToPage("LoadPage", "Load Helmet");
            AddResponseToPage("LoadPage", "Load Weapon");
        }
        private void ShowLoadOutfitOptions()
        {
            ClearPageResponses("LoadOutfitPage");

            var dbPlayer = DataService.Player.GetByID(GetPC().GlobalID);
            for(int x = 1; x <= MaxSaveSlots; x++)
            {
                if(dbPlayer.SavedOutfits.ContainsKey(x))
                {
                    AddResponseToPage("LoadOutfitPage", $"Load from Slot {x}", true, x);
                }
            }
        }
        private void ShowLoadHelmetOptions()
        {
            ClearPageResponses("LoadHelmetPage");

            var dbPlayer = DataService.Player.GetByID(GetPC().GlobalID);
            for (int x = 1; x <= MaxSaveSlots; x++)
            {
                if (dbPlayer.SavedHelmets.ContainsKey(x))
                {
                    AddResponseToPage("LoadHelmetPage", $"Load from Slot {x}", true, x);
                }
            }
        }
        private void ShowLoadWeaponOptions(NWPlayer player)
        {
            ClearPageResponses("LoadWeaponPage");
            NWPlaceable oTempStorage = (_.GetObjectByTag("OUTFIT_BARREL"));

            var dbPlayer = DataService.Player.GetByID(GetPC().GlobalID);
            for (int x = 1; x <= MaxSaveSlots; x++)
            {
                if (dbPlayer.SavedWeapons[player.RightHand.BaseItemType].ContainsKey(x))
                {
                    NWItem storedClothes = SerializationService.DeserializeItem(dbPlayer.SavedWeapons[player.RightHand.BaseItemType][x], oTempStorage);
                    storedClothes.SetLocalString("TEMP_OUTFIT_UUID", player.GlobalID.ToString());
                    if (storedClothes.BaseItemType == player.RightHand.BaseItemType)
                    {
                        AddResponseToPage("LoadWeaponPage", $"Load from Slot {x}" + " (Type: " + storedClothes.BaseItemType.ToString() + ")", true, x);
                    }
                }
            }            

            foreach (NWItem item in oTempStorage.InventoryItems)
            {
                if (item.GetLocalString("TEMP_OUTFIT_UUID") == player.GlobalID.ToString())
                {
                    item.Destroy();
                }
            }
        }
        public override void EndDialog()
        {
        }
    }
}
