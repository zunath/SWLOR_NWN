namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Who is a player and who fights for one, read straight from the engine: no state and no services, so the status effect
    /// service can ask it without reaching up to Combat. <c>PvpCombatService</c> answers its own reads from the same rules.
    /// </summary>
    public static class PlayerControlRules
    {
        public static bool IsPlayer(uint creature)
        {
            return GetIsObjectValid(creature) && GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
        }

        /// <summary>The player a creature fights for: the player itself, or the master of a player's pet.</summary>
        public static uint GetControllingPlayer(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return OBJECT_INVALID;
            if (IsPlayer(creature))
                return creature;

            var master = GetMaster(creature);
            return IsPlayer(master) ? master : OBJECT_INVALID;
        }

        /// <summary>Whether a player or a player's pet is acting on a different player.</summary>
        public static bool IsPlayerAgainstPlayer(uint source, uint target)
        {
            if (!IsPlayer(target))
                return false;

            var player = GetControllingPlayer(source);
            return GetIsObjectValid(player) && player != target;
        }
    }
}
