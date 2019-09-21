using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class Outfit: ConversationBase
    {
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

            DialogPage loadOutfitPage = new DialogPage(
                "Please select an outfit to load."
            );

            DialogPage loadHelmetPage = new DialogPage(
                "Please select a helmet to load."
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("SavePage", savePage);
            dialog.AddPage("SaveOutfitPage", saveOutfitPage);
            dialog.AddPage("SaveHelmetPage", saveHelmetPage);
            dialog.AddPage("LoadPage", loadPage);            
            dialog.AddPage("LoadOutfitPage", loadOutfitPage);
            dialog.AddPage("LoadHelmetPage", loadHelmetPage);
            return dialog;
        }

        public override void Initialize()
        {
            _.SetCommandable(TRUE, GetPC());
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
        private bool CanModifyClothes()
        {
            NWPlayer oPC = GetPC();
            NWItem oClothes = (_.GetItemInSlot(INVENTORY_SLOT_CHEST, oPC.Object));

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
            NWItem oClothes = (_.GetItemInSlot(INVENTORY_SLOT_HEAD, oPC.Object));

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
            NWItem oClothes = (_.GetItemInSlot(INVENTORY_SLOT_CHEST, oPC.Object));

            if (!CanModifyClothes())
            {
                oPC.FloatingText("You cannot save your currently equipped clothes.");
                return;
            }
            
            PCOutfit entity = GetPlayerOutfits(oPC);
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

            string clothesData = SerializationService.Serialize(oClothes);
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
            NWPlayer oPC = GetPC();
            NWItem oClothes = (_.GetItemInSlot(INVENTORY_SLOT_HEAD, oPC.Object));

            if (!CanModifyHelmet())
            {
                oPC.FloatingText("You cannot save your currently equipped helmet.");
                return;
            }

            PCHelmet entity = GetPlayerHelmets(oPC);
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

            string clothesData = SerializationService.Serialize(oClothes);
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
        private void HandleLoadOutfit(int responseID)
        {
            DialogResponse response = GetResponseByID("LoadOutfitPage", responseID);
            NWPlayer oPC = GetPC();

            if (!CanModifyClothes())
            {
                oPC.FloatingText("You cannot modify your currently equipped clothes.");
                return;
            }

            int outfitID = (int)response.CustomData;
            PCOutfit entity = GetPlayerOutfits(GetPC());
            if (entity == null) return;

            NWPlaceable oTempStorage = (_.GetObjectByTag("OUTFIT_BARREL"));
            NWItem oClothes = oPC.Chest;
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

            NWGameObject oCopy = _.CopyItem(oClothes.Object, oTempStorage.Object, TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LBICEP, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LBICEP), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LBICEP, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LBICEP), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_BELT, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_BELT), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_BELT, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_BELT), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LFOOT, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LFOOT), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LFOOT, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LFOOT), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LFOREARM, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LFOREARM), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LFOREARM, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LFOREARM), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LHAND, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LHAND), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LHAND, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LHAND), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LSHIN, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LSHIN), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LSHIN, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LSHIN), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LSHOULDER, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LSHOULDER), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LSHOULDER, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LSHOULDER), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LTHIGH, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_LTHIGH), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LTHIGH, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_LTHIGH), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_NECK, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_NECK), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_NECK, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_NECK), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_PELVIS, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_PELVIS), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_PELVIS, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_PELVIS), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RBICEP, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RBICEP), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RBICEP, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RBICEP), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RFOOT, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RFOOT), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RFOOT, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RFOOT), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RFOREARM, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RFOREARM), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RFOREARM, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RFOREARM), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RHAND, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RHAND), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RHAND, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RHAND), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_ROBE, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_ROBE), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_ROBE, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_ROBE), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RSHIN, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RSHIN), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RSHIN, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RSHIN), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RSHOULDER, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RSHOULDER), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RSHOULDER, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RSHOULDER), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RTHIGH, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_RTHIGH), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RTHIGH, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_RTHIGH), TRUE);

            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_TORSO, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_MODEL, ITEM_APPR_ARMOR_MODEL_TORSO), TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_TORSO, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_ARMOR_COLOR, ITEM_APPR_ARMOR_MODEL_TORSO), TRUE);

            NWItem oFinal = (_.CopyItem(oCopy, oPC.Object, TRUE));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            _.DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => _.ActionEquipItem(oFinal.Object, INVENTORY_SLOT_CHEST));

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

            int outfitID = (int)response.CustomData;
            PCHelmet entity = GetPlayerHelmets(GetPC());
            if (entity == null) return;

            NWPlaceable oTempStorage = (_.GetObjectByTag("OUTFIT_BARREL"));
            NWItem oClothes = oPC.Head;
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

            NWGameObject oCopy = _.CopyItem(oClothes.Object, oTempStorage.Object, TRUE);
            oCopy = _.CopyItemAndModify(oCopy, ITEM_APPR_TYPE_SIMPLE_MODEL, ITEM_APPR_TYPE_SIMPLE_MODEL, _.GetItemAppearance(storedClothes.Object, ITEM_APPR_TYPE_SIMPLE_MODEL, ITEM_APPR_TYPE_SIMPLE_MODEL), TRUE);

            NWItem oFinal = (_.CopyItem(oCopy, oPC.Object, TRUE));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");
            _.DestroyObject(oCopy);
            oClothes.Destroy();
            storedClothes.Destroy();

            oPC.AssignCommand(() => _.ActionEquipItem(oFinal.Object, INVENTORY_SLOT_HEAD));

            foreach (NWItem item in oTempStorage.InventoryItems)
            {
                if (item.GetLocalString("TEMP_OUTFIT_UUID") == oPC.GlobalID.ToString())
                {
                    item.Destroy();
                }
            }

            ShowLoadHelmetOptions();
        }
        private void ShowSaveOptions()
        {            
            ClearPageResponses("SavePage");
            AddResponseToPage("SavePage", "Save Outfit");
            AddResponseToPage("SavePage", "Save Helmet");
        }
        private void ShowSaveOutfitOptions()
        {
            PCOutfit entity = GetPlayerOutfits(GetPC()) ?? new PCOutfit();

            ClearPageResponses("SaveOutfitPage");

            string responseText = entity.Outfit1 == null ? ColorTokenService.Red("Save in Slot 1") : ColorTokenService.Green("Save in Slot 1");
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
            PCHelmet entity = GetPlayerHelmets(GetPC()) ?? new PCHelmet();

            ClearPageResponses("SaveHelmetPage");

            string responseText = entity.Helmet1 == null ? ColorTokenService.Red("Save in Slot 1") : ColorTokenService.Green("Save in Slot 1");
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
        private void ShowLoadOptions()
        {
            ClearPageResponses("LoadPage");
            AddResponseToPage("LoadPage", "Load Outfit");
            AddResponseToPage("LoadPage", "Load Helmet");
        }
        private void ShowLoadOutfitOptions()
        {
            PCOutfit entity = GetPlayerOutfits(GetPC()) ?? new PCOutfit();
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
            PCHelmet entity = GetPlayerHelmets(GetPC()) ?? new PCHelmet();
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
        public override void EndDialog()
        {
        }
    }
}
