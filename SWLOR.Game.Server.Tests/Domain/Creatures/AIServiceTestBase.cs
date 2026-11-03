#nullable enable
using NUnit.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The world every AI instance test starts from: one area, a creature with the AI enabled that is OBJECT_SELF, and
/// the helpers that place a hostile player or creature next to it. The instance under test is
/// <see cref="Bench"/>.<see cref="AIBench.Service"/>. The tests share the profile builder, so they do not run in parallel.
/// </summary>
[NonParallelizable]
public abstract class AIServiceTestBase
{
    protected const uint Invalid = AIBench.Invalid;

    protected AIBench Bench = null!;
    protected uint Area;
    protected AISim Npc = null!;

    protected TestAIService Service => Bench.Service;

    [SetUp]
    public void CreateTheWorld()
    {
        Bench = new AIBench();
        Area = Bench.AddArea("fight");
        Npc = Bench.AddCreature("npc", Area);
        Bench.SetHome(Npc.Id, Area);
        Bench.Self = Npc.Id;
        ScriptedProfiles.Build = null;
        Service.Definitions.Add(typeof(ScriptedProfiles));
        Service.CacheProfiles();
    }

    [TearDown]
    public void DisposeTheWorld()
    {
        ScriptedProfiles.Build = null;
        RealEnmityTables.Clear();
        Bench.Dispose();
    }

    /// <summary>Loads the profiles the builder function makes; the required profiles it leaves out are added.</summary>
    protected void UseProfiles(Func<AIProfileBuilder, AIProfileBuilder> build)
    {
        ScriptedProfiles.Build = build;
        Service.CacheProfiles();
        Service.ClearAIState(Npc.Id);
    }

    /// <summary>Sets the AI flags of the creature the way the spawn system does.</summary>
    protected void Flags(AIFlag flags) => Service.SetAIFlag(Npc.Id, flags);

    /// <summary>Adds a player who is hostile to the NPC.</summary>
    protected AISim HostilePlayer(string name, float x = 0f, uint? area = null)
    {
        var player = Bench.AddPlayer(name, area ?? Area, x);
        Bench.MakeEnemies(player.Id, Npc.Id);
        return player;
    }

    /// <summary>The note and engine-call text since the test began, restricted to the engine members given.</summary>
    protected List<string> Trace(params string[] members) => Bench.Trace(members);
}
