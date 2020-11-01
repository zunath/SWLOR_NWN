using System;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class Outfit: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage(
                "Please select an option.",
                "Save Options",
                "Load Options"
            );

            var savePage = new DialogPage(
                "Which type of item would you like to save?"
            );

            var loadPage = new DialogPage(
                "Which type of item would you like to load?"
            );

            var saveOutfitPage = new DialogPage(
                "Please select a slot to save the outfit in.\n\nRed slots are unused. Green slots contain stored appearances. Selecting a green slot will overwrite whatever is in that slot."
            );

            var saveHelmetPage = new DialogPage(
                "Please select a slot to save the helmet in.\n\nRed slots are unused. Green slots contain stored appearances. Selecting a green slot will overwrite whatever is in that slot."
            );

            var saveWeaponPage = new DialogPage(
                "Please select a slot to save the weapon in. (Right hand only)\n\nRed slots are unused. Green slots contain stored appearances. Selecting a green slot will overwrite whatever is in that slot."
            );

            var loadOutfitPage = new DialogPage(
                "Please select an outfit to load."
            );

            var loadHelmetPage = new DialogPage(
                "Please select a helmet to load."
            );

            var loadWeaponPage = new DialogPage(
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
            SetCommandable(true, GetPC());
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
                                ShowLoadWeaponOptions();
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

        private PCOutfit GetPlayerOutfits(NWPlayer oPC)
        {
            return DataService.PCOutfit.GetByIDOrDefault(oPC.GlobalID);
        }
        private PCHelmet GetPlayerHelmets(NWPlayer oPC)
        {
            return DataService.PCHelmet.GetByIDOrDefault(oPC.GlobalID);
        }
        private PCWeapon GetPlayerWeapons(NWPlayer oPC)
        {
            return DataService.PCWeapon.GetByIDOrDefault(oPC.GlobalID);
        }
        private bool CanModifyClothes()
        {
            var oPC = GetPC();
            NWItem oClothes = (GetItemInSlot(InventorySlot.Chest, oPC.Object));

            var canModifyArmor = oClothes.IsValid && !oClothes.IsPlot && !oClothes.IsCursed;
            if (!canModifyArmor)
            {
                return false;
            }

            return true;
        }
        private bool CanModifyHelmet()
        {
            var oPC = GetPC();
            NWItem oClothes = (GetItemInSlot(InventorySlot.Head, oPC.Object));

            var canModifyArmor = oClothes.IsValid && !oClothes.IsPlot && !oClothes.IsCursed;
            if (!canModifyArmor)
            {
                return false;
            }

            return true;
        }
        private bool CanModifyWeapon()
        {
            var oPC = GetPC();
            NWItem oClothes = (GetItemInSlot(InventorySlot.RightHand, oPC.Object));

            var canModifyArmor = oClothes.IsValid && !oClothes.IsPlot && !oClothes.IsCursed;
            if (!canModifyArmor)
            {
                return false;
            }

            return true;
        }
        private void HandleSaveOutfit(int responseID)
        {
            var oPC = GetPC();
            NWItem oClothes = (GetItemInSlot(InventorySlot.Chest, oPC.Object));

            if (!CanModifyClothes())
            {
                oPC.FloatingText("You cannot save your currently equipped clothes.");
                return;
            }
            
            var entity = GetPlayerOutfits(oPC);
            var action = DatabaseActionType.Update;

            if (entity == null)
            {
                entity = new PCOutfit
                {
                    PlayerID = oPC.GlobalID
                };
                action = DatabaseActionType.Insert;
            }

            if (!oClothes.IsValid)
            {
                oPC.FloatingText(ColorTokenService.Red("You do not have clothes equipped"));
                return;
            }

            var clothesData = SerializationService.Serialize(oClothes);
            if (responseID == 1) entity.Outfit1 = clothesData;
            else if (responseID == 2) entity.Outfit2 = clothesData;
            else if (responseID == 3) entity.Outfit3 = clothesData;
            else if (responseID == 4) entity.Outfit4 = clothesData;
            else if (responseID == 5) entity.Outfit5 = clothesData;
            else if (responseID == 6) entity.Outfit6 = clothesData;
            else if (responseID == 7) entity.Outfit7 = clothesData;
            else if (responseID == 8) entity.Outfit8 = clothesData;
            else if (responseID == 9) entity.Outfit9 = clothesData;
            else if (responseID == 10) entity.Outfit10 = clothesData;

            DataService.SubmitDataChange(entity, action);
            ShowSaveOutfitOptions();
        }
        private void HandleSaveHelmet(int responseID)
        {
            var oPC = GetPC();
            NWItem oClothes = (GetItemInSlot(InventorySlot.Head, oPC.Object));

            if (!CanModifyHelmet())
            {
                oPC.FloatingText("You cannot save your currently equipped helmet.");
                return;
            }

            var entity = GetPlayerHelmets(oPC);
            var action = DatabaseActionType.Update;

            if (entity == null)
            {
                entity = new PCHelmet
                {
                    PlayerID = oPC.GlobalID
                };
                action = DatabaseActionType.Insert;
            }

            if (!oClothes.IsValid)
            {
                oPC.FloatingText(ColorTokenService.Red("You do not have a helmet equipped"));
                return;
            }

            var clothesData = SerializationService.Serialize(oClothes);
            if (responseID == 1) entity.Helmet1 = clothesData;
            else if (responseID == 2) entity.Helmet2 = clothesData;
            else if (responseID == 3) entity.Helmet3 = clothesData;
            else if (responseID == 4) entity.Helmet4 = clothesData;
            else if (responseID == 5) entity.Helmet5 = clothesData;
            else if (responseID == 6) entity.Helmet6 = clothesData;
            else if (responseID == 7) entity.Helmet7 = clothesData;
            else if (responseID == 8) entity.Helmet8 = clothesData;
            else if (responseID == 9) entity.Helmet9 = clothesData;
            else if (responseID == 10) entity.Helmet10 = clothesData;

            DataService.SubmitDataChange(entity, action);
            ShowSaveHelmetOptions();
        }
        private void HandleSaveWeapon(int responseID)
        {
            var oPC = GetPC();
            NWItem oClothes = (GetItemInSlot(InventorySlot.RightHand, oPC.Object));

            if (!CanModifyWeapon())
            {
                oPC.FloatingText("You cannot save your currently equipped Weapon.");
                return;
            }

            var entity = GetPlayerWeapons(oPC);
            var action = DatabaseActionType.Update;

            if (entity == null)
            {
                entity = new PCWeapon
                {
                    PlayerID = oPC.GlobalID
                };
                action = DatabaseActionType.Insert;
            }

            if (!oClothes.IsValid)
            {
                oPC.FloatingText(ColorTokenService.Red("You do not have a Weapon equipped"));
                return;
            }

            var clothesData = SerializationService.Serialize(oClothes);
            if (responseID == 1) entity.Weapon1 = clothesData;
            else if (responseID == 2) entity.Weapon2 = clothesData;
            else if (responseID == 3) entity.Weapon3 = clothesData;
            else if (responseID == 4) entity.Weapon4 = clothesData;
            else if (responseID == 5) entity.Weapon5 = clothesData;
            else if (responseID == 6) entity.Weapon6 = clothesData;
            else if (responseID == 7) entity.Weapon7 = clothesData;
            else if (responseID == 8) entity.Weapon8 = clothesData;
            else if (responseID == 9) entity.Weapon9 = clothesData;
            else if (responseID == 10) entity.Weapon10 = clothesData;

            DataService.SubmitDataChange(entity, action);
            ShowSaveWeaponOptions();
        }
        private void HandleLoadOutfit(int responseID)
        {
            var response = GetResponseByID("LoadOutfitPage", responseID);
            var oPC = GetPC();

            if (!CanModifyClothes())
            {
                oPC.FloatingText("You cannot modify your currently equipped clothes.");
                return;
            }

            var outfitID = (int)response.CustomData;
            var entity = GetPlayerOutfits(GetPC());
            if (entity == null) return;

            NWPlaceable oTempStorage = (GetObjectByTag("OUTFIT_BARREL"));
            var oClothes = oPC.Chest;
            NWItem storedClothes = null;
            oClothes.SetLocalString("TEMP_OUTFIT_UUID", oPC.GlobalID.ToString());

            if (outfitID == 1) storedClothes = SerializationService.DeserializeItem(entity.Outfit1, oTempStorage);
            else if (outfitID == 2) storedClothes = SerializationService.DeserializeItem(entity.Outfit2, oTempStorage);
            else if (outfitID == 3) storedClothes = SerializationService.DeserializeItem(entity.Outfit3, oTempStorage);
            else if (outfitID == 4) storedClothes = SerializationService.DeserializeItem(entity.Outfit4, oTempStorage);
            else if (outfitID == 5) storedClothes = SerializationService.DeserializeItem(entity.Outfit5, oTempStorage);
            else if (outfitID == 6) storedClothes = SerializationService.DeserializeItem(entity.Outfit6, oTempStorage);
            else if (outfitID == 7) storedClothes = SerializationService.DeserializeItem(entity.Outfit7, oTempStorage);
            else if (outfitID == 8) storedClothes = SerializationService.DeserializeItem(entity.Outfit8, oTempStorage);
            else if (outfitID == 9) storedClothes = SerializationService.DeserializeItem(entity.Outfit9, oTempStorage);
            else if (outfitID == 10) storedClothes = SerializationService.DeserializeItem(entity.Outfit10, oTempStorage);

            if (storedClothes == null) throw new Exception("Unable to locate stored clothes.");

            var oCopy = CopyItem(oClothes.Object, oTempStorage.Object, true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftBicep, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftBicep), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Belt, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Belt), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftFoot, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftFoot), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftForearm, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftForearm), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftHand, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftHand), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShin, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShin), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShoulder, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftShoulder), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftThigh, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.LeftThigh), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Neck, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Neck), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Pelvis, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Pelvis), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightBicep, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightBicep), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightFoot, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightFoot), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightForearm, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightForearm), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightHand, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightHand), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Robe, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Robe), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShin, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShin), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShoulder, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightShoulder), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightThigh, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.RightThigh), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Torso, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.ArmorColor, (int)AppearanceArmor.Torso), true);

            NWItem oFinal = (CopyItem(oCopy, oPC.Object, true));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => ActionEquipItem(oFinal.Object, InventorySlot.Chest));

            foreach (var item in oTempStorage.InventoryItems)
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
            var response = GetResponseByID("LoadHelmetPage", responseID);
            var oPC = GetPC();

            if (!CanModifyHelmet())
            {
                oPC.FloatingText("You cannot modify your currently equipped helmet.");
                return;
            }

            var outfitID = (int)response.CustomData;
            var entity = GetPlayerHelmets(GetPC());
            if (entity == null) return;

            NWPlaceable oTempStorage = (GetObjectByTag("OUTFIT_BARREL"));
            var oClothes = oPC.Head;
            NWItem storedClothes = null;
            oClothes.SetLocalString("TEMP_OUTFIT_UUID", oPC.GlobalID.ToString());

            if (outfitID == 1) storedClothes = SerializationService.DeserializeItem(entity.Helmet1, oTempStorage);
            else if (outfitID == 2) storedClothes = SerializationService.DeserializeItem(entity.Helmet2, oTempStorage);
            else if (outfitID == 3) storedClothes = SerializationService.DeserializeItem(entity.Helmet3, oTempStorage);
            else if (outfitID == 4) storedClothes = SerializationService.DeserializeItem(entity.Helmet4, oTempStorage);
            else if (outfitID == 5) storedClothes = SerializationService.DeserializeItem(entity.Helmet5, oTempStorage);
            else if (outfitID == 6) storedClothes = SerializationService.DeserializeItem(entity.Helmet6, oTempStorage);
            else if (outfitID == 7) storedClothes = SerializationService.DeserializeItem(entity.Helmet7, oTempStorage);
            else if (outfitID == 8) storedClothes = SerializationService.DeserializeItem(entity.Helmet8, oTempStorage);
            else if (outfitID == 9) storedClothes = SerializationService.DeserializeItem(entity.Helmet9, oTempStorage);
            else if (outfitID == 10) storedClothes = SerializationService.DeserializeItem(entity.Helmet10, oTempStorage);

            if (storedClothes == null) throw new Exception("Unable to locate stored helmet.");

            var oCopy = CopyItem(oClothes.Object, oTempStorage.Object, true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.SimpleModel, 0, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.SimpleModel, 0), true);

            NWItem oFinal = (CopyItem(oCopy, oPC.Object, true));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => ActionEquipItem(oFinal.Object, InventorySlot.Head));

            foreach (var item in oTempStorage.InventoryItems)
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
            var response = GetResponseByID("LoadWeaponPage", responseID);
            var oPC = GetPC();

            if (!CanModifyWeapon())
            {
                oPC.FloatingText("You cannot modify your currently equipped Weapon.");
                return;
            }

            var outfitID = (int)response.CustomData;
            var entity = GetPlayerWeapons(GetPC());
            if (entity == null) return;

            NWPlaceable oTempStorage = (GetObjectByTag("OUTFIT_BARREL"));
            var oClothes = oPC.RightHand;
            NWItem storedClothes = null;
            oClothes.SetLocalString("TEMP_OUTFIT_UUID", oPC.GlobalID.ToString());

            if (outfitID == 1) storedClothes = SerializationService.DeserializeItem(entity.Weapon1, oTempStorage);
            else if (outfitID == 2) storedClothes = SerializationService.DeserializeItem(entity.Weapon2, oTempStorage);
            else if (outfitID == 3) storedClothes = SerializationService.DeserializeItem(entity.Weapon3, oTempStorage);
            else if (outfitID == 4) storedClothes = SerializationService.DeserializeItem(entity.Weapon4, oTempStorage);
            else if (outfitID == 5) storedClothes = SerializationService.DeserializeItem(entity.Weapon5, oTempStorage);
            else if (outfitID == 6) storedClothes = SerializationService.DeserializeItem(entity.Weapon6, oTempStorage);
            else if (outfitID == 7) storedClothes = SerializationService.DeserializeItem(entity.Weapon7, oTempStorage);
            else if (outfitID == 8) storedClothes = SerializationService.DeserializeItem(entity.Weapon8, oTempStorage);
            else if (outfitID == 9) storedClothes = SerializationService.DeserializeItem(entity.Weapon9, oTempStorage);
            else if (outfitID == 10) storedClothes = SerializationService.DeserializeItem(entity.Weapon10, oTempStorage);

            if (storedClothes == null) throw new Exception("Unable to locate stored Weapon.");

            var oCopy = CopyItem(oClothes.Object, oTempStorage.Object, true);

            var baseItemType = GetBaseItemType(oCopy);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.SimpleModel, 0, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.SimpleModel, 0), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponModel, 0, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.WeaponModel, 0), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponColor, 0, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.WeaponModel, 0), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponModel, 1, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.WeaponModel, 1), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponColor, 1, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.WeaponColor, 1), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponModel, 2, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.WeaponModel, 2), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponColor, 2, (int)GetItemAppearance(storedClothes.Object, ItemAppearanceType.WeaponColor, 2), true);

            NWItem oFinal = (CopyItem(oCopy, oPC.Object, true));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => ActionEquipItem(oFinal.Object, InventorySlot.RightHand));

            foreach (var item in oTempStorage.InventoryItems)
            {
                if (item.GetLocalString("TEMP_OUTFIT_UUID") == oPC.GlobalID.ToString())
                {
                    item.Destroy();
                }
            }

            ShowLoadWeaponOptions();
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
            var entity = GetPlayerOutfits(GetPC()) ?? new PCOutfit();

            ClearPageResponses("SaveOutfitPage");

            var responseText = entity.Outfit1 == null ? ColorTokenService.Red("Save in Slot 1") : ColorTokenService.Green("Save in Slot 1");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit2 == null ? ColorTokenService.Red("Save in Slot 2") : ColorTokenService.Green("Save in Slot 2");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit3 == null ? ColorTokenService.Red("Save in Slot 3") : ColorTokenService.Green("Save in Slot 3");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit4 == null ? ColorTokenService.Red("Save in Slot 4") : ColorTokenService.Green("Save in Slot 4");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit5 == null ? ColorTokenService.Red("Save in Slot 5") : ColorTokenService.Green("Save in Slot 5");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit6 == null ? ColorTokenService.Red("Save in Slot 6") : ColorTokenService.Green("Save in Slot 6");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit7 == null ? ColorTokenService.Red("Save in Slot 7") : ColorTokenService.Green("Save in Slot 7");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit8 == null ? ColorTokenService.Red("Save in Slot 8") : ColorTokenService.Green("Save in Slot 8");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit9 == null ? ColorTokenService.Red("Save in Slot 9") : ColorTokenService.Green("Save in Slot 9");
            AddResponseToPage("SaveOutfitPage", responseText);

            responseText = entity.Outfit10 == null ? ColorTokenService.Red("Save in Slot 10") : ColorTokenService.Green("Save in Slot 10");
            AddResponseToPage("SaveOutfitPage", responseText);
        }
        private void ShowSaveHelmetOptions()
        {
            var entity = GetPlayerHelmets(GetPC()) ?? new PCHelmet();

            ClearPageResponses("SaveHelmetPage");

            var responseText = entity.Helmet1 == null ? ColorTokenService.Red("Save in Slot 1") : ColorTokenService.Green("Save in Slot 1");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet2 == null ? ColorTokenService.Red("Save in Slot 2") : ColorTokenService.Green("Save in Slot 2");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet3 == null ? ColorTokenService.Red("Save in Slot 3") : ColorTokenService.Green("Save in Slot 3");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet4 == null ? ColorTokenService.Red("Save in Slot 4") : ColorTokenService.Green("Save in Slot 4");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet5 == null ? ColorTokenService.Red("Save in Slot 5") : ColorTokenService.Green("Save in Slot 5");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet6 == null ? ColorTokenService.Red("Save in Slot 6") : ColorTokenService.Green("Save in Slot 6");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet7 == null ? ColorTokenService.Red("Save in Slot 7") : ColorTokenService.Green("Save in Slot 7");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet8 == null ? ColorTokenService.Red("Save in Slot 8") : ColorTokenService.Green("Save in Slot 8");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet9 == null ? ColorTokenService.Red("Save in Slot 9") : ColorTokenService.Green("Save in Slot 9");
            AddResponseToPage("SaveHelmetPage", responseText);

            responseText = entity.Helmet10 == null ? ColorTokenService.Red("Save in Slot 10") : ColorTokenService.Green("Save in Slot 10");
            AddResponseToPage("SaveHelmetPage", responseText);
        }
        private void ShowSaveWeaponOptions()
        {
            var entity = GetPlayerWeapons(GetPC()) ?? new PCWeapon();

            ClearPageResponses("SaveWeaponPage");

            var responseText = entity.Weapon1 == null ? ColorTokenService.Red("Save in Slot 1") : ColorTokenService.Green("Save in Slot 1");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon2 == null ? ColorTokenService.Red("Save in Slot 2") : ColorTokenService.Green("Save in Slot 2");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon3 == null ? ColorTokenService.Red("Save in Slot 3") : ColorTokenService.Green("Save in Slot 3");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon4 == null ? ColorTokenService.Red("Save in Slot 4") : ColorTokenService.Green("Save in Slot 4");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon5 == null ? ColorTokenService.Red("Save in Slot 5") : ColorTokenService.Green("Save in Slot 5");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon6 == null ? ColorTokenService.Red("Save in Slot 6") : ColorTokenService.Green("Save in Slot 6");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon7 == null ? ColorTokenService.Red("Save in Slot 7") : ColorTokenService.Green("Save in Slot 7");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon8 == null ? ColorTokenService.Red("Save in Slot 8") : ColorTokenService.Green("Save in Slot 8");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon9 == null ? ColorTokenService.Red("Save in Slot 9") : ColorTokenService.Green("Save in Slot 9");
            AddResponseToPage("SaveWeaponPage", responseText);

            responseText = entity.Weapon10 == null ? ColorTokenService.Red("Save in Slot 10") : ColorTokenService.Green("Save in Slot 10");
            AddResponseToPage("SaveWeaponPage", responseText);
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
            var entity = GetPlayerOutfits(GetPC()) ?? new PCOutfit();
            ClearPageResponses("LoadOutfitPage");

            if (entity.Outfit1 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 1", true, 1);
            if (entity.Outfit2 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 2", true, 2);
            if (entity.Outfit3 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 3", true, 3);
            if (entity.Outfit4 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 4", true, 4);
            if (entity.Outfit5 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 5", true, 5);
            if (entity.Outfit6 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 6", true, 6);
            if (entity.Outfit7 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 7", true, 7);
            if (entity.Outfit8 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 8", true, 8);
            if (entity.Outfit9 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 9", true, 9);
            if (entity.Outfit10 != null)
                AddResponseToPage("LoadOutfitPage", "Load from Slot 10", true, 10);
        }
        private void ShowLoadHelmetOptions()
        {
            var entity = GetPlayerHelmets(GetPC()) ?? new PCHelmet();
            ClearPageResponses("LoadHelmetPage");

            if (entity.Helmet1 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 1", true, 1);
            if (entity.Helmet2 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 2", true, 2);
            if (entity.Helmet3 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 3", true, 3);
            if (entity.Helmet4 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 4", true, 4);
            if (entity.Helmet5 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 5", true, 5);
            if (entity.Helmet6 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 6", true, 6);
            if (entity.Helmet7 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 7", true, 7);
            if (entity.Helmet8 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 8", true, 8);
            if (entity.Helmet9 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 9", true, 9);
            if (entity.Helmet10 != null)
                AddResponseToPage("LoadHelmetPage", "Load from Slot 10", true, 10);
        }
        private void ShowLoadWeaponOptions()
        {
            var entity = GetPlayerWeapons(GetPC()) ?? new PCWeapon();
            ClearPageResponses("LoadWeaponPage");

            if (entity.Weapon1 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 1", true, 1);
            if (entity.Weapon2 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 2", true, 2);
            if (entity.Weapon3 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 3", true, 3);
            if (entity.Weapon4 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 4", true, 4);
            if (entity.Weapon5 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 5", true, 5);
            if (entity.Weapon6 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 6", true, 6);
            if (entity.Weapon7 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 7", true, 7);
            if (entity.Weapon8 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 8", true, 8);
            if (entity.Weapon9 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 9", true, 9);
            if (entity.Weapon10 != null)
                AddResponseToPage("LoadWeaponPage", "Load from Slot 10", true, 10);
        }
        public override void EndDialog()
        {
        }
    }
}
