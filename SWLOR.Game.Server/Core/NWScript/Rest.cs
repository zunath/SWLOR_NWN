using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Instantly gives this creature the benefits of a rest (restored hitpoints, spells, feats, etc..)
        /// </summary>
        public static void ForceRest(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(775);
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is resting.
        /// </summary>
        public static bool GetIsResting(uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.Call(505);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Get the last PC that has rested in the module.
        /// </summary>
        public static uint GetLastPCRested()
        {
            VM.Call(506);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Determine the type (REST_EVENTTYPE_REST_*) of the last rest event (as
        ///   returned from the OnPCRested module event).
        /// </summary>
        public static RestEventType GetLastRestEventType()
        {
            VM.Call(508);
            return (RestEventType)VM.StackPopInt();
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
            VM.StackPush(bCreatureToEnemyLineOfSightCheck ? 1 : 0);
            VM.Call(402);
        }
    }
}