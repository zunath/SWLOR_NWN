using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>A weapon ability paid for and waiting for the caster's next landed attack.</summary>
    public sealed class QueuedWeaponAbility
    {
        public AbilityUseSpec Use { get; init; }
        public AbilityInfo Ability { get; init; }
        public double ExpiresAt { get; init; }
    }
}
