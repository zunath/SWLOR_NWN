namespace SWLOR.Game.Server.Service.CombatService
{
    internal static class CombatActivity
    {
        public static void TrackRecentDamageTarget(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            CombatState.TrackRecentDamageTarget(attacker, defender);
        }

        public static bool HasRecentDamageTarget(uint attacker, uint defender, float windowSeconds)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender || windowSeconds <= 0f)
                return false;

            return CombatState.HasRecentDamageTarget(attacker, defender, windowSeconds);
        }

        public static void TrackRecentDamageTaken(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CombatState.TrackRecentDamageTaken(creature);
        }

        public static bool HasRecentDamageTaken(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            return CombatState.HasRecentDamageTaken(creature, windowSeconds);
        }

        public static void TrackCombatActivity(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CombatState.TrackCombatActivity(creature);
        }

        public static bool HasRecentCombatActivity(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            return CombatState.HasRecentCombatActivity(creature, windowSeconds);
        }

        public static void TrackAttackActivity(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CombatState.TrackAttackActivity(creature);
        }

        public static bool HasRecentAttackActivity(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            return CombatState.HasRecentAttackActivity(creature, windowSeconds);
        }

        public static void TrackGuardedHit(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CombatState.TrackGuardedHit(creature);
        }

        public static bool HasRecentGuardedHit(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            return CombatState.HasRecentGuardedHit(creature, windowSeconds);
        }

        public static void TrackDeflection(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CombatState.TrackDeflection(creature);
        }

        public static bool HasRecentDeflection(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            return CombatState.HasRecentDeflection(creature, windowSeconds);
        }
    }
}
