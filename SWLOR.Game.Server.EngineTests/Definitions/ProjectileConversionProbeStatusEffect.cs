using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>Test-only reaction exposes both notification timing and a visible resource change.</summary>
public sealed class ProjectileConversionProbeStatusEffect : StatusEffectBase, IPreDamageStatusEffect
{
    public override string Name => "Projectile conversion probe";
    public override EffectIconType Icon => EffectIconType.Invalid;
    public override float Frequency => 1f;
    public override bool PersistsOnLogout => false;
    public override bool SendsApplicationMessage => false;
    public override bool SendsWornOffMessage => false;
    public int Notifications { get; private set; }
    public int Damage { get; private set; }
    public long NotifiedAt { get; private set; }
    public int DirectNotifications { get; private set; }
    public int DirectDamage { get; private set; }
    public long DirectNotifiedAt { get; private set; }
    public int PreDamageNotifications { get; private set; }
    public long PreDamageAt { get; private set; }

    public void OnBeforeDamageTaken(uint defender, uint attacker, int damage,
        CombatDamageType damageType, CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
    {
        PreDamageNotifications++;
        PreDamageAt = Environment.TickCount64;
    }

    protected override void OnDamageTaken(uint defender, uint attacker, int damage,
        CombatDamageType damageType, CombatDamageDeliveryType deliveryType)
    {
        if (deliveryType == CombatDamageDeliveryType.Direct)
        {
            DirectNotifications++;
            DirectDamage += damage;
            DirectNotifiedAt = Environment.TickCount64;
            return;
        }
        if (damageType != CombatDamageType.Force || deliveryType != CombatDamageDeliveryType.Triggered) return;
        Notifications++;
        Damage += damage;
        NotifiedAt = Environment.TickCount64;
        Stat.ReduceFP(defender, 1);
    }
}
