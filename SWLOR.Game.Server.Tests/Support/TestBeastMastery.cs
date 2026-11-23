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
/// The beast mastery instance with the calls into services that are not converted yet answered by the test. The seams are in partial files by the
/// part whose code calls them (<c>TestBeastMastery.Events.cs</c>; <c>.Shared.cs</c> holds the seams several parts call: the perk read, the feat
/// sync and the window), so a part that leaves <see cref="BeastMasteryService"/> takes its file with it. This file is the stay-part: the catalog, the XP
/// and the stat application.
/// <para>
/// <see cref="Beasts"/>, <see cref="Spawn"/>, <see cref="Events"/> and <see cref="Incubation"/> are the part accessors of the split (BM-1 to BM-4):
/// each is the one instance until its part moves, and a test calls a member on the accessor of the part that owns it, so the pull request that
/// moves the part changes what the accessor returns and no test.
/// </para>
/// </summary>
public sealed partial class TestBeastMastery : BeastMasteryService
{
    public TestBeastMastery(IDBService db, IIncubationFieldNoteService notes, IPropertyService property)
        : base(db, notes, property, FacadeForwarder.For<IAIService>(), FacadeForwarder.For<ICombatRecorderService>(), FacadeForwarder.For<ICompanionControlService>(), FacadeForwarder.For<IGuiService>(), FacadeForwarder.For<IItemService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IResistanceService>(), FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<IStatusEffectApplicationService>(), FacadeForwarder.For<ICombatDamageCatalogService>())
    {
    }

    /// <summary>BM-1, the stay-part: the catalog, IDs, XP and the stat application.</summary>
    public TestBeastMastery Beasts => this;

    /// <summary>BM-2: calling a beast.</summary>
    public TestBeastMastery Spawn => this;

    /// <summary>BM-3: the beast event handlers, the acquire handler and the stables menu.</summary>
    public TestBeastMastery Events => this;

    /// <summary>BM-4: the incubator, mutation, eggs and DNA.</summary>
    public TestBeastMastery Incubation => this;

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
