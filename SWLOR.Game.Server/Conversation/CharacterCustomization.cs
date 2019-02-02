﻿using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.NWNX.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class CharacterCustomization : ConversationBase
    {
        private class Model
        {
            public int AssociationID { get; set; }
            public int BodyPartID { get; set; }
            public int[] Parts { get; set; }
            public string PartName { get; set; }
            public int TattooChannel { get; set; }
        }

        private readonly IColorTokenService _color;
        private readonly IDataService _data;

        public CharacterCustomization(
            INWScript script,
            IDialogService dialog,
            IColorTokenService color,
            IDataService data)
            : base(script, dialog)
        {
            _color = color;
            _data = data;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "You can customize your character here. Once you leave the entry area you can't make any changes to your appearance or association. Be sure you have it set up like you want!\n\nNote: We limit the available options based on the race you selected at character creation. This is intended to maintain the 'feel' of the setting. If you don't see an option here then it's unfortunately not available for your race.\n\nYou can request that new options be added on our Discord.",
                "Change Association",
                "Change Skin Color",
                "Change Head",
                "Change Hair Color",
                "Change Body Parts",
                "Change Tattoo Color");

            DialogPage changeAssociationPage = new DialogPage(
                "Please select an association from the list below.",
                "Jedi Order",
                "Mandalorian",
                "Sith Empire",
                "Smugglers",
                "Unaligned",
                "Hutt Cartel",
                "Republic",
                "Czerka",
                "Sith Order");

            DialogPage confirmAssociationPage = new DialogPage(
                "",
                "Select this association");

            DialogPage changeSkinColorPage = new DialogPage(
                "Please select a skin color from the list below.");

            DialogPage changeHeadPage = new DialogPage(
                "Please select a head from the list below.");

            DialogPage changeHairColorPage = new DialogPage(
                "Please select a hair color from the list below.");

            DialogPage changeBodyPartsPage = new DialogPage(
                "Please select a body part from the list below.",
                "Torso",
                "Pelvis",
                "Right Bicep",
                "Right Forearm",
                "Right Hand",
                "Right Thigh",
                "Right Shin",
                "Left Bicep",
                "Left Forearm",
                "Left Hand",
                "Left Thigh",
                "Left Shin");

            DialogPage changeTattoosPage = new DialogPage(
                "Please select a tattoo option from the list below.",
                "Tattoo Color 1",
                "Tattoo Color 2");

            DialogPage changeTattooColorPage = new DialogPage(
                "Please select a tattoo color from the list below.");

            DialogPage editPartPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ChangeAssociationPage", changeAssociationPage);
            dialog.AddPage("ConfirmAssociationPage", confirmAssociationPage);
            dialog.AddPage("ChangeSkinColorPage", changeSkinColorPage);
            dialog.AddPage("ChangeHeadPage", changeHeadPage);
            dialog.AddPage("ChangeHairColorPage", changeHairColorPage);
            dialog.AddPage("ChangeBodyPartsPage", changeBodyPartsPage);
            dialog.AddPage("ChangeTattooPage", changeTattoosPage);
            dialog.AddPage("ChangeTattooColorPage", changeTattooColorPage);
            dialog.AddPage("EditPartPage", editPartPage);
            return dialog;
        }

        public override void Initialize()
        {
            CustomRaceType race = (CustomRaceType)GetPC().RacialType;
            string hairText = "Hair";

            if (race == CustomRaceType.Trandoshan)
            {
                hairText = "Eyes";
                SetResponseVisible("MainPage", 5, false);
            }

            SetResponseText("MainPage", 4, "Change " + hairText);

            // Skin color can't change for Wookiees. Disable the option.
            if (race == CustomRaceType.Wookiee)
            {
                SetResponseVisible("MainPage", 2, false);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "ChangeAssociationPage":
                    ChangeAssociationResponses(responseID);
                    break;
                case "ConfirmAssociationPage":
                    ConfirmAssociationResponses(responseID);
                    break;
                case "ChangeSkinColorPage":
                    ChangeSkinColorResponses(responseID);
                    break;
                case "ChangeHeadPage":
                    ChangeHeadResponses(responseID);
                    break;
                case "ChangeHairColorPage":
                    ChangeHairColorResponses(responseID);
                    break;
                case "ChangeBodyPartsPage":
                    ChangeBodyPartResponses(responseID);
                    break;
                case "ChangeTattooPage":
                    ChangeTattooResponses(responseID);
                    break;
                case "ChangeTattooColorPage":
                    ChangeTattooColorResponses(responseID);
                    break;
                case "EditPartPage":
                    EditPartResponses(responseID);
                    break;

            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            switch (beforeMovePage)
            {
                case "ChangeAssociationPage":
                    break;
            }
        }

        private void MainResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Change Association
                    ChangePage("ChangeAssociationPage");
                    break;
                case 2: // Change Skin Color
                    LoadSkinColorPage();
                    ChangePage("ChangeSkinColorPage");
                    break;
                case 3: // Change Head
                    LoadHeadPage();
                    ChangePage("ChangeHeadPage");
                    break;
                case 4: // Change Hair Color
                    LoadHairColorPage();
                    ChangePage("ChangeHairColorPage");
                    break;
                case 5: // Change Body Part
                    ChangePage("ChangeBodyPartsPage");
                    break;
                case 6: // Change Tattoo
                    ChangePage("ChangeTattooPage");
                    break;
            }
        }
        
        private void LoadSkinColorPage()
        {
            ClearPageResponses("ChangeSkinColorPage");

            // These IDs are pulled from the SkinColors.jpg file found in the ServerFiles/Reference folder.
            int[] HumanSkinColors = { 0, 1, 2, 3, 4, 5, 6, 7, 10, 11 };
            int[] BothanSkinColors = { 6, 7, 10, 11, 117, 118, 119, 130, 131 };
            int[] ChissSkinColors = { 48, 49, 50, 51, 22, 136, 137, 138, 139, 140, 141, 142, 143 };
            int[] ZabrakSkinColors = { 0, 1, 2, 3, 88, 89, 90, 91, 102, 103 };
            int[] TwilekSkinColors = { 0, 1, 2, 3, 4, 5, 44, 45, 46, 48, 49, 50, 51, 52, 53, 54, 55, 88, 89, 90, 91, 96, 97, 98, 99, 140, 141, 142, 143 };
            int[] CyborgSkinColors = { 0, 1, 2, 3, 4, 5, 6, 7, 10, 11, 17, 18, 19 };
            int[] CatharSkinColors = { 6, 7, 10, 11, 117, 118, 119, 130, 131 };
            int[] TrandoshanSkinColors = { 4, 5, 6, 18, 19, 34, 35, 36, 38, 39, 172 };

            CustomRaceType race = (CustomRaceType)GetPC().RacialType;
            int[] colorsToUse;

            switch (race)
            {
                case CustomRaceType.Human:
                    colorsToUse = HumanSkinColors;
                    break;
                case CustomRaceType.Bothan:
                    colorsToUse = BothanSkinColors;
                    break;
                case CustomRaceType.Chiss:
                    colorsToUse = ChissSkinColors;
                    break;
                case CustomRaceType.Zabrak:
                    colorsToUse = ZabrakSkinColors;
                    break;
                case CustomRaceType.Twilek:
                    colorsToUse = TwilekSkinColors;
                    break;
                case CustomRaceType.Cyborg:
                    colorsToUse = CyborgSkinColors;
                    break;
                case CustomRaceType.Cathar:
                    colorsToUse = CatharSkinColors;
                    break;
                case CustomRaceType.Trandoshan:
                    colorsToUse = TrandoshanSkinColors;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Array.Sort(colorsToUse);
            foreach (var color in colorsToUse)
            {
                AddResponseToPage("ChangeSkinColorPage", "Color #" + color, true, color);
            }
        }

        private void ChangeAssociationResponses(int responseID)
        {
            string header;
            switch (responseID)
            {
                case 1: // Jedi Order
                    header = _color.Green("Jedi Order\n\n");
                    header += "The Guardians of peace and justice in the Republic, the Jedi Order is a large group of Light Side Force users led by the Jedi Council. Wishing to uphold the ideals of peace and bring balance to the Force they are stalwart defenders of the Galaxy. Association to the Jedi Order is held by Jedi of all positions as well as their closest allies.";
                    break;
                case 2: // Mandalorian
                    header = _color.Green("Mandalorian\n\n");
                    header += "Mandalorians are one of the supreme fighting cultures on the Galaxy. Ruled by the Mand’alor of that period, they are a fractured group consisting of Clans grouped together under a House by the racially inclusive population. Though there is currently no Mand’alor to lead them their culture lives on through each member. Association with Mandalorians is held by members or hopefuls, Military figures, and Mercenaries.";
                    break;
                case 3: // Sith Empire
                    header = _color.Green("Sith Empire\n\n");
                    header += "Founded by exiled members of the Jedi Order, the Sith Order consists of those who utilize a focus on the Dark Side of the Force. Unlike the Jedi, the Sith Order wishes to impose their power and will onto others by show of force. Seeing themselves as the true powers of the Galaxy, they seek to rule it above all things. Association to the Sith Order is held by Sith of all positions as well as some of their underlings and aspiring hopefuls.";
                    break;
                case 4: // Smugglers
                    header = _color.Green("Smugglers\n\n");
                    header += "Smugglers, though not a specific organization, are prevalent throughout the Galaxy. Though they can come from anywhere they generally look out for themselves, selling their services to any willing to pay and almost always for the largest coin. With monetary gain as their primary motivator Smugglers do not hurt for work. Association with the likes of Smugglers is held by any willing to move products or transition supplies from one set of hands to another.";
                    break;
                case 5: // Unaligned
                    header = _color.Green("Unaligned\n\n");
                    header += "The Unaligned are any who seek out a future for themselves without the strings of being associated with one of the organizations on the main galactic stage. Looking out for their own interests and striving for a better future for themselves, being unaligned is most off the frequent outlook for those in the farther reaches of the galaxy.";
                    break;
                case 6: // Hutt Cartel
                    header = _color.Green("Hutt Cartel\n\n");
                    header += "One of the largest criminal organizations both in reach and numbers, the Hutt Cartel is a powerful group of Hutts that place their slimy tails in everything they can get their hands on. Dealing in everything from weapons to drugs and assassinations to slavery, the Hutt Cartel knows no bounds to the type of crime they will deal in so long as it is profitable. Association with the Hutt Cartel is held by the likes of assassins, thieves, and gang members, as well as those that aspire to cheat their way through life.";
                    break;
                case 7: // Republic
                    header = _color.Green("Republic\n\n");
                    header += "For a little over 20 thousand years the Republic has been the ruling force throughout the Galaxy. It’s ruling body is comprised of Senators representing planets and systems. The Jedi Order works with the Republic as their primary peacekeepers with a large Galactic Republic Military Force consisting of individuals from all walks of life. Association to the Republic is held by Military and Political figures throughout the Galaxy, as well as Jedi seeking to hold back the tides of chaos that wish to tear it down.";
                    break;
                case 8: // Czerka
                    header = _color.Green("Czerka Corporation\n\n");
                    header += "The Czerka Corporation is a galaxy spanning organization that works to make a profit no matter the cost. Though previously unaligned it has recent allied itself with the Sith Empire for exclusive trade benefits throughout their conquered worlds. Dealing in mining resources, weapons, and droids, Czerka are some of the galaxies current forerunners in technology and trade. Association with Czerka can be held by nearly anyone with the ability and drive to create and trade for a profit.";
                    break;
                case 9: // Sith Order
                    header = _color.Green("Sith Order\n\n");
                    header += "Founded by exiled members of the Jedi Order, the Sith Order consists of those who utilize a focus on the Dark Side of the Force. Unlike the Jedi, the Sith Order wishes to impose their power and will onto others by show of force. Seeing themselves as the true powers of the Galaxy, they seek to rule it above all things. Association to the Sith Order is held by Sith of all positions as well as some of their underlings and aspiring hopefuls.";
                    break;
                default: return;
            }

            var model = GetDialogCustomData<Model>();
            model.AssociationID = responseID;
            SetPageHeader("ConfirmAssociationPage", header);
            ChangePage("ConfirmAssociationPage");
        }

        private void ConfirmAssociationResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Select this association
                    ApplyAssociationAlignment();
                    ClearNavigationStack();
                    ChangePage("MainPage", false);
                    break;
            }
        }

        private void ApplyAssociationAlignment()
        {
            var model = GetDialogCustomData<Model>();
            int association = model.AssociationID;
            var player = GetPC();
            var dbPlayer = _data.Single<Player>(x => x.ID == player.GlobalID);

            dbPlayer.AssociationID = association;
            _data.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }

        private void ChangeSkinColorResponses(int responseID)
        {
            int colorID = (int)GetResponseByID("ChangeSkinColorPage", responseID).CustomData;
            _.SetColor(GetPC(), COLOR_CHANNEL_SKIN, colorID);

        }


        private void ChangeHeadResponses(int responseID)
        {
            int headID = (int)GetResponseByID("ChangeHeadPage", responseID).CustomData;
            _.SetCreatureBodyPart(CREATURE_PART_HEAD, headID, GetPC());
        }

        private void LoadHeadPage()
        {
            ClearPageResponses("ChangeHeadPage");

            int[] MaleHumanHeads = { 1, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 45, 46, 47, 100, 102, 110, 116, 126, 129, 130, 131, 132, 134, 135, 136, 137, 138, 140, 141, 142, 145, 146, 150, 152, 153, 160, 161, 164, 165, 166, 167, 171, 176, 177, 182, 183, 184, 186, 190, 191, 194, 195, 196, 197, 198, 199, 231 };
            int[] MaleBothanHeads = { 40, 41, 43, };
            int[] MaleChissHeads = { };
            int[] MaleZabrakHeads = { 103, 63, };
            int[] MaleTwilekHeads = { 115, };
            int[] MaleCyborgHeads = { 156, 168, 181, 187, 74, 88, };
            int[] MaleCatharHeads = { 26, 27, 28, 29, };
            int[] MaleTrandoshanHeads = { 2, 101, 111, 123, 124, 125, 143, 162 };
            int[] MaleWookieeHeads = {119, 192, 193};

            int[] FemaleHumanHeads = { 1, 2, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 18, 19, 20, 22, 23, 24, 25, 27, 28, 30, 31, 32, 33, 34, 35, 36, 37, 39, 40, 42, 44, 45, 46, 48, 49, 100, 101, 102, 103, 104, 105, 106, 107, 108, 111, 112, 113, 114, 116, 117, 118, 121, 123, 124, 125, 127, 130, 132, 134, 136, 137, 138, 140, 141, 164, 167, 168, 171, 172, 173, 174, 175, 177, 178, 180, 181, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 199 };
            int[] FemaleBothanHeads = { 109, 162, };
            int[] FemaleChissHeads = { };
            int[] FemaleZabrakHeads = { 38, 120 };
            int[] FemaleTwilekHeads = { 139, 144, 145, };
            int[] FemaleCyborgHeads = { 41, 109 };
            int[] FemaleCatharHeads = { 13, 14 };
            int[] FemaleTrandoshanHeads = { 135, 150, 157 };
            int[] FemaleWookieeHeads = {110, 185, 186, 192, 193, 195};

            CustomRaceType race = (CustomRaceType)GetPC().RacialType;
            int gender = GetPC().Gender;
            int[] headsToUse;
            
            switch (race)
            {
                case CustomRaceType.Human:
                    headsToUse = gender == GENDER_MALE ? MaleHumanHeads : FemaleHumanHeads;
                    break;
                case CustomRaceType.Bothan:
                    headsToUse = gender == GENDER_MALE ? MaleBothanHeads : FemaleBothanHeads;
                    break;
                case CustomRaceType.Chiss:
                    headsToUse = gender == GENDER_MALE ?
                        MaleChissHeads.Concat(MaleHumanHeads).ToArray() :
                        FemaleChissHeads.Concat(FemaleHumanHeads).ToArray();
                    break;
                case CustomRaceType.Zabrak:
                    headsToUse = gender == GENDER_MALE ? MaleZabrakHeads : FemaleZabrakHeads;
                    break;
                case CustomRaceType.Twilek:
                    headsToUse = gender == GENDER_MALE ? MaleTwilekHeads : FemaleTwilekHeads;
                    break;
                case CustomRaceType.Cyborg:
                    headsToUse = gender == GENDER_MALE ?
                        MaleCyborgHeads.Concat(MaleHumanHeads).ToArray() :
                        FemaleCyborgHeads.Concat(FemaleHumanHeads).ToArray();
                    break;
                case CustomRaceType.Cathar:
                    headsToUse = gender == GENDER_MALE ? MaleCatharHeads : FemaleCatharHeads;
                    break;
                case CustomRaceType.Trandoshan:
                    headsToUse = gender == GENDER_MALE ? MaleTrandoshanHeads : FemaleTrandoshanHeads;
                    break;
                case CustomRaceType.Wookiee:
                    headsToUse = gender == GENDER_MALE ? MaleWookieeHeads : FemaleWookieeHeads;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Array.Sort(headsToUse);
            foreach (var head in headsToUse)
            {
                AddResponseToPage("ChangeHeadPage", "Head #" + head, true, head);
            }
        }

        private void LoadHairColorPage()
        {
            ClearPageResponses("ChangeHairColorPage");

            // These IDs are pulled from the HairColors.jpg file found in the ServerFiles/Reference folder.
            int[] HumanHairColors = { 0, 1, 2, 3, 4, 5, 9, 10, 11, 12, 13, 14, 15, 23, 167};
            int[] BothanHairColors = { 2, 3, };
            int[] ChissHairColors = { 23, 51, 63 };
            int[] ZabrakHairColors = { 0 };
            int[] TwilekHairColors = { 0 };
            int[] CyborgHairColors = { 16, 17, 18, 19, 20, 21 };
            int[] CatharHairColors = { 0, 1, 2, 3, 4, 5, 6, 7, 116, 117 };
            int[] TrandoshanEyeColors = { }; // All
            int[] WookieeHairColors = {0, 1, 2, 3, 14, 49, 51};

            CustomRaceType race = (CustomRaceType)GetPC().RacialType;
            int[] colorsToUse;

            switch (race)
            {
                case CustomRaceType.Human:
                    colorsToUse = HumanHairColors;
                    break;
                case CustomRaceType.Bothan:
                    colorsToUse = BothanHairColors;
                    break;
                case CustomRaceType.Chiss:
                    colorsToUse = ChissHairColors;
                    break;
                case CustomRaceType.Zabrak:
                    colorsToUse = ZabrakHairColors;
                    break;
                case CustomRaceType.Twilek:
                    colorsToUse = TwilekHairColors;
                    break;
                case CustomRaceType.Cyborg:
                    colorsToUse = CyborgHairColors.Concat(HumanHairColors).ToArray();
                    break;
                case CustomRaceType.Cathar:
                    colorsToUse = CatharHairColors;
                    break;
                case CustomRaceType.Trandoshan:
                    colorsToUse = TrandoshanEyeColors;
                    break;
                case CustomRaceType.Wookiee:
                    colorsToUse = WookieeHairColors;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // If none specified, display all.
            if (colorsToUse.Length <= 0)
            {
                for (int x = 0; x <= 175; x++)
                {
                    AddResponseToPage("ChangeHairColorPage", "Color #" + x, true, x);
                }
            }
            else
            {
                Array.Sort(colorsToUse);
                foreach (var color in colorsToUse)
                {
                    AddResponseToPage("ChangeHairColorPage", "Color #" + color, true, color);
                }
            }
        }

        private void ChangeHairColorResponses(int responseID)
        {
            int colorID = (int)GetResponseByID("ChangeHairColorPage", responseID).CustomData;
            
            _.SetColor(GetPC(), COLOR_CHANNEL_HAIR, colorID);
        }
        
        private void ChangeBodyPartResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            CustomRaceType race = (CustomRaceType)GetPC().RacialType;

            // Note: The following part IDs are found in the "parts_*.2da" files.
            // Don't use the ID number listed in the toolset when selecting parts to make available.
            // The ID in the toolset is a DIFFERENT index and doesn't correlate to the 2da ID number.
            switch (responseID)
            {
                case 1: // Torso
                    model.BodyPartID = CREATURE_PART_TORSO;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] {208, 209} : 
                        new[] { 1, 2, 166};
                    model.PartName = "Torso";
                    break;
                case 2: // Pelvis
                    model.BodyPartID = CREATURE_PART_PELVIS;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208, 209 } : 
                        new[] { 1, 2, 11, 158 };
                    model.PartName = "Pelvis";
                    break;
                case 3: // Right Bicep
                    model.BodyPartID = CREATURE_PART_RIGHT_BICEP;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2 };
                    model.PartName = "Right Bicep";
                    break;
                case 4: // Right Forearm
                    model.BodyPartID = CREATURE_PART_RIGHT_FOREARM;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2, 152 };
                    model.PartName = "Right Forearm";
                    break;
                case 5: // Right Hand
                    model.BodyPartID = CREATURE_PART_RIGHT_HAND;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2, 5, 6, 63, 100, 110, 113, 121, 151, };
                    model.PartName = "Right Hand";
                    break;
                case 6: // Right Thigh
                    model.BodyPartID = CREATURE_PART_RIGHT_THIGH;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2, 154 };
                    model.PartName = "Right Thigh";
                    break;
                case 7: // Right Shin
                    model.BodyPartID = CREATURE_PART_RIGHT_SHIN;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2 };
                    model.PartName = "Right Shin";
                    break;
                case 8: // Left Bicep
                    model.BodyPartID = CREATURE_PART_LEFT_BICEP;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2 };
                    model.PartName = "Left Bicep";
                    break;
                case 9: // Left Forearm
                    model.BodyPartID = CREATURE_PART_LEFT_FOREARM;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2, 152 };
                    model.PartName = "Left Forearm";
                    break;
                case 10: // Left Hand
                    model.BodyPartID = CREATURE_PART_LEFT_HAND;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2, 5, 6, 63, 100, 110, 113, 121, 151, };
                    model.PartName = "Left Hand";
                    break;
                case 11: // Left Thigh
                    model.BodyPartID = CREATURE_PART_LEFT_THIGH;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2, 154 };
                    model.PartName = "Left Thigh";
                    break;
                case 12: // Left Shin
                    model.BodyPartID = CREATURE_PART_LEFT_SHIN;
                    model.Parts = race == CustomRaceType.Wookiee ? 
                        new[] { 208 } : 
                        new[] { 1, 2 };
                    model.PartName = "Left Shin";
                    break;
            }

            LoadEditPartPage();
            ChangePage("EditPartPage");
        }

        private void ChangeTattooResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            switch (responseID)
            {
                case 1:
                    model.TattooChannel = COLOR_CHANNEL_TATTOO_1;
                    LoadChangeTattooColorPage();
                    ChangePage("ChangeTattooColorPage");
                    break;
                case 2:
                    model.TattooChannel = COLOR_CHANNEL_TATTOO_2;
                    LoadChangeTattooColorPage();
                    ChangePage("ChangeTattooColorPage");
                    break;
            }
        }

        private void LoadChangeTattooColorPage()
        {
            ClearPageResponses("ChangeTattooColorPage");

            for (int x = 0; x <= 175; x++)
            {
                AddResponseToPage("ChangeTattooColorPage", "Color #" + x, true, x);
            }
        }

        private void ChangeTattooColorResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            var response = GetResponseByID("ChangeTattooColorPage", responseID);
            int colorID = (int)response.CustomData;
            int colorChannel = model.TattooChannel;

            _.SetColor(GetPC(), colorChannel, colorID);
        }

        private void LoadEditPartPage()
        {
            ClearPageResponses("EditPartPage");

            var model = GetDialogCustomData<Model>();
            foreach (var modelID in model.Parts)
            {
                AddResponseToPage("EditPartPage", model.PartName + " #" + modelID, true, modelID);
            }
            
            string header = _color.Green("Body Part: ") + model.PartName + "\n\n";
            header += "You may need to unequip any clothes or armor you are wearing to see changes made to your body parts.\n\nPlease be aware that many armors override your selection here. This is a limitation in NWN that we can't work around.";

            SetPageHeader("EditPartPage", header);
        }

        private void EditPartResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            var response = GetResponseByID("EditPartPage", responseID);
            int modelID = (int)response.CustomData;
            _.SetCreatureBodyPart(model.BodyPartID, modelID, GetPC());
        }

        public override void EndDialog()
        {
        }
    }
}
