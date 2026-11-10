using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The recast instance: the group name caches, and the cooldown deadlines of players (saved on the player record, shown
/// through the cooldown visual) and of NPCs (kept in a local string). The clock is held, the engine is a recording fake
/// with real local variables, and the database is an in-memory one.
/// </summary>
[TestFixture]
public class RecastServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Npc = 2001;
    private const string PlayerId = "uuid-1001";

    private static readonly DateTime Now = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _visual = null!;
    private RecastService _recast = null!;
    private IDisposable _clock = null!;
    private IDisposable _scope = null!;
    private readonly List<string> _visualCalls = new();
    private bool _isDm;
    private bool _possessed;

    [SetUp]
    public void SetUp()
    {
        _visualCalls.Clear();
        _isDm = false;
        _possessed = false;
        _clock = Clock.OverrideForTests(() => Now);
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! == Player);
        _engine.SetReturnHandler("GetIsDM", _ => _isDm);
        _engine.SetReturnHandler("GetIsDMPossessed", _ => _possessed);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _scope = NWScriptRuntime.Override(_engine);

        _db = new InMemoryDBService().With(new Player { Id = PlayerId });
        var visual = ScriptedProxy.Create<IAbilityCooldownVisualService>(out _visual);
        _visual.On("ApplyRecastDelay", _ => Record("Apply"));
        _visual.On("ClearRecastDelay", _ => Record("Clear"));
        _visual.On("RefreshRecastDelay", _ => Record("Refresh"));
        _recast = new RecastService(_db, visual);
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _clock.Dispose();
    }

    private object? Record(string call)
    {
        _visualCalls.Add(call);
        return null;
    }

    private Player Record_Player => _db.Get<Player>(PlayerId);

    private static void SetLocal(uint target, string name, string value) => NWScript.SetLocalString(target, name, value);

    private static string Local(RecastGroup group) => $"ABILITY_RECAST_ID_{(int)group}";

    // ---- The name caches -------------------------------------------------------------------------------------------

    [Test]
    public void NamesAndVisibility_AreReadFromTheEnumerationAfterTheCacheHandler()
    {
        _recast.CacheRecastGroups();

        _recast.GetRecastGroupName(RecastGroup.FortressStrike).Should().Be("Fortress Str.");
        _recast.GetRecastGroupDisplayName(RecastGroup.FortressStrike).Should().Be("Fortress Strike");
        _recast.IsRecastGroupVisible(RecastGroup.FortressStrike).Should().BeTrue();
        _recast.IsRecastGroupVisible(RecastGroup.Invalid).Should().BeFalse();
    }

    [Test]
    public void NamesAreNotKnownBeforeTheCacheHandlerRuns_AndEachCacheStartsFromScratch()
    {
        var name = () => _recast.GetRecastGroupName(RecastGroup.Flash);
        var display = () => _recast.GetRecastGroupDisplayName(RecastGroup.Flash);
        name.Should().Throw<KeyNotFoundException>();
        display.Should().Throw<KeyNotFoundException>();

        _recast.CacheRecastGroups();
        _recast.CacheRecastGroups();

        _recast.GetRecastGroupName(RecastGroup.Flash).Should().Be("Flash");
    }

    [Test]
    public void TheCachesAreTheInstancesOwn()
    {
        _recast.CacheRecastGroups();
        var other = new RecastService(_db, ScriptedProxy.Create<IAbilityCooldownVisualService>(out _));

        var name = () => other.GetRecastGroupName(RecastGroup.Flash);

        name.Should().Throw<KeyNotFoundException>();
        other.IsRecastGroupVisible(RecastGroup.Flash).Should().BeFalse();
    }

    // ---- Is a creature on a recast delay ---------------------------------------------------------------------------

    [Test]
    public void IsOnRecastDelay_NeverBlocksADM()
    {
        _isDm = true;

        _recast.IsOnRecastDelay(Player, RecastGroup.Flash).Should().Be((false, string.Empty));
        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void IsOnRecastDelay_CreatesTheMissingTableOfAPlayerRecord()
    {
        Record_Player.RecastTimes = null!;

        _recast.IsOnRecastDelay(Player, RecastGroup.Flash).Should().Be((false, string.Empty));

        Record_Player.RecastTimes.Should().NotBeNull().And.BeEmpty();
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void IsOnRecastDelay_AnswersFalseForAGroupThePlayerNeverUsed()
    {
        _recast.IsOnRecastDelay(Player, RecastGroup.Flash).Should().Be((false, string.Empty));
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void IsOnRecastDelay_ReportsAPlayersRemainingTime()
    {
        Record_Player.RecastTimes[RecastGroup.Flash] = Now.AddSeconds(90);
        Record_Player.RecastTimes[RecastGroup.Rest] = Now.AddSeconds(-30);

        _recast.IsOnRecastDelay(Player, RecastGroup.Flash).Should().Be((true, "1 minute, 30 seconds"));
        var expired = _recast.IsOnRecastDelay(Player, RecastGroup.Rest);
        expired.Item1.Should().BeFalse();
    }

    [Test]
    public void IsOnRecastDelay_ReadsTheLocalStringOfAnNpcAndOfADMPossessedPlayer()
    {
        SetLocal(Npc, Local(RecastGroup.Flash), RecastTimestamp.Format(Now.AddSeconds(45)));
        _recast.IsOnRecastDelay(Npc, RecastGroup.Flash).Should().Be((true, "45 seconds"));
        _recast.IsOnRecastDelay(Npc, RecastGroup.Rest).Should().Be((false, string.Empty));

        _possessed = true;
        SetLocal(Player, Local(RecastGroup.Flash), RecastTimestamp.Format(Now.AddSeconds(10)));
        _recast.IsOnRecastDelay(Player, RecastGroup.Flash).Should().Be((true, "10 seconds"));
        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void IsOnRecastDelay_DropsAMalformedLocalOfAnNpc()
    {
        SetLocal(Npc, Local(RecastGroup.Flash), "not a time");

        _recast.IsOnRecastDelay(Npc, RecastGroup.Flash).Should().Be((false, string.Empty));

        _locals.Has(Npc, Local(RecastGroup.Flash)).Should().BeFalse();
    }

    // ---- Applying a delay ------------------------------------------------------------------------------------------

    [Test]
    public void ApplyRecastDelay_ByDuration_IgnoresInvalidArguments()
    {
        _recast.ApplyRecastDelay(ObjectInvalid, RecastGroup.Flash, 5f);
        _recast.ApplyRecastDelay(Player, RecastGroup.Invalid, 5f);
        _recast.ApplyRecastDelay(Player, RecastGroup.Flash, 0f);

        _db.Saved.Should().BeEmpty();
        _visualCalls.Should().BeEmpty();
    }

    [Test]
    public void ApplyRecastDelay_ByDuration_SavesThePlayersDeadlineAndShowsTheVisual()
    {
        Record_Player.RecastTimes = null!;

        _recast.ApplyRecastDelay(Player, RecastGroup.Flash, 20f);

        Record_Player.RecastTimes[RecastGroup.Flash].Should().Be(Now.AddSeconds(20));
        _db.Saved.Should().ContainSingle();
        _visualCalls.Should().Equal("Apply");
    }

    [Test]
    public void ApplyRecastDelay_WithADeadline_PassesTheTexturesToTheVisual()
    {
        _recast.ApplyRecastDelay(Player, RecastGroup.Flash, Now, Now.AddSeconds(8), "source", "icon");

        var call = _visual.Calls.Should().ContainSingle().Subject;
        call.Member.Should().Be("ApplyRecastDelay");
        call.Arguments.Should().Equal(Player, RecastGroup.Flash, Now, Now.AddSeconds(8), "source", "icon");
    }

    [Test]
    public void ApplyRecastDelay_WithADeadline_IgnoresDeadlinesInThePastOrBeforeTheStart()
    {
        _recast.ApplyRecastDelay(Player, RecastGroup.Flash, Now.AddSeconds(-10), Now.AddSeconds(-1));
        _recast.ApplyRecastDelay(Player, RecastGroup.Flash, Now.AddSeconds(10), Now.AddSeconds(5));

        _db.Saved.Should().BeEmpty();
        _visualCalls.Should().BeEmpty();
    }

    [Test]
    public void ApplyRecastDelay_StoresAnNpcsDeadlineInALocalString()
    {
        _recast.ApplyRecastDelay(Npc, RecastGroup.Flash, 30f);

        _locals.String(Npc, Local(RecastGroup.Flash)).Should().Be(RecastTimestamp.Format(Now.AddSeconds(30)));
        _db.Saved.Should().BeEmpty();
        _visualCalls.Should().BeEmpty();
    }

    [Test]
    public void ApplyRecastDelay_TouchesNothingForADM()
    {
        _isDm = true;

        _recast.ApplyRecastDelay(Player, RecastGroup.Flash, 30f);

        _db.Saved.Should().BeEmpty();
        _visualCalls.Should().BeEmpty();
    }

    // ---- Reducing a delay ------------------------------------------------------------------------------------------

    [Test]
    public void ReduceRecastDelay_IgnoresInvalidArgumentsAndTheCapstoneGroup()
    {
        Record_Player.RecastTimes[RecastGroup.Capstone] = Now.AddSeconds(100);

        _recast.ReduceRecastDelay(ObjectInvalid, RecastGroup.Flash, 5f);
        _recast.ReduceRecastDelay(Player, RecastGroup.Invalid, 5f);
        _recast.ReduceRecastDelay(Player, RecastGroup.Flash, 0f);
        _recast.ReduceRecastDelay(Player, RecastGroup.Capstone, 50f);

        Record_Player.RecastTimes[RecastGroup.Capstone].Should().Be(Now.AddSeconds(100));
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void ReduceRecastDelay_ShortensAPlayersDeadlineAndRefreshesTheVisual()
    {
        Record_Player.RecastTimes[RecastGroup.Flash] = Now.AddSeconds(60);

        _recast.ReduceRecastDelay(Player, RecastGroup.Flash, 20f);

        Record_Player.RecastTimes[RecastGroup.Flash].Should().Be(Now.AddSeconds(40));
        _visualCalls.Should().Equal("Refresh");
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void ReduceRecastDelay_RemovesAPlayersDeadlineThatRunsOut()
    {
        Record_Player.RecastTimes[RecastGroup.Flash] = Now.AddSeconds(10);

        _recast.ReduceRecastDelay(Player, RecastGroup.Flash, 20f);

        Record_Player.RecastTimes.Should().NotContainKey(RecastGroup.Flash);
        _visualCalls.Should().Equal("Clear");
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void ReduceRecastDelay_RemovesAPlayersDeadlineThatAlreadyPassed()
    {
        Record_Player.RecastTimes[RecastGroup.Flash] = Now.AddSeconds(-5);

        _recast.ReduceRecastDelay(Player, RecastGroup.Flash, 1f);

        Record_Player.RecastTimes.Should().NotContainKey(RecastGroup.Flash);
        _visualCalls.Should().Equal("Clear");
    }

    [Test]
    public void ReduceRecastDelay_LeavesAPlayerWithoutThatGroupAndCreatesAMissingTable()
    {
        _recast.ReduceRecastDelay(Player, RecastGroup.Flash, 5f);
        _db.Saved.Should().BeEmpty();

        Record_Player.RecastTimes = null!;
        _recast.ReduceRecastDelay(Player, RecastGroup.Flash, 5f);

        Record_Player.RecastTimes.Should().NotBeNull().And.BeEmpty();
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void ReduceRecastDelay_TouchesNothingForADM()
    {
        _isDm = true;
        Record_Player.RecastTimes[RecastGroup.Flash] = Now.AddSeconds(60);

        _recast.ReduceRecastDelay(Player, RecastGroup.Flash, 20f);

        Record_Player.RecastTimes[RecastGroup.Flash].Should().Be(Now.AddSeconds(60));
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void ReduceRecastDelay_ShortensAnNpcsLocalDeadline()
    {
        SetLocal(Npc, Local(RecastGroup.Flash), RecastTimestamp.Format(Now.AddSeconds(60)));

        _recast.ReduceRecastDelay(Npc, RecastGroup.Flash, 20f);

        _locals.String(Npc, Local(RecastGroup.Flash)).Should().Be(RecastTimestamp.Format(Now.AddSeconds(40)));
    }

    [Test]
    public void ReduceRecastDelay_ClearsAnNpcsLocalDeadlineThatRunsOutOrPassed()
    {
        SetLocal(Npc, Local(RecastGroup.Flash), RecastTimestamp.Format(Now.AddSeconds(5)));
        _recast.ReduceRecastDelay(Npc, RecastGroup.Flash, 20f);
        _locals.Has(Npc, Local(RecastGroup.Flash)).Should().BeFalse();

        SetLocal(Npc, Local(RecastGroup.Flash), RecastTimestamp.Format(Now.AddSeconds(-5)));
        _recast.ReduceRecastDelay(Npc, RecastGroup.Flash, 20f);
        _locals.Has(Npc, Local(RecastGroup.Flash)).Should().BeFalse();
    }

    [Test]
    public void ReduceRecastDelay_DoesNothingForAnNpcWithoutADeadline()
    {
        _recast.ReduceRecastDelay(Npc, RecastGroup.Flash, 20f);

        _engine.CountOf("SetLocalString").Should().Be(0);
        _engine.CountOf("DeleteLocalString").Should().Be(0);
    }
}
