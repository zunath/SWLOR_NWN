using SWLOR.CombatSim.Rules;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// How heal and shield sizes are read: the game's flat amounts plus a small percent of max HP, or the percent-only
    /// sizes from before the tuning; and the Willpower multipliers at 10 and 26.
    /// </summary>
    /// <param name="FlatHeals">True for each effect's <see cref="Effects.AbilityEffect.Heal"/>; false for its percent.</param>
    /// <param name="StatMultiplierAtBaseline">Multiplier at Willpower 10 and below.</param>
    /// <param name="StatMultiplierAtCap">Multiplier at Willpower 26 and above, in a straight line from the baseline one.</param>
    public readonly record struct HealFormula(
        bool FlatHeals,
        float StatMultiplierAtBaseline,
        float StatMultiplierAtCap)
    {
        /// <summary>Before the tuning: every heal a percent of max HP, and Willpower adding up to 25%.</summary>
        public static HealFormula PercentHeals { get; } = new(false, 1f, 1.25f);

        /// <summary>The game as built, read from AbilityEffectScaling.</summary>
        public static HealFormula Game { get; } = new(
            true,
            AbilityEffectScaling.RestorativeMultiplierAtBaseline,
            AbilityEffectScaling.RestorativeMultiplierAtCap);

        public static HealFormula From(RuleSet rules) => new(
            rules.FlatHeals,
            rules.HealStatMultiplierAtBaseline,
            rules.HealStatMultiplierAtCap);
    }
}
