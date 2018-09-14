using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class RaceService : IRaceService
    {
        private readonly INWScript _;

        public RaceService(
            INWScript script)
        {
            _ = script;
        }
        
        public void ApplyDefaultAppearance(NWPlayer player)
        {
            CustomRaceType race = (CustomRaceType)player.RacialType;
            int maleHead;
            int femaleHead;
            int skinColor;
            int hairColor;
            int gender = player.Gender;
            int appearance = APPEARANCE_TYPE_HUMAN;

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
                case CustomRaceType.Cathar:
                    skinColor = 54;
                    hairColor = 0;
                    appearance = APPEARANCE_TYPE_HALF_ORC;
                    maleHead = 27;
                    femaleHead = 18;
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
            }
            else if (gender == GENDER_FEMALE)
            {
                _.SetCreatureBodyPart(CREATURE_PART_HEAD, femaleHead, player);
            }
        }

    }
}
