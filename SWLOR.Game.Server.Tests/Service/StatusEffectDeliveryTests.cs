using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectDeliveryTests
{
    [Test]
    public void LegacyDamageCallbacks_OnlyReceiveDirectDamage()
    {
        var effect = new LegacyDamageCallbackStatusEffect();

        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Direct);
        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered);
        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);

        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Direct);
        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered);
        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);

        effect.LegacyDamageDealtCalls.Should().Be(1);
        effect.LegacyDamageTakenCalls.Should().Be(1);
    }

    private sealed class LegacyDamageCallbackStatusEffect : StatusEffectBase
    {
        public override string Name => "Legacy Damage Callback";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public int LegacyDamageDealtCalls { get; private set; }
        public int LegacyDamageTakenCalls { get; private set; }

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            LegacyDamageDealtCalls++;
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            LegacyDamageTakenCalls++;
        }
    }
}
