using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Instantly gives this creature the benefits of a rest (restored hitpoints, spells, feats, etc..)
        /// </summary>
        public static void ForceRest(uint oCreature)
        {
            global::NWN.Core.NWScript.ForceRest(oCreature);
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is resting.
        /// </summary>
        public static bool GetIsResting(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsResting(oCreature) != 0;
        }

        /// <summary>
        ///   Get the last PC that has rested in the module.
        /// </summary>
        public static uint GetLastPCRested()
        {
            return global::NWN.Core.NWScript.GetLastPCRested();
        }

        /// <summary>
        ///   Determine the type (REST_EVENTTYPE_REST_*) of the last rest event (as
        ///   returned from the OnPCRested module event).
        /// </summary>
        public static RestEventType GetLastRestEventType()
        {
            return (RestEventType)global::NWN.Core.NWScript.GetLastRestEventType();
        }

        /// <summary>
        ///   The creature will rest if not in combat and no enemies are nearby.
        ///   - bCreatureToEnemyLineOfSightCheck: TRUE to allow the creature to rest if enemies
        ///   are nearby, but the creature can't see the enemy.
        ///   FALSE the creature will not rest if enemies are
        ///   nearby regardless of whether or not the creature
        ///   can see them, such as if an enemy is close by,
        ///   but is in a different room behind a closed door.
        /// </summary>
        public static void ActionRest(bool bCreatureToEnemyLineOfSightCheck = false)
        {
            global::NWN.Core.NWScript.ActionRest(bCreatureToEnemyLineOfSightCheck ? 1 : 0);
        }
    }
}