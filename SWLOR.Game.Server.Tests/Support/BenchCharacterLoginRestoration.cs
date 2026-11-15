#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A <see cref="CharacterLoginRestoration"/> whose five restoration calls only record themselves in the given steps
/// (shared with <see cref="BenchMigrationService"/>), so a test pins the order they run in.
/// </summary>
public sealed class BenchCharacterLoginRestoration : CharacterLoginRestoration
{
    private readonly List<string> _steps;

    public BenchCharacterLoginRestoration(IMigrationService migration, InMemoryDBService db, List<string> steps)
        : base(migration, new OverflowStorageService(db), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>())
    {
        _steps = steps;
    }

    protected override void RestorePlayerAttributes(uint player) => _steps.Add("attributes");

    protected override void RestorePlayerFeats(uint player) => _steps.Add("feats");

    protected override void RefreshCombatReadinessEquipment(uint player) => _steps.Add("readiness");

    protected override void RefreshPlayerMaxHP(uint player) => _steps.Add("maxhp");

    protected override void RefreshPlayerEquipmentStats(uint player) => _steps.Add("equipment");
}
