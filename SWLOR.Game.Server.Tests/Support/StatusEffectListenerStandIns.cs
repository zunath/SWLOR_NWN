using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Stand-ins for the three real implementers of the status effect notices. Each writes the notice it acts on to the bench log
/// in the order the service sends it and leaves the others empty, as the real implementer does; the bench registers them in the
/// order the boot handlers register the real ones: melee, impact, capstone.
/// </summary>
public static class StatusEffectListenerStandIns
{
    /// <summary>Stands in for <c>MeleeTechniqueService</c>: the stance and direct damage notices.</summary>
    public sealed class Melee : IStatusEffectMeleeListener
    {
        private readonly List<string> _log;

        public Melee(List<string> log) => _log = log;

        public void OnStanceChanged(uint creature) => _log.Add($"OnStanceChanged:{creature}");

        public void OnBeforeDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType) =>
            _log.Add($"PrepareDirectDamage:{attacker}:{defender}:{deliveryType}");
    }

    /// <summary>Stands in for <c>CombatAbilityImpactService</c>: the stamina drain and the hostile party buff.</summary>
    public sealed class Impact : IStatusEffectAppliedListener
    {
        private readonly List<string> _log;

        public Impact(List<string> log) => _log = log;

        public void OnStatusApplied(uint source, uint creature, StatusEffectCategory categories) =>
            _log.Add($"StaminaDrain:{source}:{creature}:{categories}");

        public void OnHostileDebuffApplied(uint source, AbilityDetail ability) =>
            _log.Add($"HostileBuff:{source}:{ability?.Name}:0:True");

        public void OnApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability)
        {
        }
    }

    /// <summary>Stands in for <c>CapstoneProofService</c>: the proof recorded when an application completes.</summary>
    public sealed class Capstone : IStatusEffectAppliedListener
    {
        private readonly List<string> _log;

        public Capstone(List<string> log) => _log = log;

        public void OnStatusApplied(uint source, uint creature, StatusEffectCategory categories)
        {
        }

        public void OnHostileDebuffApplied(uint source, AbilityDetail ability)
        {
        }

        public void OnApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability) =>
            _log.Add($"CapstoneProof:{source}:{creature}:{statusEffectType.Name}:{ability?.Name}");
    }
}
