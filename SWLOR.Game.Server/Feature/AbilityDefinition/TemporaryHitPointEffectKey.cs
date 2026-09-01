namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    /// <summary>
    /// Stable identifiers for temporary-HP pools shared across multiple gameplay systems.
    /// These values become native effect tags and local-variable names, so they must not change.
    /// </summary>
    public static class TemporaryHitPointEffectKey
    {
        public const string FatalDamageSave = "FATAL_DAMAGE_SAVE";
        public const string GuardianWard = "GUARDIAN_WARD";
    }
}
