using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Determine whether oEncounter is active.
        /// </summary>
        public static int GetEncounterActive(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterActive(oEncounter);
        }

        /// <summary>
        ///   Set oEncounter's active state to nNewValue.
        ///   - nNewValue: TRUE/FALSE
        ///   - oEncounter
        /// </summary>
        public static void SetEncounterActive(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterActive(nNewValue, oEncounter);
        }

        /// <summary>
        ///   Get the maximum number of times that oEncounter will spawn.
        /// </summary>
        public static int GetEncounterSpawnsMax(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterSpawnsMax(oEncounter);
        }

        /// <summary>
        ///   Set the maximum number of times that oEncounter can spawn
        /// </summary>
        public static void SetEncounterSpawnsMax(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterSpawnsMax(nNewValue, oEncounter);
        }

        /// <summary>
        ///   Get the number of times that oEncounter has spawned so far
        /// </summary>
        public static int GetEncounterSpawnsCurrent(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterSpawnsCurrent(oEncounter);
        }

        /// <summary>
        ///   Set the number of times that oEncounter has spawned so far
        /// </summary>
        public static void SetEncounterSpawnsCurrent(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterSpawnsCurrent(nNewValue, oEncounter);
        }

        /// <summary>
        ///   Set the difficulty level of oEncounter.
        ///   - nEncounterDifficulty: ENCOUNTER_DIFFICULTY_*
        ///   - oEncounter
        /// </summary>
        public static void SetEncounterDifficulty(EncounterDifficulty nEncounterDifficulty,
            uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterDifficulty((int)nEncounterDifficulty, oEncounter);
        }

        /// <summary>
        ///   Get the difficulty level of oEncounter.
        /// </summary>
        public static int GetEncounterDifficulty(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterDifficulty(oEncounter);
        }
    }
}
