#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The beast mastery instance with the calls into services that are not converted yet answered by the test. The seams of a part that has left
/// <see cref="BeastMasteryService"/> are on that part's double; <c>TestBeastMastery.Shared.cs</c> holds the recording list and the perk read the
/// parts share. This file is the stay-part: the catalog, the XP and the stat application.
/// <para>
/// <see cref="Beasts"/>, <see cref="Spawn"/>, <see cref="Events"/> and <see cref="Incubation"/> are the part accessors of the split (BM-1 to BM-4):
/// a test calls a member on the accessor of the part that owns it, so the pull request that moves a part changes what the accessor returns and
/// no test. <see cref="Spawn"/>, <see cref="Events"/> and <see cref="Incubation"/> are the real <see cref="BeastSpawnService"/>,
/// <see cref="BeastEventService"/> and <see cref="BeastIncubationService"/> (<see cref="TestBeastSpawnService"/>,
/// <see cref="TestBeastEventService"/>, <see cref="TestBeastIncubationService"/>), built over this instance and its recording list.
/// </para>
/// </summary>
public sealed partial class TestBeastMastery : BeastMasteryService
{
    public TestBeastMastery(IDBService db, IIncubationFieldNoteService notes, IPropertyService property)
        : base(db, FacadeForwarder.For<IGuiService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IResistanceService>(), FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<ICombatDamageCatalogService>())
    {
        Spawn = new TestBeastSpawnService(this, db);
        Events = new TestBeastEventService(this, db);
        Incubation = new TestBeastIncubationService(this, db, property, notes);
    }

    /// <summary>BM-1, the stay-part: the catalog, IDs, XP and the stat application.</summary>
    public TestBeastMastery Beasts => this;

    /// <summary>BM-2: calling a beast.</summary>
    public TestBeastSpawnService Spawn { get; }

    /// <summary>BM-3: the beast event handlers, the acquire handler and the stables menu.</summary>
    public TestBeastEventService Events { get; }

    /// <summary>BM-4: the incubator, mutation, eggs and DNA.</summary>
    public TestBeastIncubationService Incubation { get; }

    public IEnumerable<Type>? Definitions { get; set; }
    public Dictionary<(uint Creature, StatType Stat), int> StatAdjustments { get; } = new();
    public Dictionary<uint, NPCStats> NpcStats { get; } = new();

    public IEnumerable<Type> DefaultDefinitions() => base.GetBeastListDefinitionTypes();

    protected override IEnumerable<Type> GetBeastListDefinitionTypes() => Definitions ?? base.GetBeastListDefinitionTypes();

    protected override int GetStatAdjustment(uint creature, StatType stat) =>
        StatAdjustments.GetValueOrDefault((creature, stat));

    protected override void PublishBeastGainXPRefresh(uint player) => Calls.Add($"Refresh {player}");

    protected override void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints) =>
        Calls.Add($"SetNPCMaxHitPoints {creature} {desiredMaxHitPoints}");

    protected override NPCStats GetNPCStats(uint npc) => NpcStats.GetValueOrDefault(npc) ?? new NPCStats();
}
