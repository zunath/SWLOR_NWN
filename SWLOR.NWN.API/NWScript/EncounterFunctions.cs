using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Determines whether the specified encounter is active.
        /// </summary>
        /// <param name="oEncounter">The encounter to check (default: OBJECT_INVALID)</param>
        /// <returns>1 if the encounter is active, 0 otherwise</returns>
        public static int GetEncounterActive(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterActive(oEncounter);
        }

        /// <summary>
        /// Sets the encounter's active state to the specified value.
        /// </summary>
        /// <param name="nNewValue">The new active state (1 for TRUE, 0 for FALSE)</param>
        /// <param name="oEncounter">The encounter to set the active state for (default: OBJECT_INVALID)</param>
        public static void SetEncounterActive(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterActive(nNewValue, oEncounter);
        }

        /// <summary>
        /// Gets the maximum number of times that the encounter will spawn.
        /// </summary>
        /// <param name="oEncounter">The encounter to check (default: OBJECT_INVALID)</param>
        /// <returns>The maximum number of spawns</returns>
        public static int GetEncounterSpawnsMax(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterSpawnsMax(oEncounter);
        }

        /// <summary>
        /// Sets the maximum number of times that the encounter can spawn.
        /// </summary>
        /// <param name="nNewValue">The new maximum number of spawns</param>
        /// <param name="oEncounter">The encounter to set the maximum spawns for (default: OBJECT_INVALID)</param>
        public static void SetEncounterSpawnsMax(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterSpawnsMax(nNewValue, oEncounter);
        }

        /// <summary>
        /// Gets the number of times that the encounter has spawned so far.
        /// </summary>
        /// <param name="oEncounter">The encounter to check (default: OBJECT_INVALID)</param>
        /// <returns>The current number of spawns</returns>
        public static int GetEncounterSpawnsCurrent(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterSpawnsCurrent(oEncounter);
        }

        /// <summary>
        /// Sets the number of times that the encounter has spawned so far.
        /// </summary>
        /// <param name="nNewValue">The new current number of spawns</param>
        /// <param name="oEncounter">The encounter to set the current spawns for (default: OBJECT_INVALID)</param>
        public static void SetEncounterSpawnsCurrent(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterSpawnsCurrent(nNewValue, oEncounter);
        }

        /// <summary>
        /// Sets the difficulty level of the specified encounter.
        /// </summary>
        /// <param name="nEncounterDifficulty">The encounter difficulty (ENCOUNTER_DIFFICULTY_* constants)</param>
        /// <param name="oEncounter">The encounter to set the difficulty for (default: OBJECT_INVALID)</param>
        public static void SetEncounterDifficulty(EncounterDifficulty nEncounterDifficulty,
            uint oEncounter = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetEncounterDifficulty((int)nEncounterDifficulty, oEncounter);
        }

        /// <summary>
        /// Gets the difficulty level of the specified encounter.
        /// </summary>
        /// <param name="oEncounter">The encounter to get the difficulty for (default: OBJECT_INVALID)</param>
        /// <returns>The encounter difficulty level</returns>
        public static int GetEncounterDifficulty(uint oEncounter = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetEncounterDifficulty(oEncounter);
        }
    }
}
