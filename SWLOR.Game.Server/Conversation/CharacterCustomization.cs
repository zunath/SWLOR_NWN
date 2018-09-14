using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using System;
using System.Linq;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class CharacterCustomization : ConversationBase
    {
        public CharacterCustomization(
            INWScript script,
            IDialogService dialog)
            : base(script, dialog)
        {
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "You can customize your character here. Once you leave the entry area you can't make any changes to your appearance. Be sure you have it set up like you want!\n\nNote: We limit the available options based on the race you selected at character creation. This is intended to maintain the 'feel' of the setting. If you don't see an option here then it's unfortunately not available for your race.\n\nYou can request that new options be added on our Discord.",
                "Change Skin Color",
                "Change Head",
                "Change Hair Color");

            DialogPage changeSkinColorPage = new DialogPage(
                "Please select a skin color from the list below.");

            DialogPage changeHeadPage = new DialogPage(
                "Please select a head from the list below.");

            DialogPage changeHairColorPage = new DialogPage(
                "Please select a hair color from the list below.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ChangeSkinColorPage", changeSkinColorPage);
            dialog.AddPage("ChangeHeadPage", changeHeadPage);
            dialog.AddPage("ChangeHairColorPage", changeHairColorPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
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
            }
        }

        private void MainResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Change Skin Color
                    LoadSkinColorPage();
                    ChangePage("ChangeSkinColorPage");
                    break;
                case 2: // Change Head
                    LoadHeadPage();
                    ChangePage("ChangeHeadPage");
                    break;
                case 3: // Change Hair Color
                    LoadHairColorPage();
                    ChangePage("ChangeHairColorPage");
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Array.Sort(colorsToUse);
            foreach (var color in colorsToUse)
            {
                AddResponseToPage("ChangeSkinColorPage", "Color #" + color, true, color);
            }

            AddResponseToPage("ChangeSkinColorPage", "Back", true, -1);
        }

        private void ChangeSkinColorResponses(int responseID)
        {
            int colorID = GetResponseByID("ChangeSkinColorPage", responseID).CustomData[string.Empty];

            if (colorID == -1)
            {
                ChangePage("MainPage");
                return;
            }

            _.SetColor(GetPC(), COLOR_CHANNEL_SKIN, colorID);

        }


        private void ChangeHeadResponses(int responseID)
        {
            int headID = GetResponseByID("ChangeHeadPage", responseID).CustomData[string.Empty];

            if (headID == -1)
            {
                ChangePage("MainPage");
                return;
            }

            _.SetCreatureBodyPart(CREATURE_PART_HEAD, headID, GetPC());
        }

        private void LoadHeadPage()
        {
            ClearPageResponses("ChangeHeadPage");

            int[] MaleHumanHeads = { 1, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 45, 46, 47, 100, 102, 110, 116, 126, 129, 130, 131, 132, 134, 135, 136, 137, 138, 140, 141, 142, 145, 146, 150, 152, 153, 160, 161, 164, 165, 166, 167, 171, 176, 177, 182, 183, 184, 186, 190, 191, 194, 195, 196, 197, 198, 199 };
            int[] MaleBothanHeads = { 40, 41, 43, };
            int[] MaleChissHeads = { };
            int[] MaleZabrakHeads = { 103, 63, };
            int[] MaleTwilekHeads = { 115, };
            int[] MaleCyborgHeads = { 156, 168, 181, 187, 74, 88, };
            int[] MaleCatharHeads = { 26, 27, 28, 29, };

            int[] FemaleHumanHeads = { 1, 2, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 18, 19, 20, 22, 23, 24, 25, 27, 28, 30, 31, 32, 33, 34, 35, 36, 37, 39, 40, 42, 44, 45, 46, 48, 49, 100, 101, 102, 103, 104, 105, 106, 107, 108, 111, 112, 113, 114, 116, 117, 118, 121, 123, 124, 125, 127, 130, 132, 134, 136, 137, 138, 140, 141, 164, 167, 168, 171, 172, 173, 174, 175, 177, 178, 180, 181, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 199 };
            int[] FemaleBothanHeads = { 109, 162, };
            int[] FemaleChissHeads = { };
            int[] FemaleZabrakHeads = { 38, 120 };
            int[] FemaleTwilekHeads = { 139, 144, 145, };
            int[] FemaleCyborgHeads = { 41, 109 };
            int[] FemaleCatharHeads = { 13, 14, 16, 166, 198 };

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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Array.Sort(headsToUse);
            foreach (var head in headsToUse)
            {
                AddResponseToPage("ChangeHeadPage", "Head #" + head, true, head);
            }

            AddResponseToPage("ChangeHeadPage", "Back", true, -1);
        }

        private void LoadHairColorPage()
        {
            ClearPageResponses("ChangeHairColorPage");

            // These IDs are pulled from the SkinColors.jpg file found in the ServerFiles/Reference folder.

            int[] HumanHairColors = { 0, 1, 2, 3, 4, 5, 9, 10, 11, 12, 13, 14, 15, };
            int[] BothanHairColors = { 2, 3, };
            int[] ChissHairColors = { 23, 51, 63 };
            int[] ZabrakHairColors = { 0 };
            int[] TwilekHairColors = { 0 };
            int[] CyborgHairColors = { 16, 17, 18, 19, 20, 21 };
            int[] CatharHairColors = { 0, 1, 2, 3, 4, 5, 6, 7, 116, 117 };


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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Array.Sort(colorsToUse);
            foreach (var color in colorsToUse)
            {
                AddResponseToPage("ChangeHairColorPage", "Color #" + color, true, color);
            }

            AddResponseToPage("ChangeHairColorPage", "Back", true, -1);


        }

        private void ChangeHairColorResponses(int responseID)
        {

            int colorID = GetResponseByID("ChangeHairColorPage", responseID).CustomData[string.Empty];

            if (colorID == -1)
            {
                ChangePage("MainPage");
                return;
            }

            _.SetColor(GetPC(), COLOR_CHANNEL_HAIR, colorID);

        }

        public override void EndDialog()
        {
        }
    }
}
