using SWLOR.CombatSim.Rules;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// How a heal or shield's size is read from max HP and the healer's Willpower. <see cref="Game"/> is the game today:
    /// every heal is a percent of the target's max HP, and Willpower adds up to 25%.
    /// </summary>
    /// <param name="PercentOfMaxHpShare">
    /// Share of each heal sized from the target's own max HP; the rest is sized from <paramref name="ReferenceMaxHp"/>,
    /// so it is the same flat amount for every target.
    /// </param>
    /// <param name="ReferenceMaxHp">Max HP the flat part is sized for: a target with this much HP heals the same under any share.</param>
    /// <param name="StatMultiplierAtBaseline">Multiplier at Willpower 10 and below.</param>
    /// <param name="StatMultiplierAtCap">Multiplier at Willpower 26 and above, in a straight line from the baseline one.</param>
    public readonly record struct HealFormula(
        float PercentOfMaxHpShare,
        int ReferenceMaxHp,
        float StatMultiplierAtBaseline,
        float StatMultiplierAtCap)
    {
        public static HealFormula Game { get; } = new(1f, 0, 1f, 1.25f);

        public static HealFormula From(RuleSet rules) => new(
            rules.HealPercentOfMaxHpShare,
            rules.HealReferenceMaxHp,
            rules.HealStatMultiplierAtBaseline,
            rules.HealStatMultiplierAtCap);
    }
}
