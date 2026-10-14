namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// An ability that heals its caster for a share of the damage it deals, more against a target at or below
    /// half HP, up to <see cref="MaximumMaxHpPercentPerCast"/> of the caster's max HP per cast.
    /// </summary>
    public sealed class DamageHealing
    {
        /// <summary>The game's Combat.MaximumActivatedDamageHealingMaxHPPercent.</summary>
        public const float MaximumMaxHpPercentPerCast = 15f;

        /// <summary>The game's Combat.MaximumDamageDerivedHealingPercentPerHit.</summary>
        public const float MaximumDamagePercentPerHit = 50f;

        public float Percent { get; init; }
        public float LowHpPercent { get; init; }
    }
}
