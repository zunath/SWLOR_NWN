using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum.Area;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>
/// A flat instanced corridor with its spawn tables cleared, so nothing but the scenario's own creatures acts in it.
/// The runner's default arena is a copy of the module starting area, whose placed creatures may fight, heal and roll
/// dice beside the scenario. Disposing destroys every object the scenario spawned.
/// </summary>
public sealed class ScenarioArena : IDisposable
{
    private const string CorridorAreaResref = "tat_anc_flatlnd2";

    private readonly IDisposable _cancellation;

    /// <summary>The scenario's own context: spawn, equip and seed through this one.</summary>
    public EngineTestContext Context { get; }

    public uint Area { get; }

    private ScenarioArena(uint area, EngineTestContext context, IDisposable cancellation)
    {
        Area = area;
        Context = context;
        _cancellation = cancellation;
    }

    public static async Task<ScenarioArena> CreateAsync(EngineTestContext parent)
    {
        var area = parent.CreateInstancedArea(CorridorAreaResref);
        // Spawn initialization runs when the creating script yields, so the tables are cleared first.
        SetLocalString(area, "CREATURE_SPAWN_TABLE_ID", string.Empty);
        SetLocalString(area, "RESOURCE_SPAWN_TABLE_ID", string.Empty);
        await parent.DelaySecondsAsync(1f);

        var center = new NumericsVector3(GetAreaSize(Dimension.Width, area) * 5f, GetAreaSize(Dimension.Height, area) * 5f, 0);
        center.Z = GetGroundHeight(Location(area, center, 0));
        var context = new EngineTestContext(parent.TestName, area, Location(area, center - new NumericsVector3(2, 0, 0), 0));
        var cancellation = parent.CancellationToken.Register(context.CancelTest);
        return new ScenarioArena(area, context, cancellation);
    }

    /// <summary>The ids of the creatures standing in the area (the scenario records how many, to prove it is quiet).</summary>
    public int CountCreatures()
    {
        var count = 0;
        for (var obj = GetFirstObjectInArea(Area); GetIsObjectValid(obj); obj = GetNextObjectInArea(Area))
        {
            if (GetObjectType(obj) == SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature)
                count++;
        }

        return count;
    }

    public void Dispose()
    {
        _cancellation.Dispose();
        Context.Cleanup();
    }
}
