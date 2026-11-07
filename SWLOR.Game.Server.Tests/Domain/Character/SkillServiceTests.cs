using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the skill instance: the catalog caches, the rank rules, the XP award path (its order of reads,
/// writes, messages and events) and the weapon skill mappings.
/// </summary>
[TestFixture]
public class SkillServiceTests
{
    private const uint Creature = 1001;
    private const uint Weapon = 2002;
    private const uint MessageSource = 3003;

    private static readonly SkillType[] Fillers =
    {
        SkillType.Piloting, SkillType.FirstAid, SkillType.Smithery, SkillType.Fabrication, SkillType.Gathering,
        SkillType.Leadership, SkillType.BeastMastery, SkillType.Force
    };

    private PlayerEngine _player = null!;
    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private IDisposable _eventsScope = null!;
    private IReadOnlyList<string> _log => Trace();
    private InMemoryDBService _store = null!;
    private ScriptedProxy _twinProxy = null!;
    private bool _twinPair;
    private TestSkillService _service = null!;
    private Player _record = null!;

    private TestSkillService NewService()
    {
        var gui = ScriptedProxy.Create<IGuiService>(out var guiProxy);
        guiProxy.On("PublishRefreshEvent", _ =>
        {
            Mark("refresh");
            return null;
        });
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out var feedbackProxy);
        feedbackProxy.On("SendWarningToPlayer", args =>
        {
            Mark("warning:" + args[1]);
            return null;
        });
        var twin = ScriptedProxy.Create<ITwinPistolEquipmentService>(out _twinProxy);
        _twinProxy.On("IsActivePair", _ => _twinPair);
        return new TestSkillService(new LoggingDB(_store, Mark), gui, feedback, twin);
    }

    [SetUp]
    public void SetUp()
    {
        _player = new PlayerEngine();
        _engine = _player.Engine;
        _engine.SetReturn("GetCurrentHitPoints", 100);

        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("SignalEvent", args =>
        {
            Mark("signal:" + args[0]);
            return true;
        });
        _events.SetReturnHandler("PushEventData", args =>
        {
            Mark("push:" + args[0] + "=" + args[1]);
            return null;
        });
        _eventsScope = EventsPluginRuntime.Override(_events);

        _store = new InMemoryDBService();
        _record = new Player { Id = PlayerEngine.PlayerId };
        _store.With(_record);
        _twinPair = false;

        _service = NewService();
        _service.CacheData();
        _service.CacheXPChartData();
        _service.LoadMappings();
        _engine.ClearCalls();
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        _eventsScope.Dispose();
        _player.Dispose();
    }

    private PlayerSkill Give(SkillType skill, int rank, int xp = 0, bool locked = false, int? limit = null)
    {
        var state = new PlayerSkill { Rank = rank, XP = xp, IsLocked = locked, RankLimit = limit };
        _record.Skills[skill] = state;
        return state;
    }

    private void FillOthers(int rank, bool locked = false, int? limit = null)
    {
        foreach (var filler in Fillers)
            Give(filler, rank, 0, locked, limit);
    }

    /// <summary>Records a step in the engine's call list, so reads, writes, messages and events share one order.</summary>
    private static void Mark(string step) => WriteTimestampedLogEntry(step);

    private List<string> Trace()
    {
        var steps = new List<string>();
        foreach (var call in _engine.Calls)
        {
            switch (call.Member)
            {
                case "SendMessageToPC":
                    steps.Add("message:" + call.Arguments[1]);
                    break;
                case "FloatingTextStringOnCreature":
                    steps.Add("floating:" + call.Arguments[0]);
                    break;
                case "WriteTimestampedLogEntry":
                    steps.Add((string)call.Arguments[0]!);
                    break;
            }
        }

        return steps;
    }

    private IEnumerable<string> LogStartingWith(string prefix) => Trace().Where(x => x.StartsWith(prefix));

    // ---- the catalog caches

    [Test]
    public void CacheData_ListsEverySkillAndCategory_AndSignalsTheLoad()
    {
        _engine.ClearCalls();
        _service.CacheData();

        _service.GetAllSkills().Keys.Should().BeEquivalentTo(Enum.GetValues<SkillType>());
        _service.GetAllSkillCategories().Keys.Should().BeEquivalentTo(Enum.GetValues<SkillCategoryType>());
        _log.Should().Contain("signal:SWLOR_CACHE_SKILLS_LOADED");
    }

    [Test]
    public void CacheData_SplitsTheCatalogByActivityAndCapContribution()
    {
        var all = _service.GetAllSkills();
        var active = _service.GetAllActiveSkills();

        active.Keys.Should().BeEquivalentTo(all.Where(x => x.Value.IsActive).Select(x => x.Key));
        _service.GetAllContributingSkills().Keys.Should()
            .BeEquivalentTo(all.Where(x => x.Value.ContributesToSkillCap).Select(x => x.Key));
        _service.GetActiveContributingSkills().Keys.Should()
            .BeEquivalentTo(all.Where(x => x.Value.ContributesToSkillCap && x.Value.IsActive).Select(x => x.Key));
        _service.GetActiveCraftingSkills().Keys.Should()
            .BeEquivalentTo(active.Where(x => x.Value.IsShownInCraftMenu).Select(x => x.Key));
        _service.GetActiveResearchableCraftingSkills().Keys.Should()
            .BeEquivalentTo(active.Where(x => x.Value.IsShownInResearchMenu).Select(x => x.Key));
    }

    [Test]
    public void CacheData_ReadsHandOutCopies()
    {
        _service.GetAllSkills().Clear();

        _service.GetAllSkills().Should().NotBeEmpty();
    }

    [Test]
    public void Categories_AreGroupedAndFiltered()
    {
        var categories = _service.GetAllSkillCategories();

        _service.GetAllActiveSkillCategories().Keys.Should().Contain(SkillCategoryType.Combat);
        _service.GetAllContributingSkillCategories().Keys.Should().Contain(SkillCategoryType.Combat);
        _service.GetActiveContributingSkillCategories().Keys.Should().Contain(SkillCategoryType.Combat);
        _service.GetSkillCategoryDetails(SkillCategoryType.Combat).Should().BeSameAs(categories[SkillCategoryType.Combat]);
    }

    [Test]
    public void SkillsByCategory_ListEachSkillUnderItsCategory()
    {
        foreach (var category in _service.GetAllSkillCategories().Keys)
            _service.GetAllSkillsByCategory(category).Values.Should().OnlyContain(x => x.Category == category);

        _service.GetActiveSkillsByCategory(SkillCategoryType.Combat).Keys.Should().Contain(SkillType.Armor);
        _service.GetAllSkillsByCategory(SkillCategoryType.Combat).Keys.Should().Contain(SkillType.Armor);
    }

    [Test]
    public void DisplayOrder_SortsByNameAndPutsLanguagesLast()
    {
        var display = _service.GetAllActiveSkillsForDisplay().Values.ToList();
        var firstLanguage = display.FindIndex(x => x.Category == SkillCategoryType.Languages);

        display.Skip(firstLanguage).Should().OnlyContain(x => x.Category == SkillCategoryType.Languages);
        display.Take(firstLanguage).Select(x => x.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        display.Skip(firstLanguage).Select(x => x.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);

        foreach (var category in _service.GetAllActiveSkillCategories().Keys)
        {
            _service.GetActiveSkillsByCategoryForDisplay(category).Values.Select(x => x.Name).Should()
                .BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        }
    }

    [Test]
    public void GetSkillDetails_ReadsTheSkillAttribute()
    {
        var details = _service.GetSkillDetails(SkillType.Armor);

        details.Name.Should().Be("Armor");
        details.MaxRank.Should().Be(50);
        details.ContributesToSkillCap.Should().BeTrue();
    }

    // ---- skill points and rank limits

    [TestCase(-5, 10)]
    [TestCase(0, 10)]
    [TestCase(75, 85)]
    [TestCase(400, 410)]
    [TestCase(900, 410)]
    public void GetTotalSkillPoints_AddsTheStartingPoolToTheClampedEarnedPoints(int earned, int expected)
    {
        _service.GetTotalSkillPoints(new Player { TotalSPAcquired = earned }).Should().Be(expected);
    }

    [Test]
    public void ApCap_IsATenthOfTheSkillCap()
    {
        _service.APCap.Should().Be(40);
    }

    [Test]
    public void GetRankLimit_ClampsToTheSkillsRankRange_AndIsNullWithoutALimit()
    {
        var details = _service.GetSkillDetails(SkillType.Armor);

        _service.GetRankLimit(new PlayerSkill(), details).Should().BeNull();
        _service.GetRankLimit(new PlayerSkill { RankLimit = -3 }, details).Should().Be(0);
        _service.GetRankLimit(new PlayerSkill { RankLimit = 20 }, details).Should().Be(20);
        _service.GetRankLimit(new PlayerSkill { RankLimit = 99 }, details).Should().Be(50);
    }

    [Test]
    public void GetRankCeiling_IsTheLimitOrTheMaxRank()
    {
        var details = _service.GetSkillDetails(SkillType.Armor);

        _service.GetRankCeiling(new PlayerSkill(), details).Should().Be(50);
        _service.GetRankCeiling(new PlayerSkill { RankLimit = 12 }, details).Should().Be(12);
    }

    [Test]
    public void GetTotalContributingSkillRanks_CountsOnlyCapSkills_AndNeverNegative()
    {
        Give(SkillType.Armor, 10);
        Give(SkillType.Piloting, 5);
        Give(SkillType.Mirialan, 20);
        Give(SkillType.Smithery, -4);

        _service.GetTotalContributingSkillRanks(_record).Should().Be(15);
    }

    [Test]
    public void GetTotalContributingSkillRanks_BeforeTheCacheLoads_FallsBackToTheSkillAttributes()
    {
        var uncached = NewService();
        Give(SkillType.Armor, 10);
        Give(SkillType.Mirialan, 20);

        uncached.GetTotalContributingSkillRanks(_record).Should().Be(10);
    }

    [Test]
    public void IsBlockedBySkillCap_NeedsTheCap_AndNothingLeftToDecay()
    {
        FillOthers(50);
        Give(SkillType.Armor, 0);
        _service.IsBlockedBySkillCap(_record, SkillType.Armor).Should().BeFalse("other skills can still give up ranks");

        FillOthers(50, locked: true);
        _service.IsBlockedBySkillCap(_record, SkillType.Armor).Should().BeTrue();
        _service.IsBlockedBySkillCap(_record, SkillType.Mirialan).Should().BeFalse("languages are never blocked by the cap");

        FillOthers(40, locked: true);
        _service.IsBlockedBySkillCap(_record, SkillType.Armor).Should().BeFalse("the player is under the cap");
    }

    // ---- GetMaxDistributableXP

    [Test]
    public void GetMaxDistributableXP_ForAPlayerObject_RefusesInvalidSkillsNonPlayersAndDMs()
    {
        _service.GetMaxDistributableXP(Creature, SkillType.Invalid).Should().Be(0);

        _engine.SetReturn("GetIsPC", false);
        _service.GetMaxDistributableXP(Creature, SkillType.Armor).Should().Be(0);

        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturn("GetIsDM", true);
        _service.GetMaxDistributableXP(Creature, SkillType.Armor).Should().Be(0);
    }

    [Test]
    public void GetMaxDistributableXP_ForAPlayerObject_ReadsTheStoredRecord()
    {
        Give(SkillType.Armor, 0, 50);

        _service.GetMaxDistributableXP(Creature, SkillType.Armor).Should()
            .Be(_service.GetMaxDistributableXP(_record, SkillType.Armor));
        _log.Should().Contain("db.get");
    }

    [Test]
    public void GetMaxDistributableXP_IsZeroAtTheCeilingOrWhenTheCapBlocksTheSkill()
    {
        Give(SkillType.Armor, 50);
        _service.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(0);

        Give(SkillType.Armor, 5, 0, limit: 5);
        _service.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(0);

        FillOthers(50, locked: true);
        Give(SkillType.Armor, 0);
        _service.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(0);

        _service.GetMaxDistributableXP(_record, SkillType.Invalid).Should().Be(0);
    }

    [Test]
    public void GetMaxDistributableXP_SumsTheXpOfTheRemainingRanks()
    {
        Give(SkillType.Mirialan, 18, 100);

        _service.GetMaxDistributableXP(_record, SkillType.Mirialan).Should()
            .Be(_service.GetRequiredXP(18) - 100 + _service.GetRequiredXP(19));
    }

    [Test]
    public void GetMaxDistributableXP_LimitsACapSkillToTheFreeRanksPlusWhatDecayCanTake()
    {
        FillOthers(49);
        Give(SkillType.Armor, 0);

        // 392 ranks leave 8 free ranks and the fillers can give up 392 more: the skill reaches its max rank.
        _service.GetMaxDistributableXP(_record, SkillType.Armor).Should()
            .Be(Enumerable.Range(0, 50).Sum(rank => _service.GetRequiredXP(rank)));

        // Fillers at their limit have nothing to give: only the 8 free ranks remain.
        FillOthers(49, limit: 49);
        _service.GetMaxDistributableXP(_record, SkillType.Armor).Should()
            .Be(Enumerable.Range(0, 8).Sum(rank => _service.GetRequiredXP(rank)));
    }

    [Test]
    public void GetMaxDistributableXP_AcceptsOneXpWhenStoredXpAlreadyPaysTheNextRank()
    {
        FillOthers(49, limit: 49);
        Give(SkillType.Armor, 8, _service.GetRequiredXP(8) + 5);

        // 400 ranks are in use and nothing decays, but the skill is not blocked outright: no reachable rank is left.
        _service.GetMaxDistributableXP(_record, SkillType.Armor).Should().BeOneOf(0, 1);
        Give(SkillType.Armor, 7, _service.GetRequiredXP(7) + 5);
        _service.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(1);
    }

    // ---- GiveSkillXP: refusals

    [Test]
    public void GiveSkillXP_RefusesBeforeTouchingTheDatabase()
    {
        _service.GiveSkillXP(Creature, SkillType.Invalid, 100).Should().Be(0);
        _service.GiveSkillXP(Creature, SkillType.Armor, 0).Should().Be(0);

        _engine.SetReturn("GetIsPC", false);
        _service.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);
        _engine.SetReturn("GetIsPC", true);

        _engine.SetReturn("GetIsDM", true);
        _service.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);
        _engine.SetReturn("GetIsDM", false);

        _engine.SetReturn("GetIsDead", true);
        _service.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);
        _engine.SetReturn("GetIsDead", false);

        _engine.SetReturn("GetCurrentHitPoints", 0);
        _service.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);

        _log.Should().BeEmpty();
    }

    // ---- GiveSkillXP: the plain award

    [Test]
    public void GiveSkillXP_AppliesTheHenchmanPenaltyByDefault()
    {
        Give(SkillType.Armor, 0);

        var used = _service.GiveSkillXP(Creature, SkillType.Armor, 100);

        used.Should().Be(70);
        _record.Skills[SkillType.Armor].XP.Should().Be(70);
        _log.Should().Equal(
            "db.get",
            "message:You earned Armor skill experience. (70)",
            "db.set",
            "refresh");
    }

    [Test]
    public void GiveSkillXP_AddsSocialStatusAndDMBonuses_WhenBonusesAreNotIgnored()
    {
        Give(SkillType.Armor, 0);
        _engine.SetReturn("GetAbilityScore", 4);
        _service.StatAdjustment = 20;
        _record.DMXPBonus = 10;

        // 4 * 2.5% + 20 * 1% + 10 * 1% = 40% of 100, then the 30% henchman penalty.
        _service.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(98);
        _service.StatAdjustmentRequests.Should().Equal(Creature);
    }

    [Test]
    public void GiveSkillXP_IgnoringBonusesSkipsTheStatLookup()
    {
        Give(SkillType.Armor, 0);
        _engine.SetReturn("GetAbilityScore", 4);
        _service.StatAdjustment = 20;

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, ignoreBonuses: true, applyHenchmanPenalty: false)
            .Should().Be(100);

        _service.StatAdjustmentRequests.Should().BeEmpty();
        _engine.CountOf("GetAbilityScore").Should().Be(0);
    }

    [Test]
    public void GiveSkillXP_WithAMessageSource_CombinesTheFeedbackUntilTheNextFrame()
    {
        Give(SkillType.Armor, 0);

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false, MessageSource);
        _service.GiveSkillXP(Creature, SkillType.Armor, 50, true, false, MessageSource);

        _log.Should().NotContain(x => x.StartsWith("message:"));
        _service.Scheduled.Should().HaveCount(1);

        _engine.SetReturn("GetIsObjectValid", true);
        _service.Scheduled.Single()();

        _log.Should().Contain("message:You earned Armor skill experience. (150)");
    }

    [Test]
    public void GiveSkillXP_TheDeferredFeedbackSkipsPlayersWhoLeftBeforeTheFrame()
    {
        Give(SkillType.Armor, 0);
        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false, MessageSource);

        _engine.SetReturn("GetIsObjectValid", false);
        _service.Scheduled.Single()();

        _log.Should().NotContain(x => x.StartsWith("message:"));
    }

    // ---- GiveSkillXP: XP debt

    [Test]
    public void GiveSkillXP_RepaysDebtFirst_AndSavesWhenNothingIsLeft()
    {
        Give(SkillType.Armor, 0);
        _record.XPDebt = 500;

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(100);

        _record.XPDebt.Should().Be(400);
        _record.Skills[SkillType.Armor].XP.Should().Be(0);
        _log.Should().Equal(
            "db.get",
            "message:100 XP was removed from your debt. (Remaining: 400)",
            "db.set");
    }

    [Test]
    public void GiveSkillXP_ClearingTheDebtSaysSoAndAppliesTheRest()
    {
        Give(SkillType.Armor, 0);
        _record.XPDebt = 40;

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(100, "40 XP of debt plus 60 XP applied");

        _record.XPDebt.Should().Be(0);
        _record.Skills[SkillType.Armor].XP.Should().Be(60);
        _log.Should().Equal(
            "db.get",
            "message:Your XP debt has been cleared.",
            "message:You earned Armor skill experience. (60)",
            "db.set",
            "refresh");
    }

    // ---- GiveSkillXP: ceiling and cap

    [Test]
    public void GiveSkillXP_AtTheMaxRank_DiscardsTheXpWithoutAWarning()
    {
        Give(SkillType.Armor, 50, 30);

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(0);

        _record.Skills[SkillType.Armor].XP.Should().Be(0);
        _log.Should().Equal("db.get", "db.set", "refresh");
    }

    [Test]
    public void GiveSkillXP_AtARankLimit_WarnsAfterSaving()
    {
        Give(SkillType.Armor, 10, 0, limit: 10);

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(0);

        _log.Should().Equal("db.get", "db.set", "warning:SKILL_LIMIT");
    }

    [Test]
    public void GiveSkillXP_AtALanguageLimit_DoesNotWarn()
    {
        Give(SkillType.Mirialan, 5, 0, limit: 5);

        _service.GiveSkillXP(Creature, SkillType.Mirialan, 100, true, false).Should().Be(0);

        _log.Should().Equal("db.get", "db.set");
    }

    [Test]
    public void GiveSkillXP_BlockedByTheSkillCap_WarnsAndKeepsRepaidDebt()
    {
        FillOthers(50, locked: true);
        Give(SkillType.Armor, 0);
        _record.XPDebt = 10;

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(10);

        _record.XPDebt.Should().Be(0);
        _log.Should().Equal(
            "db.get",
            "message:Your XP debt has been cleared.",
            "db.set",
            "warning:SKILL_CAP");
    }

    [Test]
    public void GiveSkillXP_BlockedByTheSkillCap_WithoutDebtSavesNothing()
    {
        FillOthers(50, locked: true);
        Give(SkillType.Armor, 0);

        _service.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(0);

        _log.Should().Equal("db.get", "warning:SKILL_CAP");
    }

    // ---- GiveSkillXP: ranks

    [Test]
    public void GiveSkillXP_ARankUp_FollowsTheDocumentedOrder()
    {
        Give(SkillType.Armor, 0);
        _record.TotalSPAcquired = 9;
        var required = _service.GetRequiredXP(0);

        _service.GiveSkillXP(Creature, SkillType.Armor, required + 25, true, false).Should().Be(required + 25);

        _record.Skills[SkillType.Armor].Rank.Should().Be(1);
        _record.Skills[SkillType.Armor].XP.Should().Be(25);
        _record.UnallocatedSP.Should().Be(1);
        _record.TotalSPAcquired.Should().Be(10);
        _record.UnallocatedAP.Should().Be(1);
        _record.TotalAPAcquired.Should().Be(1);
        _log.Should().Equal(
            "db.get",
            "message:You earned Armor skill experience. (" + (required + 25) + ")",
            "floating:Your Armor skill level increased to rank 1!",
            "message:" + ColorToken.Green("You acquired 1 ability point!"),
            "db.set",
            "refresh",
            "signal:SWLOR_GAIN_SKILL_POINT");
    }

    [Test]
    public void GiveSkillXP_ALanguageRankUpGivesNoSkillOrAbilityPoints()
    {
        Give(SkillType.Mirialan, 0);
        _record.TotalSPAcquired = 9;

        _service.GiveSkillXP(Creature, SkillType.Mirialan, _service.GetRequiredXP(0), true, false);

        _record.Skills[SkillType.Mirialan].Rank.Should().Be(1);
        _record.UnallocatedSP.Should().Be(0);
        _record.TotalSPAcquired.Should().Be(9);
        _record.UnallocatedAP.Should().Be(0);
    }

    [Test]
    public void GiveSkillXP_NoAbilityPointComesPastTheTotalOrOffTheTenthRank()
    {
        Give(SkillType.Armor, 0);
        _record.TotalSPAcquired = 4;
        _service.GiveSkillXP(Creature, SkillType.Armor, _service.GetRequiredXP(0), true, false);
        _record.UnallocatedAP.Should().Be(0);

        Give(SkillType.Armor, 0);
        _record.TotalSPAcquired = 9;
        _record.TotalAPAcquired = 40;
        _service.GiveSkillXP(Creature, SkillType.Armor, _service.GetRequiredXP(0), true, false);
        _record.UnallocatedAP.Should().Be(0);
    }

    [Test]
    public void GiveSkillXP_ReachingTheCeilingDiscardsTheLeftover()
    {
        Give(SkillType.Armor, 0, 0, limit: 1);

        var used = _service.GiveSkillXP(Creature, SkillType.Armor, _service.GetRequiredXP(0) + 100, true, false);

        _record.Skills[SkillType.Armor].Rank.Should().Be(1);
        _record.Skills[SkillType.Armor].XP.Should().Be(0);
        used.Should().Be(_service.GetRequiredXP(0), "discarded XP is not counted as used");
    }

    [Test]
    public void GiveSkillXP_SeveralRanksInOneAward_RaiseEachInTurn()
    {
        Give(SkillType.Armor, 0);
        var xp = _service.GetRequiredXP(0) + _service.GetRequiredXP(1) + 1;

        _service.GiveSkillXP(Creature, SkillType.Armor, xp, true, false);

        _record.Skills[SkillType.Armor].Rank.Should().Be(2);
        _record.Skills[SkillType.Armor].XP.Should().Be(1);
        LogStartingWith("floating:").Should().Equal(
            "floating:Your Armor skill level increased to rank 1!",
            "floating:Your Armor skill level increased to rank 2!");
    }

    // ---- GiveSkillXP: decay

    [Test]
    public void GiveSkillXP_AtTheCap_DecaysOneRandomOtherSkillPerRank()
    {
        FillOthers(50);
        Give(SkillType.Armor, 0);
        Random.SetSeed(7);
        var expected = Fillers[new System.Random(7).Next(Fillers.Length)];
        _record.Skills[expected].XP = 12;

        _service.GiveSkillXP(Creature, SkillType.Armor, _service.GetRequiredXP(0), true, false);

        _record.Skills[SkillType.Armor].Rank.Should().Be(1);
        _record.Skills[expected].Rank.Should().Be(49);
        _record.Skills[expected].XP.Should().Be(0);
        Fillers.Where(x => x != expected).Should().OnlyContain(x => _record.Skills[x].Rank == 50);
        _log.Should().ContainInOrder("db.set", "refresh", "signal:SWLOR_GAIN_SKILL_POINT", "signal:SWLOR_SKILL_LOST_BY_DECAY");
        _events.CallsTo("PushEventData").Select(x => (x.Arguments[0], x.Arguments[1])).Should()
            .Equal(("SKILL_TYPE_ID", ((int)expected).ToString()));
    }

    [Test]
    public void GiveSkillXP_DecayOnlyTakesFromSkillsAboveTheirLimitWhileAnyExist()
    {
        FillOthers(50, limit: 50);
        Give(SkillType.Force, 50, 0, limit: 40);
        Give(SkillType.Armor, 0);
        Random.SetSeed(3);

        _service.GiveSkillXP(Creature, SkillType.Armor, _service.GetRequiredXP(0), true, false);

        _record.Skills[SkillType.Force].Rank.Should().Be(49);
        Fillers.Where(x => x != SkillType.Force).Should().OnlyContain(x => _record.Skills[x].Rank == 50);
    }

    [Test]
    public void GiveSkillXP_WhenTheCapIsReachedAndNothingCanDecay_TheLoopStops()
    {
        // 399 ranks: the first rank fills the cap, the second is refused because every other skill is locked.
        FillOthers(50);
        Give(SkillType.Force, 49);
        Give(SkillType.Armor, 0);
        foreach (var filler in Fillers)
            _record.Skills[filler].IsLocked = true;

        var xp = _service.GetRequiredXP(0) + _service.GetRequiredXP(1);
        _service.GiveSkillXP(Creature, SkillType.Armor, xp, true, false).Should().BeGreaterThan(0);

        _record.Skills[SkillType.Armor].Rank.Should().Be(1, "the cap stops the second rank");
    }

    // ---- AddMissingSkills

    [Test]
    public void AddMissingSkills_CreatesARecordForEverySkillTheCharacterLacks()
    {
        _engine.SetReturn("GetEnteringObject", Creature);
        var kept = Give(SkillType.Armor, 12, 34);

        _service.AddMissingSkills();

        _record.Skills.Keys.Should().BeEquivalentTo(Enum.GetValues<SkillType>());
        _record.Skills[SkillType.Armor].Should().BeSameAs(kept);
        _record.Skills[SkillType.Piloting].Rank.Should().Be(0);
        _log.Should().Equal("db.get", "db.set");
    }

    [Test]
    public void AddMissingSkills_IgnoresDMsAndNonPlayers()
    {
        _engine.SetReturn("GetEnteringObject", Creature);
        _engine.SetReturn("GetIsDM", true);
        _service.AddMissingSkills();

        _engine.SetReturn("GetIsDM", false);
        _engine.SetReturn("GetIsPC", false);
        _service.AddMissingSkills();

        _log.Should().BeEmpty();
    }

    // ---- GetCreatureSkillRank

    [Test]
    public void GetCreatureSkillRank_ReadsAPlayersStoredRank()
    {
        Give(SkillType.Armor, 17);

        _service.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(17);
        _service.GetCreatureSkillRank(Creature, SkillType.Piloting).Should().Be(0);
    }

    [Test]
    public void GetCreatureSkillRank_IsZeroForDMsInvalidSkillsAndOtherCreatures()
    {
        Give(SkillType.Armor, 17);
        _service.GetCreatureSkillRank(Creature, SkillType.Invalid).Should().Be(0);

        _engine.SetReturn("GetIsPC", false);
        _service.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(0);

        _engine.SetReturn("GetIsDM", true);
        _service.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(0);
    }

    [Test]
    public void GetCreatureSkillRank_ADroidAnswersFromItsControllerItem()
    {
        _service.DroidCreatures.Add(Creature);
        _service.DroidRank = 22;
        Give(SkillType.Armor, 17);

        _service.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(22);
        _log.Should().BeEmpty("a droid's ranks are not stored on a player record");
    }

    // ---- weapon skills

    [Test]
    public void GetSkillTypeByBaseItem_MapsWeaponsAndAmmunition_AndNothingElse()
    {
        _service.GetSkillTypeByBaseItem(BaseItem.Longsword).Should().Be(SkillType.Vibroblade);
        _service.GetSkillTypeByBaseItem(BaseItem.Dagger).Should().Be(SkillType.Vibroknife);
        _service.GetSkillTypeByBaseItem(BaseItem.Lightsaber).Should().Be(SkillType.Lightsaber);
        _service.GetSkillTypeByBaseItem(BaseItem.Rifle).Should().Be(SkillType.Rifle);
        _service.GetSkillTypeByBaseItem(BaseItem.Pistol).Should().Be(SkillType.Pistol);
        _service.GetSkillTypeByBaseItem(BaseItem.CompanionPistol).Should().Be(SkillType.TwinPistol);
        _service.GetSkillTypeByBaseItem(BaseItem.PowerGlove).Should().Be(SkillType.Unarmed);
        _service.GetSkillTypeByBaseItem(BaseItem.Shuriken).Should().Be(SkillType.Throwing);
        _service.GetSkillTypeByBaseItem(BaseItem.Amulet).Should().Be(SkillType.Invalid);
    }

    [Test]
    public void GetSkillTypeByBaseItem_IsInvalidBeforeTheMappingsLoad()
    {
        NewService().GetSkillTypeByBaseItem(BaseItem.Longsword).Should().Be(SkillType.Invalid);
    }

    [Test]
    public void GetAttackSkillTypeByBaseItem_TreatsGlovesAndBracersAsBareHands()
    {
        _service.GetAttackSkillTypeByBaseItem(BaseItem.Gloves).Should().Be(SkillType.Unarmed);
        _service.GetAttackSkillTypeByBaseItem(BaseItem.Bracer).Should().Be(SkillType.Unarmed);
        _service.GetSkillTypeByBaseItem(BaseItem.Gloves).Should().Be(SkillType.Katar);
        _service.GetAttackSkillTypeByBaseItem(BaseItem.Katar).Should().Be(SkillType.Katar);
    }

    [Test]
    public void GetWeaponSkill_PrefersAnExplicitOverride()
    {
        _service.GetWeaponSkill(Creature, Weapon, SkillType.Force).Should().Be(SkillType.Force);
        _service.GetWeaponSkill(Creature, OBJECT_INVALID, SkillType.Force).Should().Be(SkillType.Force);
    }

    [Test]
    public void GetWeaponSkill_IgnoresAnInvalidOverride()
    {
        _engine.SetReturn("GetIsObjectValid", false);

        _service.GetWeaponSkill(Creature, Weapon, SkillType.Invalid).Should().Be(SkillType.Invalid);
        _service.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Invalid);
    }

    [Test]
    public void GetWeaponSkill_ReadsTheBaseItemOfAValidWeapon()
    {
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetBaseItemType", BaseItem.Rifle);

        _service.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Rifle);
        _twinProxy.Calls.Should().BeEmpty("the pair is only asked about when the weapon is held");
    }

    [Test]
    public void GetWeaponSkill_AHeldPistolOfAnActivePairTrainsTwinPistol()
    {
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetBaseItemType", BaseItem.Pistol);
        _engine.SetReturnHandler("GetItemInSlot", args => (InventorySlot)args[0]! == InventorySlot.RightHand ? Weapon : OBJECT_INVALID);

        _twinPair = true;
        _service.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.TwinPistol);

        _twinPair = false;
        _service.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Pistol);
    }

    [Test]
    public void GetWeaponSkill_ACompanionPistolOutsideAPairTrainsNothing()
    {
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetBaseItemType", BaseItem.CompanionPistol);

        _service.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Invalid);
    }

    // ---- the XP chart behind the instance

    [Test]
    public void TheXpChartMembers_AnswerFromTheChart()
    {
        _service.GetRequiredXP(0).Should().Be(550);
        _service.GetTotalRequiredXP(1).Should().Be(550 + 825);
        _service.GetLevelByTotalXP(560).Should().Be((1, 10));
        _service.GetDeltaXP(-1).Should().Be(450);
        _service.GetPracticeXP(10, 5, 20).Should().Be(SkillXPChart.GetPracticeXP(10, 5, 20));
        _service.GetEspionageRequiredRank(PerkType.Poisoncraft, 2).Should().Be(15);
        _service.GetEspionagePracticeRankLimit(PerkType.Poisoncraft, 5).Should().Be(50);
        _service.CalculateEspionageXP(PerkType.Poisoncraft, 2, 20).Should()
            .Be(SkillXPChart.CalculateEspionageXP(PerkType.Poisoncraft, 2, 20));
    }

    // ---- the seams

    [Test]
    public void TheDefaultSeams_RunWithoutAServer()
    {
        var plain = new SkillService(new LoggingDB(_store, Mark), ScriptedProxy.Create<IGuiService>(out _),
            ScriptedProxy.Create<IPlayerFeedbackService>(out _), ScriptedProxy.Create<ITwinPistolEquipmentService>(out _));

        // A DM answers 0 before any seam is reached, and a creature that is not a droid falls through to the player lookup.
        _engine.SetReturn("GetIsDM", true);
        plain.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(0);
    }

    // ---- stand-ins

    private sealed class TestSkillService : SkillService
    {
        public TestSkillService(IDBService db, IGuiService gui, IPlayerFeedbackService feedback, ITwinPistolEquipmentService twin)
            : base(db, gui, feedback, twin)
        {
        }

        public int StatAdjustment { get; set; }
        public List<uint> StatAdjustmentRequests { get; } = new();
        public HashSet<uint> DroidCreatures { get; } = new();
        public int DroidRank { get; set; }
        public List<Action> Scheduled { get; } = new();

        protected override int GetExperiencePercentAdjustment(uint player)
        {
            StatAdjustmentRequests.Add(player);
            return StatAdjustment;
        }

        protected override bool IsDroid(uint creature) => DroidCreatures.Contains(creature);

        protected override int GetDroidSkillRank(uint creature, SkillType skillType) => DroidRank;

        protected override void ScheduleXPMessages(Action callback) => Scheduled.Add(callback);
    }

    /// <summary>Logs the order of reads and writes next to the messages, then answers from the in-memory store.</summary>
    private sealed class LoggingDB : IDBService
    {
        private readonly InMemoryDBService _inner;
        private readonly Action<string> _mark;

        public LoggingDB(InMemoryDBService inner, Action<string> mark)
        {
            _inner = inner;
            _mark = mark;
        }

        public T Get<T>(string id) where T : EntityBase
        {
            _mark("db.get");
            return _inner.Get<T>(id);
        }

        public void Set<T>(T entity) where T : EntityBase
        {
            _mark("db.set");
            _inner.Set(entity);
        }

        public void Delete<T>(string id) where T : EntityBase => _inner.Delete<T>(id);

        public bool Exists<T>(string id) where T : EntityBase => _inner.Exists<T>(id);

        public IEnumerable<T> Search<T>(DBQuery<T> query) where T : EntityBase => _inner.Search(query);

        public long SearchCount<T>(DBQuery<T> query) where T : EntityBase => _inner.SearchCount(query);

        public long GetWriteVersion<T>() where T : EntityBase => _inner.GetWriteVersion<T>();

        public void Load() => _inner.Load();

        public StackExchange.Redis.RedisValue StreamAdd(StackExchange.Redis.RedisKey key,
            StackExchange.Redis.NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
            _inner.StreamAdd(key, streamPairs, maxLength, useApproximateMaxLength);

        public string GetRawJson<T>(string id) => _inner.GetRawJson<T>(id);

        public IEnumerable<string> SearchIds<T>(DBQuery<T> query) where T : EntityBase => _inner.SearchIds(query);

        public IEnumerable<string> SearchRawJson<T>(DBQuery<T> query) where T : EntityBase => _inner.SearchRawJson(query);

        public void SetUnindexedJsonProperty<T>(string id, string propertyName, string json) where T : EntityBase =>
            _inner.SetUnindexedJsonProperty<T>(id, propertyName, json);

        public string EscapeTokens(string str) => _inner.EscapeTokens(str);

        public Task<StackExchange.Redis.RedisValue> StreamAddAsync(StackExchange.Redis.RedisKey key,
            StackExchange.Redis.NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
            _inner.StreamAddAsync(key, streamPairs, maxLength, useApproximateMaxLength);
    }
}
