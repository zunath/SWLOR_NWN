using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class RaceService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
        }

        private static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            CustomRaceType race = (CustomRaceType) player.RacialType;

            if (race == CustomRaceType.Wookiee)
            {
                _.SetObjectVisualTransform(player, OBJECT_VISUAL_TRANSFORM_SCALE, 1.2f);
            }
        }

        public static void ApplyDefaultAppearance(NWPlayer player)
        {
            CustomRaceType race = (CustomRaceType)player.RacialType;
            int maleHead;
            int femaleHead;
            int skinColor;
            int hairColor;
            int gender = player.Gender;
            int appearance = APPEARANCE_TYPE_HUMAN;

            int maleNeck = 1;
            int maleTorso = 1;
            int malePelvis = 1;
            int maleRightBicep = 1;
            int maleRightForearm = 1;
            int maleRightHand = 1;
            int maleRightThigh = 1;
            int maleRightShin = 1;
            int maleRightFoot = 1;
            int maleLeftBicep = 1;
            int maleLeftForearm = 1;
            int maleLeftHand = 1;
            int maleLeftThigh = 1;
            int maleLeftShin = 1;
            int maleLeftFoot = 1;

            int femaleNeck = 1;
            int femaleTorso = 1;
            int femalePelvis = 1;
            int femaleRightBicep = 1;
            int femaleRightForearm = 1;
            int femaleRightHand = 1;
            int femaleRightThigh = 1;
            int femaleRightShin = 1;
            int femaleRightFoot = 1;
            int femaleLeftBicep = 1;
            int femaleLeftForearm = 1;
            int femaleLeftHand = 1;
            int femaleLeftThigh = 1;
            int femaleLeftShin = 1;
            int femaleLeftFoot = 1;


            switch (race)
            {
                case CustomRaceType.Human:
                    skinColor = 2;
                    hairColor = 0;
                    maleHead = 1;
                    femaleHead = 1;
                    break;
                case CustomRaceType.Bothan:
                    skinColor = 6;
                    hairColor = 1;
                    appearance = APPEARANCE_TYPE_ELF;
                    maleHead = 40;
                    femaleHead = 109;
                    break;
                case CustomRaceType.Chiss:
                    skinColor = 137;
                    hairColor = 134;
                    maleHead = 33;
                    femaleHead = 191;
                    break;
                case CustomRaceType.Zabrak:
                    skinColor = 88;
                    hairColor = 0;
                    maleHead = 103;
                    femaleHead = 120;
                    break;
                case CustomRaceType.Twilek:
                    skinColor = 52;
                    hairColor = 0;
                    maleHead = 115;
                    femaleHead = 145;
                    break;
                case CustomRaceType.Cyborg:
                    skinColor = 2;
                    hairColor = 0;
                    maleHead = 168;
                    femaleHead = 41;
                    break;
                case CustomRaceType.Mirialan:
                    skinColor = 38;
                    hairColor = 3;
                    maleHead = 20;
                    femaleHead = 1;
                    break;
                case CustomRaceType.Echani:
                    skinColor = 164;
                    hairColor = 16;
                    maleHead = 182;
                    femaleHead = 45;
                    break;
                case CustomRaceType.Cathar:
                    skinColor = 54;
                    hairColor = 0;
                    appearance = APPEARANCE_TYPE_HALF_ORC;
                    maleHead = 27;
                    femaleHead = 18;
                    break;
                case CustomRaceType.Trandoshan:
                    skinColor = 39;
                    hairColor = 4;
                    maleHead = 162;
                    femaleHead = 135;
                    maleNeck = 201;
                    maleTorso = 201;
                    malePelvis = 201;
                    maleRightBicep = 201;
                    maleRightForearm = 201;
                    maleRightHand = 201;
                    maleRightThigh = 201;
                    maleRightShin = 201;
                    maleRightFoot = 201;
                    maleLeftBicep = 201;
                    maleLeftForearm = 201;
                    maleLeftHand = 201;
                    maleLeftThigh = 201;
                    maleLeftShin = 201;
                    maleLeftFoot = 201;

                    femaleNeck = 201;
                    femaleTorso = 201;
                    femalePelvis = 201;
                    femaleRightBicep = 201;
                    femaleRightForearm = 201;
                    femaleRightHand = 201;
                    femaleRightThigh = 201;
                    femaleRightShin = 201;
                    femaleRightFoot = 201;
                    femaleLeftBicep = 201;
                    femaleLeftForearm = 201;
                    femaleLeftHand = 201;
                    femaleLeftThigh = 201;
                    femaleLeftShin = 201;
                    femaleLeftFoot = 201;

                    break;
                case CustomRaceType.Wookiee:

                    appearance = APPEARANCE_TYPE_ELF;
                    skinColor = 0;
                    hairColor = 0;
                    maleHead = 192;
                    femaleHead = 110;
                    maleNeck = 1;
                    maleTorso = 208;
                    malePelvis = 208;
                    maleRightBicep = 208;
                    maleRightForearm = 208;
                    maleRightHand = 208;
                    maleRightThigh = 208;
                    maleRightShin = 208;
                    maleRightFoot = 208;
                    maleLeftBicep = 208;
                    maleLeftForearm = 208;
                    maleLeftHand = 208;
                    maleLeftThigh = 208;
                    maleLeftShin = 208;
                    maleLeftFoot = 208;

                    femaleNeck = 1;
                    femaleTorso = 208;
                    femalePelvis = 208;
                    femaleRightBicep = 208;
                    femaleRightForearm = 208;
                    femaleRightHand = 208;
                    femaleRightThigh = 208;
                    femaleRightShin = 208;
                    femaleRightFoot = 208;
                    femaleLeftBicep = 208;
                    femaleLeftForearm = 208;
                    femaleLeftHand = 208;
                    femaleLeftThigh = 208;
                    femaleLeftShin = 208;
                    femaleLeftFoot = 208;

                    break;
                case CustomRaceType.MonCalamari:
                    skinColor = 6;
                    hairColor = 7;

                    maleHead = 6;
                    femaleHead = 6;
                    maleNeck = 1;
                    maleTorso = 204;
                    malePelvis = 204;
                    maleRightBicep = 204;
                    maleRightForearm = 204;
                    maleRightHand = 204;
                    maleRightThigh = 204;
                    maleRightShin = 204;
                    maleRightFoot = 204;
                    maleLeftBicep = 204;
                    maleLeftForearm = 204;
                    maleLeftHand = 204;
                    maleLeftThigh = 204;
                    maleLeftShin = 204;
                    maleLeftFoot = 204;

                    femaleNeck = 1;
                    femaleTorso = 204;
                    femalePelvis = 204;
                    femaleRightBicep = 204;
                    femaleRightForearm = 204;
                    femaleRightHand = 204;
                    femaleRightThigh = 204;
                    femaleRightShin = 204;
                    femaleRightFoot = 204;
                    femaleLeftBicep = 204;
                    femaleLeftForearm = 204;
                    femaleLeftHand = 204;
                    femaleLeftThigh = 204;
                    femaleLeftShin = 204;
                    femaleLeftFoot = 204;
                    break;
                case CustomRaceType.Ugnaught:

                    appearance = APPEARANCE_TYPE_DWARF;
                    skinColor = 0;
                    hairColor = 0;
                    maleHead = 100;
                    femaleHead = 100;
                    maleNeck = 1;
                    maleTorso = 1;
                    malePelvis = 7;
                    maleRightBicep = 1;
                    maleRightForearm = 1;
                    maleRightHand = 1;
                    maleRightThigh = 1;
                    maleRightShin = 1;
                    maleRightFoot = 1;
                    maleLeftBicep = 1;
                    maleLeftForearm = 1;
                    maleLeftHand = 1;
                    maleLeftThigh = 1;
                    maleLeftShin = 1;
                    maleLeftFoot = 1;

                    femaleNeck = 1;
                    femaleTorso = 54;
                    femalePelvis = 70;
                    femaleRightBicep = 1;
                    femaleRightForearm = 1;
                    femaleRightHand = 1;
                    femaleRightThigh = 1;
                    femaleRightShin = 1;
                    femaleRightFoot = 1;
                    femaleLeftBicep = 1;
                    femaleLeftForearm = 1;
                    femaleLeftHand = 1;
                    femaleLeftThigh = 1;
                    femaleLeftShin = 1;
                    femaleLeftFoot = 1;
                    break;
                default:
                    {
                        _.BootPC(player, "You have selected an invalid race. This could be due to files in your override folder. Ensure these are removed from the folder and then try creating a new character. If you have any problems, visit our website at http://starwarsnwn.com");
                        return;
                    }
            }
            _.SetCreatureAppearanceType(player, appearance);
            _.SetColor(player, COLOR_CHANNEL_SKIN, skinColor);
            _.SetColor(player, COLOR_CHANNEL_HAIR, hairColor);
            
            if (gender == GENDER_MALE)
            {
                _.SetCreatureBodyPart(CREATURE_PART_HEAD, maleHead, player);

                _.SetCreatureBodyPart(CREATURE_PART_NECK, maleNeck, player);
                _.SetCreatureBodyPart(CREATURE_PART_TORSO, maleTorso, player);
                _.SetCreatureBodyPart(CREATURE_PART_PELVIS, malePelvis, player);

                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_BICEP, maleRightBicep, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_FOREARM, maleRightForearm, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_HAND, maleRightHand, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_THIGH, maleRightThigh, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_SHIN, maleRightShin, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_FOOT, maleRightFoot, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_BICEP, maleLeftBicep, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_FOREARM, maleLeftForearm, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_HAND, maleLeftHand, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_THIGH, maleLeftThigh, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_SHIN, maleLeftShin, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_FOOT, maleLeftFoot, player);
            }
            else if (gender == GENDER_FEMALE)
            {
                _.SetCreatureBodyPart(CREATURE_PART_HEAD, femaleHead, player);

                _.SetCreatureBodyPart(CREATURE_PART_NECK, femaleNeck, player);
                _.SetCreatureBodyPart(CREATURE_PART_TORSO, femaleTorso, player);
                _.SetCreatureBodyPart(CREATURE_PART_PELVIS, femalePelvis, player);

                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_BICEP, femaleRightBicep, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_FOREARM, femaleRightForearm, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_HAND, femaleRightHand, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_THIGH, femaleRightThigh, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_SHIN, femaleRightShin, player);
                _.SetCreatureBodyPart(CREATURE_PART_RIGHT_FOOT, femaleRightFoot, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_BICEP, femaleLeftBicep, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_FOREARM, femaleLeftForearm, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_HAND, femaleLeftHand, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_THIGH, femaleLeftThigh, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_SHIN, femaleLeftShin, player);
                _.SetCreatureBodyPart(CREATURE_PART_LEFT_FOOT, femaleLeftFoot, player);
            }
        }

    }
}
