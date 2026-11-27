using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the skill XP service: the award path (its order of reads, writes, messages and events), the XP chart formulas and the
/// default bodies of its seams, over the shared skill world.
/// </summary>
[TestFixture]
public class SkillXPServiceTests : SkillTestBase
{
    // ---- GiveSkillXP: refusals

    [Test]
    public void GiveSkillXP_RefusesBeforeTouchingTheDatabase()
    {
        _service.XP.GiveSkillXP(Creature, SkillType.Invalid, 100).Should().Be(0);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 0).Should().Be(0);

        _engine.SetReturn("GetIsPC", false);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);
        _engine.SetReturn("GetIsPC", true);

        _engine.SetReturn("GetIsDM", true);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);
        _engine.SetReturn("GetIsDM", false);

        _engine.SetReturn("GetIsDead", true);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);
        _engine.SetReturn("GetIsDead", false);

        _engine.SetReturn("GetCurrentHitPoints", 0);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(0);

        _log.Should().BeEmpty();
    }

    // ---- GiveSkillXP: the plain award

    [Test]
    public void GiveSkillXP_AppliesTheHenchmanPenaltyByDefault()
    {
        Give(SkillType.Armor, 0);

        var used = _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100);

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
        _service.XP.StatAdjustment = 20;
        _record.DMXPBonus = 10;

        // 4 * 2.5% + 20 * 1% + 10 * 1% = 40% of 100, then the 30% henchman penalty.
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100).Should().Be(98);
        _service.XP.StatAdjustmentRequests.Should().Equal(Creature);
    }

    [Test]
    public void GiveSkillXP_IgnoringBonusesSkipsTheStatLookup()
    {
        Give(SkillType.Armor, 0);
        _engine.SetReturn("GetAbilityScore", 4);
        _service.XP.StatAdjustment = 20;

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, ignoreBonuses: true, applyHenchmanPenalty: false)
            .Should().Be(100);

        _service.XP.StatAdjustmentRequests.Should().BeEmpty();
        _engine.CountOf("GetAbilityScore").Should().Be(0);
    }

    [Test]
    public void GiveSkillXP_WithAMessageSource_CombinesTheFeedbackUntilTheNextFrame()
    {
        Give(SkillType.Armor, 0);

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false, MessageSource);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 50, true, false, MessageSource);

        _log.Should().NotContain(x => x.StartsWith("message:"));
        _service.XP.Scheduled.Should().HaveCount(1);

        _engine.SetReturn("GetIsObjectValid", true);
        _service.XP.Scheduled.Single()();

        _log.Should().Contain("message:You earned Armor skill experience. (150)");
    }

    [Test]
    public void GiveSkillXP_TheDeferredFeedbackSkipsPlayersWhoLeftBeforeTheFrame()
    {
        Give(SkillType.Armor, 0);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false, MessageSource);

        _engine.SetReturn("GetIsObjectValid", false);
        _service.XP.Scheduled.Single()();

        _log.Should().NotContain(x => x.StartsWith("message:"));
    }

    // ---- GiveSkillXP: XP debt

    [Test]
    public void GiveSkillXP_RepaysDebtFirst_AndSavesWhenNothingIsLeft()
    {
        Give(SkillType.Armor, 0);
        _record.XPDebt = 500;

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(100);

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

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(100, "40 XP of debt plus 60 XP applied");

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

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(0);

        _record.Skills[SkillType.Armor].XP.Should().Be(0);
        _log.Should().Equal("db.get", "db.set", "refresh");
    }

    [Test]
    public void GiveSkillXP_AtARankLimit_WarnsAfterSaving()
    {
        Give(SkillType.Armor, 10, 0, limit: 10);

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(0);

        _log.Should().Equal("db.get", "db.set", "warning:SKILL_LIMIT");
    }

    [Test]
    public void GiveSkillXP_AtALanguageLimit_DoesNotWarn()
    {
        Give(SkillType.Mirialan, 5, 0, limit: 5);

        _service.XP.GiveSkillXP(Creature, SkillType.Mirialan, 100, true, false).Should().Be(0);

        _log.Should().Equal("db.get", "db.set");
    }

    [Test]
    public void GiveSkillXP_BlockedByTheSkillCap_WarnsAndKeepsRepaidDebt()
    {
        FillOthers(50, locked: true);
        Give(SkillType.Armor, 0);
        _record.XPDebt = 10;

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(10);

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

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, 100, true, false).Should().Be(0);

        _log.Should().Equal("db.get", "warning:SKILL_CAP");
    }

    // ---- GiveSkillXP: ranks

    [Test]
    public void GiveSkillXP_ARankUp_FollowsTheDocumentedOrder()
    {
        Give(SkillType.Armor, 0);
        _record.TotalSPAcquired = 9;
        var required = _service.Catalog.GetRequiredXP(0);

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, required + 25, true, false).Should().Be(required + 25);

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

        _service.XP.GiveSkillXP(Creature, SkillType.Mirialan, _service.Catalog.GetRequiredXP(0), true, false);

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
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, _service.Catalog.GetRequiredXP(0), true, false);
        _record.UnallocatedAP.Should().Be(0);

        Give(SkillType.Armor, 0);
        _record.TotalSPAcquired = 9;
        _record.TotalAPAcquired = 40;
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, _service.Catalog.GetRequiredXP(0), true, false);
        _record.UnallocatedAP.Should().Be(0);
    }

    [Test]
    public void GiveSkillXP_ReachingTheCeilingDiscardsTheLeftover()
    {
        Give(SkillType.Armor, 0, 0, limit: 1);

        var used = _service.XP.GiveSkillXP(Creature, SkillType.Armor, _service.Catalog.GetRequiredXP(0) + 100, true, false);

        _record.Skills[SkillType.Armor].Rank.Should().Be(1);
        _record.Skills[SkillType.Armor].XP.Should().Be(0);
        used.Should().Be(_service.Catalog.GetRequiredXP(0), "discarded XP is not counted as used");
    }

    [Test]
    public void GiveSkillXP_SeveralRanksInOneAward_RaiseEachInTurn()
    {
        Give(SkillType.Armor, 0);
        var xp = _service.Catalog.GetRequiredXP(0) + _service.Catalog.GetRequiredXP(1) + 1;

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, xp, true, false);

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

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, _service.Catalog.GetRequiredXP(0), true, false);

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

        _service.XP.GiveSkillXP(Creature, SkillType.Armor, _service.Catalog.GetRequiredXP(0), true, false);

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

        var xp = _service.Catalog.GetRequiredXP(0) + _service.Catalog.GetRequiredXP(1);
        _service.XP.GiveSkillXP(Creature, SkillType.Armor, xp, true, false).Should().BeGreaterThan(0);

        _record.Skills[SkillType.Armor].Rank.Should().Be(1, "the cap stops the second rank");
    }

    // ---- the XP chart behind the instance

    [Test]
    public void TheXpChartMembers_AnswerFromTheChart()
    {
        _service.Catalog.GetRequiredXP(0).Should().Be(550);
        _service.Catalog.GetTotalRequiredXP(1).Should().Be(550 + 825);
        _service.Catalog.GetLevelByTotalXP(560).Should().Be((1, 10));
        _service.XP.GetDeltaXP(-1).Should().Be(450);
        _service.XP.GetPracticeXP(10, 5, 20).Should().Be(SkillXPChart.GetPracticeXP(10, 5, 20));
        _service.Catalog.GetEspionageRequiredRank(PerkType.Poisoncraft, 2).Should().Be(15);
        _service.Catalog.GetEspionagePracticeRankLimit(PerkType.Poisoncraft, 5).Should().Be(50);
        _service.XP.CalculateEspionageXP(PerkType.Poisoncraft, 2, 20).Should()
            .Be(SkillXPChart.CalculateEspionageXP(PerkType.Poisoncraft, 2, 20));
    }

    // ---- the seams

    [Test]
    public void TheDefaultSeams_RunWithoutAServer()
    {
        var stat = ScriptedProxy.Create<IStatService>(out var statScript);
        statScript.On("GetStatAdjustment", _ => 10);
        var gui = ScriptedProxy.Create<IGuiService>(out var guiScript);
        guiScript.On("PublishRefreshEvent", _ => null);
        var plain = new SkillXPService(new LoggingDB(_store, Mark), gui,
            ScriptedProxy.Create<IPlayerFeedbackService>(out _), new Lazy<IStatService>(() => stat), _service, _service.Rank);
        Give(SkillType.Armor, 0);

        // The experience bonus is read from the stat service, and an award with a message source hands the combined message to the scheduler.
        plain.GiveSkillXP(Creature, SkillType.Armor, 100, false, false, MessageSource).Should().BeGreaterThan(100);

        // The deferred message sits on the static scheduler; drain it while this test's engine is installed.
        Scheduler.Process();
    }
}
