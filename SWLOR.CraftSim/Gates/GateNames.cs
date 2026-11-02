namespace SWLOR.CraftSim.Gates
{
    /// <summary>The seven gates of the plan, so a report names them the same way every time.</summary>
    public static class GateNames
    {
        public const string NoFixedRotation = "No fixed rotation";
        public const string StyleBalance = "Style balance";
        public const string PerksOptional = "Perks optional";
        public const string Length = "Length";
        public const string EconomyParity = "Economy parity";
        public const string XpRate = "XP rate";
        public const string EnhancementParity = "Enhancement parity";

        public static readonly IReadOnlyList<string> All = new[] { NoFixedRotation, StyleBalance, PerksOptional, Length, EconomyParity, XpRate, EnhancementParity };
    }
}
