using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class RacePlugin
    {
        private const string PLUGIN_NAME = "NWNX_Race";

        // Sets a racial modifier.
        public static void SetRacialModifier(
            RacialType race, 
            RaceModifiers modifier, 
            uint iParam1,
            uint iParam2 = 0xDEADBEEF, 
            uint iParam3 = 0xDEADBEEF)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRacialModifier");
            NWNCore.NativeFunctions.nwnxPushInt((int)iParam3);
            NWNCore.NativeFunctions.nwnxPushInt((int)iParam2);
            NWNCore.NativeFunctions.nwnxPushInt((int)iParam1);
            NWNCore.NativeFunctions.nwnxPushInt((int)modifier);
            NWNCore.NativeFunctions.nwnxPushInt((int)race);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the parent race for a race.
        public static int GetParentRace(RacialType race)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetParentRace");
            NWNCore.NativeFunctions.nwnxPushInt((int)race);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetFavoredEnemyFeat(RacialType iRace, FeatType iFeat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFavoredEnemyFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)iFeat);
            NWNCore.NativeFunctions.nwnxPushInt((int)iRace);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}