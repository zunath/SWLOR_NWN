using System.Collections.Generic;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Names the files and types that make up <c>Service.Combat</c> while it is split (see
    /// <c>Readmes/MaintainabilityRefactorCombatSplit.md</c>): the partial class in <c>Service/CombatParts</c>, the part
    /// services <c>Domain/Combat/Combat*Service.cs</c> and the static <c>CombatRules</c>. Tests that read the source of
    /// <c>Combat</c> or reflect on its private members go through <see cref="CombatSource"/>-style helpers built on
    /// this list, so a move changes this one place.
    /// </summary>
    public static class CombatParts
    {
        /// <summary>
        /// Services of the Combat domain whose names start with <c>Combat</c> but that are not parts of <c>Combat</c>:
        /// they were converted on their own and have their own facades.
        /// </summary>
        public static readonly IReadOnlySet<string> OtherServices = new HashSet<string>(StringComparer.Ordinal)
        {
            "CombatPointService",
            "CombatRecorderService"
        };

        /// <summary>The static class the pure formulas move to.</summary>
        public const string RulesTypeName = "CombatRules";

        /// <summary>
        /// Whether a type or file name (without extension) is a part of <c>Combat</c> in <c>Domain/Combat</c>: a
        /// <c>Combat*Service</c> class or <c>CombatRules</c>, but not an interface and not one of <see cref="OtherServices"/>.
        /// </summary>
        public static bool IsPartTypeName(string name)
        {
            if (name == RulesTypeName)
                return true;

            if (!name.StartsWith("Combat", StringComparison.Ordinal) ||
                !name.EndsWith("Service", StringComparison.Ordinal))
                return false;

            return !OtherServices.Contains(name);
        }
    }
}
