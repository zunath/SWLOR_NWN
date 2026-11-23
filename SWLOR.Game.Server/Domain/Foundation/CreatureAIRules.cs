using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// The creature AI checks that read only engine state: whether a creature is leash-evading, whether it runs AI, whether a
    /// target is inside its aggro aura and whether it stands outside its home radius. No state and no container, so Combat,
    /// Character, Space and the feature scripts call them without an AI interface. The creature AI service uses them too.
    /// </summary>
    public static class CreatureAIRules
    {
        public const float AggroRadius = 8.5f;
        public const float ReturnHomeRadius = 15f;
        public const string LeashEvadeActiveVariable = "AI_LEASH_EVADE_ACTIVE";

        public static bool IsLeashEvading(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   GetLocalBool(creature, LeashEvadeActiveVariable);
        }

        public static bool IsOutsideHomeRadius(uint creature, Location homeLocation, float radius = ReturnHomeRadius)
        {
            return GetIsObjectValid(GetAreaFromLocation(homeLocation)) &&
                   (GetAreaFromLocation(homeLocation) != GetArea(creature) ||
                    GetDistanceBetweenLocations(GetLocation(creature), homeLocation) > radius);
        }

        public static bool IsInAggroRange(uint creature, uint target)
        {
            return GetIsObjectValid(target) &&
                   GetArea(creature) == GetArea(target) &&
                   GetDistanceBetween(creature, target) <= AggroRadius &&
                   LineOfSightObject(target, creature);
        }

        public static bool IsCreatureAIEnabled(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   GetAILevel(creature) != AILevel.VeryLow;
        }
    }
}
