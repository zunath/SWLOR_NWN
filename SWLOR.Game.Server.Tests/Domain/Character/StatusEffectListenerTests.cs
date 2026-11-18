using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The melee and applied listeners (P5-5d): who is told, in what order, and that registering twice keeps one entry. The bench
/// registers its three stand-ins in the order the boot handlers register the real implementers (melee, impact, capstone).
/// </summary>
[TestFixture]
public class StatusEffectListenerTests : StatusEffectServiceTestBase
{
    private sealed class Tagged : IStatusEffectMeleeListener, IStatusEffectAppliedListener
    {
        private readonly List<string> _log;

        public Tagged(List<string> log) => _log = log;

        public void OnStanceChanged(uint creature) => _log.Add("Tagged:Stance");

        public void OnBeforeDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType) => _log.Add("Tagged:Damage");

        public void OnStatusApplied(uint source, uint creature, StatusEffectCategory categories) => _log.Add("Tagged:Applied");

        public void OnHostileDebuffApplied(uint source, AbilityDetail ability) => _log.Add("Tagged:Hostile");

        public void OnApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability) => _log.Add("Tagged:Completed");
    }

    [Test]
    public void AnApplicationTellsEveryAppliedListenerInRegistrationOrderAtTheSamePointsAsBefore()
    {
        Bench.Hostile.Add((Target, Caster));
        Bench.ActiveImpact = new AbilityImpactSummary { Ability = new AbilityDetail { Name = "Active" } };
        var tagged = new Tagged(Log);
        Service.RegisterAppliedListener(tagged);
        Service.RegisterAppliedListener(tagged);
        var effect = Probe<ProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.Debuff);

        Application.ApplyStatusEffect(Caster, Target, effect, 10f).Should().BeTrue();

        Log.Where(line => line.Contains("StaminaDrain") || line.Contains("HostileBuff") || line.Contains("CapstoneProof") || line.StartsWith("Tagged") ||
                          line.StartsWith("Refresh") || line.StartsWith("Nearby"))
            .Should().Equal(
                $"StaminaDrain:{Caster}:{Target}:{StatusEffectCategory.Debuff}",
                "Tagged:Applied",
                $"HostileBuff:{Caster}:Active:0:True",
                "Tagged:Hostile",
                $"Refresh:{Target}:StatusEffectReceivedRefreshEvent",
                $"Nearby:{Target}:name-{Target} receives the effect of Probe from name-{Caster}",
                $"CapstoneProof:{Caster}:{Target}:ProbeEffect:Active",
                "Tagged:Completed");
    }

    [Test]
    public void ARegisteredMeleeListenerIsKeptOnceAndTold_AfterTheStandIn()
    {
        var tagged = new Tagged(Log);
        Service.RegisterMeleeListener(tagged);
        Service.RegisterMeleeListener(tagged);
        Applied<ProbeEffect>(Target, Target);

        CombatEvents.NotifyPreDamageStatusEffects(Caster, Target, 7, CombatDamageType.Fire, CombatDamageDeliveryType.Direct);

        Log.Should().Equal(
            $"PrepareDirectDamage:{Caster}:{Target}:{CombatDamageDeliveryType.Direct}",
            "Tagged:Damage");
    }
}
