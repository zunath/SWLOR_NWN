using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Encounter
    {
        private const string PLUGIN_NAME = "NWNX_Encounter";

        public static int GetNumberOfCreaturesInEncounterList(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNumberOfCreaturesInEncounterList");
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static CreatureListEntry GetEncounterCreatureByIndex(uint encounter, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEncounterCreatureByIndex");
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();

            return new CreatureListEntry
            {
                unique = Internal.NativeFunctions.nwnxPopInt(),
                challengeRating = Internal.NativeFunctions.nwnxPopFloat(),
                resref = Internal.NativeFunctions.nwnxPopString()
            };
        }

        public static void SetEncounterCreatureByIndex(uint encounter, int index, CreatureListEntry creatureEntry)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEncounterCreatureByIndex");
            Internal.NativeFunctions.nwnxPushInt(creatureEntry.unique);
            Internal.NativeFunctions.nwnxPushFloat(creatureEntry.challengeRating);
            Internal.NativeFunctions.nwnxPushString(creatureEntry.resref!);
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetFactionId(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFactionId");
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetFactionId(uint encounter, int factionId)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFactionId");
            Internal.NativeFunctions.nwnxPushInt(factionId);
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetPlayerTriggeredOnly(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlayerTriggeredOnly");
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetPlayerTriggeredOnly(uint encounter, int playerTriggeredOnly)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlayerTriggeredOnly");
            Internal.NativeFunctions.nwnxPushInt(playerTriggeredOnly);
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetResetTime(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetResetTime");
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetResetTime(uint encounter, int resetTime)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetResetTime");
            Internal.NativeFunctions.nwnxPushInt(resetTime);
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetNumberOfSpawnPoints(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNumberOfSpawnPoints");

            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }


        public static Location GetSpawnPointByIndex(uint encounter, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSpawnPointByIndex");

            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();

            var o = Internal.NativeFunctions.nwnxPopFloat();
            var z = Internal.NativeFunctions.nwnxPopFloat();
            var y = Internal.NativeFunctions.nwnxPopFloat();
            var x = Internal.NativeFunctions.nwnxPopFloat();

            return NWScript.NWScript.Location(NWScript.NWScript.GetArea(encounter), NWScript.NWScript.Vector3(x, y, z), o);
        }

        public static int GetMinNumSpawned(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMinNumSpawned");

            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetMaxNumSpawned(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMaxNumSpawned");

            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetCurrentNumSpawned(uint encounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNumSpawned");

            Internal.NativeFunctions.nwnxPushObject(encounter);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }
    }
}