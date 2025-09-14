using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Instantly gives this creature the benefits of a rest (restored hitpoints, spells, feats, etc.).
        /// </summary>
        /// <param name="oCreature">The creature to force rest</param>
        public static void ForceRest(uint oCreature)
        {
            global::NWN.Core.NWScript.ForceRest(oCreature);
        }

        /// <summary>
        /// Returns TRUE if the creature is resting.
        /// </summary>
        /// <param name="oCreature">The creature to check (defaults to OBJECT_INVALID)</param>
        /// <returns>TRUE if the creature is resting</returns>
        public static bool GetIsResting(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsResting(oCreature) != 0;
        }

        /// <summary>
        /// Gets the last PC that has rested in the module.
        /// </summary>
        /// <returns>The last PC that has rested</returns>
        public static uint GetLastPCRested()
        {
            return global::NWN.Core.NWScript.GetLastPCRested();
        }

        /// <summary>
        /// Determines the type of the last rest event (as returned from the OnPCRested module event).
        /// </summary>
        /// <returns>The type (REST_EVENTTYPE_REST_*) of the last rest event</returns>
        public static RestEventType GetLastRestEventType()
        {
            return (RestEventType)global::NWN.Core.NWScript.GetLastRestEventType();
        }

        /// <summary>
        /// The creature will rest if not in combat and no enemies are nearby.
        /// </summary>
        /// <param name="bCreatureToEnemyLineOfSightCheck">TRUE to allow the creature to rest if enemies
        /// are nearby, but the creature can't see the enemy.
        /// FALSE the creature will not rest if enemies are
        /// nearby regardless of whether or not the creature
        /// can see them, such as if an enemy is close by,
        /// but is in a different room behind a closed door</param>
        public static void ActionRest(bool bCreatureToEnemyLineOfSightCheck = false)
        {
            global::NWN.Core.NWScript.ActionRest(bCreatureToEnemyLineOfSightCheck ? 1 : 0);
        }
    }
}