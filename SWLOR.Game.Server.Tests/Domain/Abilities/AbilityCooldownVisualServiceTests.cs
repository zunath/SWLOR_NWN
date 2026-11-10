using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The recharge-frame instance: the texture names, the stage maths, the texture cache built from the ability registry,
/// the live recharges with their scheduled updates, and the handlers. The engine is a recording fake whose delayed
/// commands the tests run by hand, the clock is overridden, the database is an in-memory one, and the unconverted
/// <c>Ability</c> registry and <c>Gui</c> refresh are answered by a subclass.
/// </summary>
[TestFixture]
public class AbilityCooldownVisualServiceTests
{
    private const uint Player = 1001;
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Module = 9;
    private const string PlayerId = "uuid-1001";

    private static readonly DateTime T0 = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private InMemoryDBService _db = null!;
    private TestVisual _visual = null!;
    private readonly List<uint> _refreshes = new();
    private DateTime _now;
    private bool _isDm;
    private bool _valid;
    private string _uuid = PlayerId;
    private readonly Dictionary<int, string> _icons = new();

    [SetUp]
    public void SetUp()
    {
        _now = T0;
        _refreshes.Clear();
        _isDm = false;
        _valid = true;
        _uuid = PlayerId;
        _icons.Clear();

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && ((uint)args[0]! != Player || _valid));
        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturnHandler("GetIsDM", _ => _isDm);
        _engine.SetReturn("GetIsDMPossessed", false);
        _engine.SetReturnHandler("GetObjectUUID", _ => _uuid);
        _engine.SetReturn("GetModule", Module);
        _engine.SetReturn("GetEnteringObject", Player);
        _engine.SetReturn("GetExitingObject", Player);
        _engine.SetReturnHandler("Get2DAString", args => _icons.GetValueOrDefault((int)args[2]!, "****"));
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(Clock.OverrideForTests(() => _now));

        _db = new InMemoryDBService().With(new Player { Id = PlayerId });
        _visual = new TestVisual(_db, ScriptedGui(_refreshes));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private static AbilityDetail Ability(RecastGroup group, float delaySeconds = 30f) => new()
    {
        RecastGroup = group,
        RecastDelay = _ => delaySeconds
    };

    private List<string> Overrides() =>
        _engine.CallsTo("SetTextureOverride").Select(c => $"{c.Arguments[0]}>{c.Arguments[1]}").ToList();

    /// <summary>Runs the update the service last scheduled: the module command that starts the delay, then the delayed command.</summary>
    private void RunScheduledUpdate()
    {
        var assigned = _engine.CallsTo("AssignCommand").Last();
        _engine.ClearCalls();
        ((Action)assigned.Arguments[1]!)();
        var delayed = _engine.CallsTo("DelayCommand").Last();
        ((Action)delayed.Arguments[1]!)();
    }

    private void CacheTwoAbilities()
    {
        _icons[100] = "ife_alpha";
        _icons[101] = "ife_beta";
        _visual.Abilities[(FeatType)100] = Ability(RecastGroup.Earthshatter);
        _visual.Abilities[(FeatType)101] = Ability(RecastGroup.Flash);
        _visual.CacheData();
    }

    [TestCase("ife_absdef1", 0, "pr0_absdef1")]
    [TestCase("ife_absdef1", 5, "pr5_absdef1")]
    [TestCase("iit_ess2_062", 3, "pr3_ess2_062")]
    [TestCase("IFE_absdef1", 1, "pr1_absdef1")]
    [TestCase("ife_wtchflprsnc3", 5, "pr5_wtchflprsnc3")]
    [TestCase("ife_wtchflprsnc34", 5, null)]
    [TestCase("is_spell_icon", 1, null)]
    [TestCase("ife_absdef1", -1, null)]
    [TestCase("ife_absdef1", 6, null)]
    [TestCase("", 0, null)]
    [TestCase("   ", 0, null)]
    [TestCase(null, 0, null)]
    public void GetCooldownTextureName_BuildsNamesForSupportedArtworkOnly(string? source, int stage, string? expected)
    {
        _visual.GetCooldownTextureName(source!, stage).Should().Be(expected);
    }

    [Test]
    public void CalculateCooldownStage_AdvancesThroughSixFramesAndEndsAtTheDeadline()
    {
        var end = T0.AddSeconds(60);

        _visual.CalculateCooldownStage(T0, T0, end).Should().Be(0);
        _visual.CalculateCooldownStage(T0.AddSeconds(9), T0, end).Should().Be(0);
        _visual.CalculateCooldownStage(T0.AddSeconds(10), T0, end).Should().Be(1);
        _visual.CalculateCooldownStage(T0.AddSeconds(59), T0, end).Should().Be(5);
        _visual.CalculateCooldownStage(end, T0, end).Should().Be(-1);
        _visual.CalculateCooldownStage(end.AddSeconds(1), T0, end).Should().Be(-1);
        _visual.CalculateCooldownStage(T0.AddSeconds(-5), T0, end).Should().Be(0, "a clock before the start shows the first frame");
        _visual.CalculateCooldownStage(T0, end, end.AddSeconds(-1)).Should().Be(-1, "an empty or inverted window never shows");
    }

    [Test]
    public void CacheData_MapsGroupsToTheirOwnIconsAndDropsIconsSharedAcrossGroups()
    {
        _icons[100] = "ife_alpha";
        _icons[101] = "ife_shared";
        _icons[102] = "ife_shared";
        _icons[103] = "****";
        _icons[104] = "is_plain";
        _icons[105] = "ife_nodelay";
        _icons[106] = "ife_invalidgroup";
        _visual.Abilities[(FeatType)100] = Ability(RecastGroup.Earthshatter);
        _visual.Abilities[(FeatType)101] = Ability(RecastGroup.Earthshatter);
        _visual.Abilities[(FeatType)102] = Ability(RecastGroup.Flash);
        _visual.Abilities[(FeatType)103] = Ability(RecastGroup.Flash);
        _visual.Abilities[(FeatType)104] = Ability(RecastGroup.Flash);
        _visual.Abilities[(FeatType)105] = new AbilityDetail { RecastGroup = RecastGroup.Flash, RecastDelay = null };
        _visual.Abilities[(FeatType)106] = Ability(RecastGroup.Invalid);
        _visual.CacheData();

        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60));
        _visual.ApplyRecastDelay(Player, RecastGroup.Flash, T0, T0.AddSeconds(60));

        // The shared icon is removed from both groups, leaving the stance group none.
        Overrides().Should().Equal("ife_alpha>pr0_alpha");
        _visual.GetCooldownStage(Player, RecastGroup.Flash).Should().Be(0, "a group with no icon is still tracked");
    }

    [Test]
    public void CacheData_ReplacesTheEarlierCacheWhenItRunsAgain()
    {
        CacheTwoAbilities();
        _visual.Abilities.Clear();
        _icons[102] = "ife_gamma";
        _visual.Abilities[(FeatType)102] = Ability(RecastGroup.Earthshatter);

        _visual.CacheData();
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60));

        Overrides().Should().Equal("ife_gamma>pr0_gamma");
    }

    [Test]
    public void ApplyRecastDelay_ShowsTheFirstFrameOnEachCachedIconAndRefreshesTheHotbar()
    {
        CacheTwoAbilities();
        _engine.ClearCalls();

        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60));

        Overrides().Should().Equal("ife_alpha>pr0_alpha");
        _refreshes.Should().Equal(Player);
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(0);
        _engine.CountOf("AssignCommand").Should().Be(1, "the next frame is scheduled");
    }

    [Test]
    public void ApplyRecastDelay_BuildsTheCacheOnFirstUse()
    {
        _icons[100] = "ife_alpha";
        _visual.Abilities[(FeatType)100] = Ability(RecastGroup.Earthshatter);

        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60));
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60));

        Overrides().Should().Contain("ife_alpha>pr0_alpha");
        _visual.AbilityReads.Should().Be(1, "the cache is built once");
    }

    [Test]
    public void ApplyRecastDelay_IgnoresDmsInvalidGroupsAndFinishedRecharges()
    {
        CacheTwoAbilities();
        _engine.ClearCalls();

        _isDm = true;
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60));
        _isDm = false;
        _visual.ApplyRecastDelay(Player, RecastGroup.Invalid, T0, T0.AddSeconds(60));
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0.AddSeconds(-60), T0);

        _engine.Calls.Where(c => c.Member == "SetTextureOverride").Should().BeEmpty();
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
    }

    [Test]
    public void ApplyRecastDelay_UsesASuppliedAnchorTextureAndSeparateArtwork()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "anchor_tex", "ife_art");

        Overrides().Should().Equal("anchor_tex>pr0_art");
    }

    [Test]
    public void ApplyRecastDelay_UsesTheAnchorItselfWhenNoArtworkIsGiven()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");

        Overrides().Should().Equal("ife_anchor>pr0_anchor");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("this_name_is_too_long")]
    [TestCase("plain_texture")]
    public void ApplyRecastDelay_RejectsAnAnchorThatCannotBeFramed(string anchor)
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), anchor);

        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
        _engine.CountOf("SetTextureOverride").Should().Be(0);
    }

    [Test]
    public void ApplyRecastDelay_RestoresTheEarlierOverrideBeforeShowingTheNewOne()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor", "ife_art");
        _engine.ClearCalls();

        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor", "ife_art");

        Overrides().Should().Equal("ife_anchor>ife_art", "ife_anchor>pr0_art");
    }

    [Test]
    public void ScheduledUpdates_AdvanceTheFramesAndClearTheOverrideAtTheDeadline()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");

        _now = T0.AddSeconds(10);
        RunScheduledUpdate();
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(1);
        Overrides().Should().Equal("ife_anchor>pr1_anchor");

        _now = T0.AddSeconds(55);
        RunScheduledUpdate();
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(5);

        _now = T0.AddSeconds(60);
        _refreshes.Clear();
        RunScheduledUpdate();
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
        // The anchor supplies its own artwork again.
        Overrides().Should().Equal("ife_anchor>");
        _refreshes.Should().Equal(Player);
    }

    [Test]
    public void ScheduledUpdates_AreIgnoredOnceTheRechargeWasReplaced()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");
        var stale = _engine.CallsTo("AssignCommand").Last();
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(120), "ife_anchor");
        _engine.ClearCalls();

        _now = T0.AddSeconds(30);
        ((Action)stale.Arguments[1]!)();
        ((Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!)();

        _engine.CountOf("SetTextureOverride").Should().Be(0);
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(0);
    }

    [Test]
    public void ScheduledUpdates_DropTheRechargeWhenThePlayerLeftOrChangedCharacter()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");
        _uuid = "someone-else";

        _now = T0.AddSeconds(10);
        RunScheduledUpdate();

        Overrides().Should().BeEmpty("the overrides belonged to another character");
        _uuid = PlayerId;
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
    }

    [Test]
    public void ClearRecastDelay_RestoresTheArtworkAndRefreshes()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor", "ife_art");
        _engine.ClearCalls();
        _refreshes.Clear();

        _visual.ClearRecastDelay(Player, RecastGroup.Earthshatter);

        Overrides().Should().Equal("ife_anchor>ife_art");
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
        _refreshes.Should().Equal(Player);
    }

    [Test]
    public void ClearRecastDelay_ForgetsThePlayerOnceNothingIsRecharging()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");
        _visual.ApplyRecastDelay(Player, RecastGroup.Flash, T0, T0.AddSeconds(60), "ife_other");

        _visual.ClearRecastDelay(Player, RecastGroup.Earthshatter);
        _visual.GetCooldownStage(Player, RecastGroup.Flash).Should().Be(0);
        _visual.ClearRecastDelay(Player, RecastGroup.Flash);

        _visual.GetCooldownStage(Player, RecastGroup.Flash).Should().Be(-1);
    }

    [Test]
    public void ClearRecastDelay_DoesNothingForAnInvalidPlayerOrAnUnknownGroup()
    {
        _visual.ClearRecastDelay(ObjectInvalid, RecastGroup.Earthshatter);
        _visual.ClearRecastDelay(Player, RecastGroup.Earthshatter);

        _refreshes.Should().BeEmpty();
        _engine.CountOf("SetTextureOverride").Should().Be(0);
    }

    [Test]
    public void GetCooldownStage_IsMinusOneForAnInvalidPlayer()
    {
        _visual.GetCooldownStage(ObjectInvalid, RecastGroup.Earthshatter).Should().Be(-1);
    }

    [Test]
    public void ClearAllRecastDelays_RestoresEveryOverrideAndRefreshesOnce()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");
        _visual.ApplyRecastDelay(Player, RecastGroup.Flash, T0, T0.AddSeconds(60), "ife_other");
        _engine.ClearCalls();
        _refreshes.Clear();

        _visual.ClearAllRecastDelays(Player);

        Overrides().Should().BeEquivalentTo(new[] { "ife_anchor>", "ife_other>" });
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
        _refreshes.Should().Equal(Player);
    }

    [Test]
    public void ClearAllRecastDelays_DoesNothingWithoutRechargesOrWithAnInvalidPlayer()
    {
        _visual.ClearAllRecastDelays(ObjectInvalid);
        _visual.ClearAllRecastDelays(Player);

        _refreshes.Should().BeEmpty();
    }

    [Test]
    public void ClearCooldownVisualsOnExit_ClearsTheLeavingPlayer()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");

        _visual.ClearCooldownVisualsOnExit();

        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
    }

    [Test]
    public void RefreshRecastDelay_KeepsTheElapsedProgressAndTheAnchorOfAnActiveRecharge()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor", "ife_art");
        _now = T0.AddSeconds(30);
        _engine.ClearCalls();

        _visual.RefreshRecastDelay(Player, RecastGroup.Earthshatter, T0.AddSeconds(120));

        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(1, "30 of 120 seconds have passed");
        Overrides().Should().Equal("ife_anchor>ife_art", "ife_anchor>pr1_art");
    }

    [Test]
    public void RefreshRecastDelay_EstimatesTheStartFromTheLongestAbilityDelayWhenNothingIsActive()
    {
        _icons[100] = "ife_alpha";
        _visual.Abilities[(FeatType)100] = Ability(RecastGroup.Earthshatter, 60f);
        _visual.Abilities[(FeatType)101] = Ability(RecastGroup.Earthshatter, 10f);
        _visual.CacheData();
        _now = T0.AddSeconds(30);

        _visual.RefreshRecastDelay(Player, RecastGroup.Earthshatter, T0.AddSeconds(60));

        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(3, "30 of 60 seconds have passed");
    }

    [Test]
    public void RefreshRecastDelay_UsesTheRemainingTimeForAGroupWithoutAbilities()
    {
        _visual.Abilities.Clear();

        _visual.RefreshRecastDelay(Player, RecastGroup.Earthshatter, T0.AddSeconds(10));

        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(0);
    }

    [Test]
    public void RefreshRecastDelay_ClearsAFinishedRechargeAndIgnoresDmsAndInvalidGroups()
    {
        _visual.ApplyRecastDelay(Player, RecastGroup.Earthshatter, T0, T0.AddSeconds(60), "ife_anchor");

        _visual.RefreshRecastDelay(Player, RecastGroup.Invalid, T0.AddSeconds(10));
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(0);

        _isDm = true;
        _visual.RefreshRecastDelay(Player, RecastGroup.Earthshatter, T0.AddSeconds(-1));
        _isDm = false;
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(0);

        _visual.RefreshRecastDelay(Player, RecastGroup.Earthshatter, T0.AddSeconds(-1));
        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(-1);
    }

    [Test]
    public void RestoreCooldownVisuals_ResumesTheRechargesStillRunningAtLogin()
    {
        _icons[100] = "ife_alpha";
        _visual.Abilities[(FeatType)100] = Ability(RecastGroup.Earthshatter, 60f);
        _db.Get<Player>(PlayerId).RecastTimes = new Dictionary<RecastGroup, DateTime>
        {
            [RecastGroup.Earthshatter] = T0.AddSeconds(30),
            [RecastGroup.Flash] = T0.AddSeconds(-5)
        };

        _visual.RestoreCooldownVisuals();

        _visual.GetCooldownStage(Player, RecastGroup.Earthshatter).Should().Be(3, "30 of the 60 second delay remain");
        _visual.GetCooldownStage(Player, RecastGroup.Flash).Should().Be(-1, "that recharge is over");
    }

    [Test]
    public void RestoreCooldownVisuals_CreatesTheMissingRecastTableAndSkipsDms()
    {
        var record = _db.Get<Player>(PlayerId);
        record.RecastTimes = null!;

        _isDm = true;
        _visual.RestoreCooldownVisuals();
        record.RecastTimes.Should().BeNull();
        _db.Saved.Should().BeEmpty();

        _isDm = false;
        _visual.RestoreCooldownVisuals();
        record.RecastTimes.Should().NotBeNull().And.BeEmpty();
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void RestoreCooldownVisuals_DoesNothingForAPlayerWithoutARecord()
    {
        _uuid = "unknown";

        _visual.RestoreCooldownVisuals();

        _db.Saved.Should().BeEmpty();
    }

    private static IGuiService ScriptedGui(List<uint> refreshes)
    {
        var gui = ScriptedProxy.Create<IGuiService>(out var script);
        script.On("PublishRefreshEvent", args =>
        {
            refreshes.Add((uint)args[0]!);
            return null;
        });
        return gui;
    }

    private sealed class TestVisual : AbilityCooldownVisualService
    {
        public TestVisual(IDBService db, IGuiService gui)
            : base(db, gui, FacadeForwarder.LazyFor<IAbilityService>())
        {
        }

        public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
        
        public int AbilityReads { get; private set; }

        protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails()
        {
            AbilityReads++;
            return Abilities;
        }

    }
}
