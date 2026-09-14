namespace SWLOR.Game.Server.Service.StatService
{
    /// <summary>
    /// Describes how a stat adjustment should be interpreted when deciding whether
    /// an otherwise untyped effect is beneficial.
    /// </summary>
    public enum StatTypeCategory
    {
        /// <summary>
        /// The stat does not make an otherwise untyped effect beneficial.
        /// </summary>
        NonBeneficial = 0,

        /// <summary>
        /// Positive adjustments are beneficial, while negative adjustments are not.
        /// </summary>
        BeneficialWhenPositive = 1,

        /// <summary>
        /// Negative adjustments are beneficial, while positive adjustments are not.
        /// </summary>
        BeneficialWhenNegative = 2
    }
}
