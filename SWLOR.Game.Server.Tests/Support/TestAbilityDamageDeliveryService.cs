using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The damage delivery of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 13) over the bench's
/// status effect and recorder stand-ins and the Dark Force part, with its calls into the combat services answered by the
/// bench, as <see cref="TestAbilityService"/> answered them before the move.
/// </summary>
public sealed class TestAbilityDamageDeliveryService : AbilityDamageDeliveryService
{
    private readonly AbilityBench _bench;

    public TestAbilityDamageDeliveryService(AbilityBench bench, IStatusEffectCombatEventService statusEffectCombatEvents, ICombatRecorderService combatRecorder, IAbilityDarkForceService darkForce)
        : base(statusEffectCombatEvents, combatRecorder, darkForce, FacadeForwarder.For<ICombatFeedbackService>(), FacadeForwarder.For<ICombatRepeatedTargetService>(),
            FacadeForwarder.For<ICombatHitEffectsService>(), FacadeForwarder.For<ICombatDamageReflectionService>(), FacadeForwarder.For<ICombatAbilityBonusService>())
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    protected override int ConsumeStatusAppliedNextAttackDamageBonus(uint creature)
    {
        Log("ConsumeStatusAppliedBonus");
        return _bench.StatusAppliedBonus;
    }

    protected override void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage) =>
        Log($"TempHpFeedback:{attacker}:{defender}:{damage}");

    protected override void ConsumeSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType, int damage) =>
        Log($"ConsumeSameTargetPressure:{damage}");

    protected override void ApplyDamageDealtEffects(uint attacker, uint defender, int damage, SkillType skillType, CombatDamageType damageType, bool isAbilityDamage, bool allowsHitProcs) =>
        Log($"DamageDealtEffects:{damage}:{isAbilityDamage}:{allowsHitProcs}");

    protected override void ApplyDamageReflectionEffects(uint attacker, uint defender, int damage, CombatDamageType damageType) =>
        Log($"DamageReflection:{attacker}:{defender}:{damage}:{damageType}");
}
