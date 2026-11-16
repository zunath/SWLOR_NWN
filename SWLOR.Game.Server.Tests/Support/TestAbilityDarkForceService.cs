using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The Dark Force conversion and restoration of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 14)
/// over the bench's impact state store and recorder stand-in, with its calls into the stat and combat resource services
/// answered by the bench, as <see cref="TestAbilityService"/> answered them before the move.
/// </summary>
public sealed class TestAbilityDarkForceService : AbilityDarkForceService
{
    private readonly AbilityBench _bench;

    public TestAbilityDarkForceService(AbilityBench bench, IAbilityImpactState impactState, ICombatRecorderService combatRecorder)
        : base(impactState, combatRecorder, FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ICombatResourcesService>())
    {
        _bench = bench;
    }

    // The damage inputs, the activation checks and the aura part answer the same read through their own doubles.
    protected override int GetStatAdjustment(uint creature, StatType stat) => _bench.Stats.GetValueOrDefault((creature, stat));

    protected override int RestoreFP(uint creature, int amount)
    {
        _bench.Log.Add($"RestoreFP:{creature}:{amount}");
        return amount;
    }

    protected override int ApplyDamageDerivedHealing(uint creature, int damage, int percent)
    {
        _bench.Log.Add($"DamageHealing:{creature}:{damage}:{percent}");
        return damage;
    }
}
