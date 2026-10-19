namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Creatures the Design Bible's World NPCs tab rates as Boss carry this local from their template.
    /// </summary>
    public static class BossCreature
    {
        public const string Variable = "IS_BOSS";

        public static bool IsBoss(uint creature)
        {
            return GetLocalBool(creature, Variable);
        }
    }
}
