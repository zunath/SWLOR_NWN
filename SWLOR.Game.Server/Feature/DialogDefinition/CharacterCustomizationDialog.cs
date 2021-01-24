using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class CharacterCustomizationDialog: DialogBase
    {
        private class Model
        {
            public CreaturePart BodyPartID { get; set; }
            public int[] Parts { get; set; }
            public string PartName { get; set; }
            public ColorChannel TattooChannel { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string SkinColorPageId = "SKIN_COLOR_PAGE";
        private const string HeadPageId = "HEAD_PAGE";
        private const string HairColorPageId = "HAIR_COLOR_PAGE";
        private const string BodyPartTypePageId = "BODY_PART_TYPE_PAGE";
        private const string BodyPartPageId = "BODY_PART_PAGE";
        private const string TattooTypePageId = "TATTOO_TYPE_PAGE";
        private const string TattooColorPageId = "TATTOO_COLOR_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(SkinColorPageId, SkinColorPageInit)
                .AddPage(HeadPageId, HeadPageInit)
                .AddPage(HairColorPageId, HairColorPageInit)
                .AddPage(BodyPartTypePageId, BodyPartTypePageInit)
                .AddPage(BodyPartPageId, BodyPartPageInit)
                .AddPage(TattooTypePageId, TattooTypePageInit)
                .AddPage(TattooColorPageId, TattooColorPageInit);


            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var racialType = GetRacialType(player);

            page.Header = "You can customize your character here. Once you leave the entry area you can't make any changes to your appearance or association. Be sure you have it set up like you want!\n\nNote: We limit the available options based on the race you selected at character creation. This is intended to maintain the 'feel' of the setting. If you don't see an option here then it's unfortunately not available for your race.\n\nYou can request that new options be added on our Discord.";

            if (racialType != RacialType.Wookiee)
            {
                page.AddResponse("Change Skin Color", () =>
                {
                    ChangePage(SkinColorPageId);
                });
            }

            page.AddResponse("Change Head", () =>
            {
                ChangePage(HeadPageId);
            });

            var changeHairColorText = (racialType == RacialType.Trandoshan || racialType == RacialType.MonCalamari)
                ? "Change Eye Color"
                : "Change Hair Color";
            page.AddResponse(changeHairColorText, () =>
            {
                ChangePage(HairColorPageId);
            });

            page.AddResponse("Change Body Parts", () =>
            {
                ChangePage(BodyPartTypePageId);
            });

            page.AddResponse("Change Tattoo Color", () =>
            {
                ChangePage(TattooTypePageId);
            });
        }

        private void SkinColorPageInit(DialogPage page)
        {
            page.Header = "Please select a skin color from the list below.";

            // These IDs are pulled from the SkinColors.jpg file found in the ServerFiles/Reference folder.
            int[] HumanSkinColors = { 0, 1, 2, 3, 4, 5, 6, 7, 10, 11 };
            int[] BothanSkinColors = { 6, 7, 10, 11, 117, 118, 119, 130, 131 };
            int[] ChissSkinColors = { 48, 49, 50, 51, 22, 136, 137, 138, 139, 140, 141, 142, 143 };
            int[] ZabrakSkinColors = { 0, 1, 2, 3, 88, 89, 90, 91, 102, 103 };
            int[] TwilekSkinColors = { 0, 1, 2, 3, 4, 5, 44, 45, 46, 48, 49, 50, 51, 52, 53, 54, 55, 88, 89, 90, 91, 96, 97, 98, 99, 140, 141, 142, 143 };
            int[] CyborgSkinColors = { 0, 1, 2, 3, 4, 5, 6, 7, 10, 11, 17, 18, 19 };
            int[] CatharSkinColors = { 6, 7, 10, 11, 117, 118, 119, 130, 131 };
            int[] TrandoshanSkinColors = { 4, 5, 6, 18, 19, 34, 35, 36, 38, 39, 172 };
            int[] MirialanSkinColors = { 33, 34, 36, 37, 38, 52, 53, 54, 55, 93, 94 };
            int[] EchaniSkinColors = { 16, 20, 40, 164 };
            int[] MonCalamariSkinColors =
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 60, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
                80, 81, 82, 83, 84, 85, 86, 87, 94, 95, 97, 98, 99, 105, 106, 107, 109, 110, 111, 113, 114, 115, 117, 118, 119, 120, 121, 122, 124, 125, 126, 127, 129, 130, 131, 137, 138, 139, 141, 142, 143, 145, 146, 147, 148, 149, 151, 153, 155, 157, 158, 159, 160, 161, 162, 165, 171, 172, 175
            };
            int[] UgnaughtSkinColors = { 0, 1, 2, 3, 4, 5, 10, 12, 13, 14, 116, 117 };

            var player = GetPC();
            var race = GetRacialType(player);
            int[] colorsToUse;

            switch (race)
            {
                case RacialType.Human:
                    colorsToUse = HumanSkinColors;
                    break;
                case RacialType.Bothan:
                    colorsToUse = BothanSkinColors;
                    break;
                case RacialType.Chiss:
                    colorsToUse = ChissSkinColors;
                    break;
                case RacialType.Zabrak:
                    colorsToUse = ZabrakSkinColors;
                    break;
                case RacialType.Twilek:
                    colorsToUse = TwilekSkinColors;
                    break;
                case RacialType.Mirialan:
                    colorsToUse = MirialanSkinColors;
                    break;
                case RacialType.Echani:
                    colorsToUse = EchaniSkinColors;
                    break;
                case RacialType.Cyborg:
                    colorsToUse = CyborgSkinColors;
                    break;
                case RacialType.Cathar:
                    colorsToUse = CatharSkinColors;
                    break;
                case RacialType.Trandoshan:
                    colorsToUse = TrandoshanSkinColors;
                    break;
                case RacialType.MonCalamari:
                    colorsToUse = MonCalamariSkinColors;
                    break;
                case RacialType.Ugnaught:
                    colorsToUse = UgnaughtSkinColors;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Array.Sort(colorsToUse);
            foreach (var color in colorsToUse)
            {
                page.AddResponse($"Color #{color}", () =>
                {
                    SetColor(player, ColorChannel.Skin, color);
                });
            }

        }

        private void HeadPageInit(DialogPage page)
        {
            page.Header = "Please select a head from the list below.";

            int[] MaleHumanHeads = { 1, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 45, 46, 47, 100, 102, 110, 116, 121, 126, 129, 130, 131, 132, 134, 135, 136, 137, 138, 140, 141, 142, 145, 146, 150, 152, 153, 160, 161, 164, 165, 166, 167, 171, 176, 177, 182, 183, 184, 186, 190, 191, 194, 195, 196, 197, 198, 199, 231 };
            int[] MaleBothanHeads = { 40, 41, 43, };
            int[] MaleChissHeads = { };
            int[] MaleZabrakHeads = { 103, };
            int[] MaleTwilekHeads = { 115, };
            int[] MaleMirialanHeads = { };
            int[] MaleEchaniHeads = { };
            int[] MaleCyborgHeads = { 74, 88, 156, 168, 181, 187 };
            int[] MaleCatharHeads = { 26, 27, 28, 29, };
            int[] MaleTrandoshanHeads = { 2, 101, 111, 123, 124, 125, 143, 162 };
            int[] MaleWookieeHeads = { 119, 192, 193 };
            int[] MaleMonCalamariHeads = { 6, 44, 48, 49, 104, 105, 106, 107, 108, 112, 113, 114, 117, 119, 120, 127 };
            int[] MaleUgnaughtHeads = { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110 };

            int[] FemaleHumanHeads = { 1, 2, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 18, 19, 20, 22, 23, 25, 27, 28, 30, 31, 32, 33, 34, 35, 36, 37, 39, 40, 42, 44, 45, 46, 48, 49, 100, 101, 102, 103, 104, 105, 106, 107, 108, 111, 112, 113, 114, 116, 117, 118, 121, 123, 124, 125, 127, 130, 132, 134, 136, 137, 138, 140, 141, 164, 167, 168, 171, 172, 173, 174, 175, 177, 178, 180, 181, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 199 };
            int[] FemaleBothanHeads = { 109, 162, };
            int[] FemaleChissHeads = { };
            int[] FemaleZabrakHeads = { 38, 120 };
            int[] FemaleTwilekHeads = { 139, 144, 145, };
            int[] FemaleMirialanHeads = { };
            int[] FemaleEchaniHeads = { };
            int[] FemaleCyborgHeads = { 41, 109 };
            int[] FemaleCatharHeads = { 13, 14 };
            int[] FemaleTrandoshanHeads = { 24, 126, 128, 135, 150, 157 };
            int[] FemaleWookieeHeads = { 110, 185, 186, 192, 193, 195 };
            int[] FemaleMonCalamariHeads = { 3, 6, 16, 17, 21, 26, 29, 41, 43, 47, 109, 110, 115, 119, 122 };
            int[] FemaleUgnaughtHeads = { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110 };

            var player = GetPC();
            var gender = GetGender(player);
            var race = GetRacialType(player);
            int[] headsToUse;

            switch (race)
            {
                case RacialType.Human:
                    headsToUse = gender == Gender.Male ? MaleHumanHeads : FemaleHumanHeads;
                    break;
                case RacialType.Bothan:
                    headsToUse = gender == Gender.Male ? MaleBothanHeads : FemaleBothanHeads;
                    break;
                case RacialType.Chiss:
                    headsToUse = gender == Gender.Male ?
                        MaleChissHeads.Concat(MaleHumanHeads).ToArray() :
                        FemaleChissHeads.Concat(FemaleHumanHeads).ToArray();
                    break;
                case RacialType.Zabrak:
                    headsToUse = gender == Gender.Male ? MaleZabrakHeads : FemaleZabrakHeads;
                    break;
                case RacialType.Twilek:
                    headsToUse = gender == Gender.Male ? MaleTwilekHeads : FemaleTwilekHeads;
                    break;
                case RacialType.Mirialan:
                    headsToUse = gender == Gender.Male ?
                        MaleMirialanHeads.Concat(MaleHumanHeads).ToArray() :
                        FemaleMirialanHeads.Concat(FemaleHumanHeads).ToArray();
                    break;
                case RacialType.Echani:
                    headsToUse = gender == Gender.Male ?
                        MaleEchaniHeads.Concat(MaleHumanHeads).ToArray() :
                        FemaleEchaniHeads.Concat(FemaleHumanHeads).ToArray();
                    break;
                case RacialType.Cyborg:
                    headsToUse = gender == Gender.Male ?
                        MaleCyborgHeads.Concat(MaleHumanHeads).ToArray() :
                        FemaleCyborgHeads.Concat(FemaleHumanHeads).ToArray();
                    break;
                case RacialType.Cathar:
                    headsToUse = gender == Gender.Male ? MaleCatharHeads : FemaleCatharHeads;
                    break;
                case RacialType.Trandoshan:
                    headsToUse = gender == Gender.Male ? MaleTrandoshanHeads : FemaleTrandoshanHeads;
                    break;
                case RacialType.Wookiee:
                    headsToUse = gender == Gender.Male ? MaleWookieeHeads : FemaleWookieeHeads;
                    break;
                case RacialType.MonCalamari:
                    headsToUse = gender == Gender.Male ? MaleMonCalamariHeads : FemaleMonCalamariHeads;
                    break;
                case RacialType.Ugnaught:
                    headsToUse = gender == Gender.Male ? MaleUgnaughtHeads : FemaleUgnaughtHeads;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Array.Sort(headsToUse);
            foreach (var head in headsToUse)
            {
                page.AddResponse($"Head #{head}", () =>
                {
                    SetCreatureBodyPart(CreaturePart.Head, head, player);
                });
            }

        }
        private void HairColorPageInit(DialogPage page)
        {
            page.Header = "Please select a color from the list below.";

            // These IDs are pulled from the HairColors.jpg file found in the ServerFiles/Reference folder.
            int[] HumanHairColors = { 0, 1, 2, 3, 4, 5, 9, 10, 11, 12, 13, 14, 15, 23, 167 };
            int[] BothanHairColors = { 2, 3, };
            int[] ChissHairColors = { 23, 51, 63, 16, 17, 24, 25, 31, 32, 33, 164, 165, 166, 167, 170, 171 };
            int[] ZabrakHairColors = { 0 };
            int[] TwilekHairColors = { }; // All
            int[] MirialanHairColors = { 0, 1, 2, 3, 4, 5, 6, 7, 14, 15, 16, 17, 167 };
            int[] EchaniHairColors = { 16, 62 };
            int[] CyborgHairColors = { 16, 17, 18, 19, 20, 21 };
            int[] CatharHairColors = { 0, 1, 2, 3, 4, 5, 6, 7, 116, 117 };
            int[] TrandoshanEyeColors = { }; // All
            int[] WookieeHairColors = { 0, 1, 2, 3, 14, 49, 51 };
            int[] MonCalamariHairColors = { }; // All
            int[] UgnaughtHairColors = { 16, 17, 18, 19, 62, 120, 128, 164, 166, 168 };

            var player = GetPC();
            var race = GetRacialType(player);
            int[] colorsToUse;

            switch (race)
            {
                case RacialType.Human:
                    colorsToUse = HumanHairColors;
                    break;
                case RacialType.Bothan:
                    colorsToUse = BothanHairColors;
                    break;
                case RacialType.Chiss:
                    colorsToUse = ChissHairColors;
                    break;
                case RacialType.Zabrak:
                    colorsToUse = ZabrakHairColors;
                    break;
                case RacialType.Twilek:
                    colorsToUse = TwilekHairColors;
                    break;
                case RacialType.Mirialan:
                    colorsToUse = MirialanHairColors;
                    break;
                case RacialType.Echani:
                    colorsToUse = EchaniHairColors;
                    break;
                case RacialType.Cyborg:
                    colorsToUse = CyborgHairColors.Concat(HumanHairColors).ToArray();
                    break;
                case RacialType.Cathar:
                    colorsToUse = CatharHairColors;
                    break;
                case RacialType.Trandoshan:
                    colorsToUse = TrandoshanEyeColors;
                    break;
                case RacialType.Wookiee:
                    colorsToUse = WookieeHairColors;
                    break;
                case RacialType.MonCalamari:
                    colorsToUse = MonCalamariHairColors;
                    break;
                case RacialType.Ugnaught:
                    colorsToUse = UgnaughtHairColors;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // If none specified, display all.
            if (colorsToUse.Length <= 0)
            {
                for (var x = 0; x <= 175; x++)
                {
                    var color = x; // Copy to local variable due to usage in action below.
                    page.AddResponse($"Color #{x}", () =>
                    {
                        SetColor(player, ColorChannel.Hair, color);
                    });
                }
            }
            else
            {
                Array.Sort(colorsToUse);
                foreach (var color in colorsToUse)
                {
                    page.AddResponse($"Color #{color}", () =>
                    {
                        SetColor(player, ColorChannel.Hair, color);
                    });
                }
            }
        }
        private void BodyPartTypePageInit(DialogPage page)
        {
            var player = GetPC();
            var race = GetRacialType(player);
            var model = GetDataModel<Model>();

            page.Header = "Please select a body part from the list below.";

            page.AddResponse("Torso", () =>
            {
                model.PartName = "Torso";
                model.BodyPartID = CreaturePart.Torso;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208, 209 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 166 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Pelvis", () =>
            {
                model.PartName = "Pelvis";
                model.BodyPartID = CreaturePart.Pelvis;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208, 209 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 11, 158 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Right Bicep", () =>
            {
                model.PartName = "Right Bicep";
                model.BodyPartID = CreaturePart.RightBicep;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Right Forearm", () =>
            {
                model.PartName = "Right Forearm";
                model.BodyPartID = CreaturePart.RightForearm;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 152 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Right Hand", () =>
            {
                model.PartName = "Right Hand";
                model.BodyPartID = CreaturePart.RightHand;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 5, 6, 63, 100, 110, 113, 121, 151, };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Right Thigh", () =>
            {
                model.PartName = "Right Thigh";
                model.BodyPartID = CreaturePart.RightThigh;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 154 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Right Shin", () =>
            {
                model.PartName = "Right Shin";
                model.BodyPartID = CreaturePart.RightShin;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Left Bicep", () =>
            {
                model.PartName = "Left Bicep";
                model.BodyPartID = CreaturePart.LeftBicep;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Left Forearm", () =>
            {
                model.PartName = "Left Forearm";
                model.BodyPartID = CreaturePart.LeftForearm;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 152 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Left Hand", () =>
            {
                model.PartName = "Left Hand";
                model.BodyPartID = CreaturePart.LeftHand;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 5, 6, 63, 100, 110, 113, 121, 151, };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Left Thigh", () =>
            {
                model.PartName = "Left Thigh";
                model.BodyPartID = CreaturePart.LeftThigh;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2, 154 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });

            page.AddResponse("Left Shin", () =>
            {
                model.PartName = "Left Shin";
                model.BodyPartID = CreaturePart.LeftShin;
                switch (race)
                {
                    case RacialType.Wookiee:
                        model.Parts = new[] { 208 };
                        break;
                    case RacialType.MonCalamari:
                        model.Parts = new[] { 204 };
                        break;
                    default:
                        model.Parts = new[] { 1, 2 };
                        break;
                }

                ChangePage(BodyPartPageId);
            });
        }

        private void BodyPartPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            page.Header = ColorToken.Green("Body Part: ") + model.PartName + "\n\n" + 
                          "You may need to unequip any clothes or armor you are wearing to see changes made to your body parts.\n\nPlease be aware that many armors override your selection here. This is a limitation in NWN that we can't work around.";

            foreach (var part in model.Parts)
            {
                page.AddResponse($"{model.PartName} #{part}", () =>
                {
                    SetCreatureBodyPart(model.BodyPartID, part, player);
                });
            }
        }

        private void TattooTypePageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = "Please select a tattoo option from the list below.";

            page.AddResponse("Tattoo Color 1", () =>
            {
                model.TattooChannel = ColorChannel.Tattoo1;
                ChangePage(TattooColorPageId);
            });

            page.AddResponse("Tattoo Color 2", () =>
            {
                model.TattooChannel = ColorChannel.Tattoo2;
                ChangePage(TattooColorPageId);
            });
        }

        private void TattooColorPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            page.Header = "Please select a tattoo color.";

            for (var x = 0; x <= 175; x++)
            {
                var color = x;
                page.AddResponse($"Color #{x}", () =>
                {
                    SetColor(player, model.TattooChannel, color);
                });
            }
        }
    }
}
