#nullable enable
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class TestDeath : DeathService
{
    public TestDeath(
        IDBService db,
        INamedAnimationService animation,
        IMessagingService messaging,
        IPlayerNameService names,
        TestDeathRespawn respawn)
        : base(db, animation, messaging, names, FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<IStatusEffectApplicationService>(), FacadeForwarder.For<ICombatRecorderService>(), FacadeForwarder.For<ICombatRecordDamageService>(),
            Array.Empty<ICreatureRevivedListener>(), FacadeForwarder.For<ICombatDamageTakenService>(), respawn)
    {
        _respawn = respawn;
    }

    public List<string> Calls { get; } = new();
    private readonly TestDeathRespawn _respawn;
    public List<(LogGroup Group, string Details)> Logs => _respawn.Logs;
    public List<TestWaiver> Waivers { get; } = new();
    public bool PreventFatal { get; set; }
    public bool Recording { get; set; }

    protected override bool TryPreventFatalDamageAndGrantTemporaryHP(uint defender, int damage, bool restoreToOneHP)
    {
        Calls.Add($"TryPreventFatalDamage:{defender}:{damage}:{restoreToOneHP}");
        return PreventFatal;
    }

    protected override void ClearQueuedAbility(uint player) => Calls.Add($"ClearQueuedAbility:{player}");

    protected override void RecordPlayerDeath(uint deceased, uint killer) => Calls.Add($"RecordPlayerDeath:{deceased}:{killer}");

    protected override void ClearStatusEffectsOnDeath(uint player) => Calls.Add($"ClearStatusEffectsOnDeath:{player}");

    protected override List<IDeathDebtWaiverStatusEffect> GetDeathDebtWaiverEffects(uint player)
    {
        Calls.Add("RecordDeathDebtWaiver");
        return Waivers.Select(waiver => waiver.Effect).ToList();
    }

    protected override bool IsRecording() => Recording;

    protected override void RecordSubdual(uint subdued, uint subduedBy) => Calls.Add($"RecordSubdual:{subdued}:{subduedBy}");

    protected override void RecordRespawn(uint player) => Calls.Add($"RecordRespawn:{player}");

    protected override IDisposable BeginRespawnAttribution()
    {
        Calls.Add("BeginRespawnAttribution");
        return new DeathScope(() => Calls.Add("EndRespawnAttribution"));
    }

    protected override void RestoreGhostAfterRevive(uint creature) => Calls.Add($"RestoreGhostAfterRevive:{creature}");

    protected override void ReapplyAuraEffectsForCreature(uint creature) => Calls.Add($"ReapplyAura:{creature}");

    protected override void ApplyStatusEffect(uint source, uint creature, Type statusEffectClass, float durationSeconds) =>
        Calls.Add($"ApplyStatusEffect:{source}:{creature}:{statusEffectClass.Name}:{durationSeconds}");

}
