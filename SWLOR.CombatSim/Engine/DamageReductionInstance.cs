using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>A timed cut to damage taken, such as Pain Suppressant's or a Force Sanctuary zone's.</summary>
    public sealed class DamageReductionInstance
    {
        public float Percent { get; init; }
        public DamageReductionScope Scope { get; init; }

        /// <summary>Cuts in the same family keep only the strongest active one; null for none.</summary>
        public string Group { get; init; }
        public double ExpiresAt { get; init; }

        public bool Applies(DamageType type) => Scope switch
        {
            DamageReductionScope.All => true,
            DamageReductionScope.PhysicalAndForce => type is DamageType.Physical or DamageType.Force,
            DamageReductionScope.ForceOnly => type == DamageType.Force,
            _ => false
        };
    }
}
