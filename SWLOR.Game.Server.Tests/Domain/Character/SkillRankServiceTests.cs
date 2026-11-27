using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using static SWLOR.NWN.API.NWScript.NWScript;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the skill rank rules: rank limits and ceilings, the skill cap total, the decay candidates and the XP a skill can still
/// take. The catalog is in <see cref="SkillServiceTests"/> and the XP award path in <see cref="SkillXPServiceTests"/>.
/// </summary>
[TestFixture]
public class SkillRankServiceTests : SkillTestBase
{
    // ---- skill points and rank limits

    [Test]
    public void GetRankLimit_ClampsToTheSkillsRankRange_AndIsNullWithoutALimit()
    {
        var details = _service.Catalog.GetSkillDetails(SkillType.Armor);

        _service.Rank.GetRankLimit(new PlayerSkill(), details).Should().BeNull();
        _service.Rank.GetRankLimit(new PlayerSkill { RankLimit = -3 }, details).Should().Be(0);
        _service.Rank.GetRankLimit(new PlayerSkill { RankLimit = 20 }, details).Should().Be(20);
        _service.Rank.GetRankLimit(new PlayerSkill { RankLimit = 99 }, details).Should().Be(50);
    }

    [Test]
    public void GetRankCeiling_IsTheLimitOrTheMaxRank()
    {
        var details = _service.Catalog.GetSkillDetails(SkillType.Armor);

        _service.Rank.GetRankCeiling(new PlayerSkill(), details).Should().Be(50);
        _service.Rank.GetRankCeiling(new PlayerSkill { RankLimit = 12 }, details).Should().Be(12);
    }

    [Test]
    public void GetTotalContributingSkillRanks_CountsOnlyCapSkills_AndNeverNegative()
    {
        Give(SkillType.Armor, 10);
        Give(SkillType.Piloting, 5);
        Give(SkillType.Mirialan, 20);
        Give(SkillType.Smithery, -4);

        _service.Rank.GetTotalContributingSkillRanks(_record).Should().Be(15);
    }

    [Test]
    public void GetTotalContributingSkillRanks_BeforeTheCacheLoads_FallsBackToTheSkillAttributes()
    {
        var uncached = NewService();
        Give(SkillType.Armor, 10);
        Give(SkillType.Mirialan, 20);

        uncached.Rank.GetTotalContributingSkillRanks(_record).Should().Be(10);
    }

    [Test]
    public void IsBlockedBySkillCap_NeedsTheCap_AndNothingLeftToDecay()
    {
        FillOthers(50);
        Give(SkillType.Armor, 0);
        _service.Rank.IsBlockedBySkillCap(_record, SkillType.Armor).Should().BeFalse("other skills can still give up ranks");

        FillOthers(50, locked: true);
        _service.Rank.IsBlockedBySkillCap(_record, SkillType.Armor).Should().BeTrue();
        _service.Rank.IsBlockedBySkillCap(_record, SkillType.Mirialan).Should().BeFalse("languages are never blocked by the cap");

        FillOthers(40, locked: true);
        _service.Rank.IsBlockedBySkillCap(_record, SkillType.Armor).Should().BeFalse("the player is under the cap");
    }

    // ---- GetMaxDistributableXP

    [Test]
    public void GetMaxDistributableXP_ForAPlayerObject_RefusesInvalidSkillsNonPlayersAndDMs()
    {
        _service.Rank.GetMaxDistributableXP(Creature, SkillType.Invalid).Should().Be(0);

        _engine.SetReturn("GetIsPC", false);
        _service.Rank.GetMaxDistributableXP(Creature, SkillType.Armor).Should().Be(0);

        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturn("GetIsDM", true);
        _service.Rank.GetMaxDistributableXP(Creature, SkillType.Armor).Should().Be(0);
    }

    [Test]
    public void GetMaxDistributableXP_ForAPlayerObject_ReadsTheStoredRecord()
    {
        Give(SkillType.Armor, 0, 50);

        _service.Rank.GetMaxDistributableXP(Creature, SkillType.Armor).Should()
            .Be(_service.Rank.GetMaxDistributableXP(_record, SkillType.Armor));
        _log.Should().Contain("db.get");
    }

    [Test]
    public void GetMaxDistributableXP_IsZeroAtTheCeilingOrWhenTheCapBlocksTheSkill()
    {
        Give(SkillType.Armor, 50);
        _service.Rank.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(0);

        Give(SkillType.Armor, 5, 0, limit: 5);
        _service.Rank.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(0);

        FillOthers(50, locked: true);
        Give(SkillType.Armor, 0);
        _service.Rank.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(0);

        _service.Rank.GetMaxDistributableXP(_record, SkillType.Invalid).Should().Be(0);
    }

    [Test]
    public void GetMaxDistributableXP_SumsTheXpOfTheRemainingRanks()
    {
        Give(SkillType.Mirialan, 18, 100);

        _service.Rank.GetMaxDistributableXP(_record, SkillType.Mirialan).Should()
            .Be(_service.Catalog.GetRequiredXP(18) - 100 + _service.Catalog.GetRequiredXP(19));
    }

    [Test]
    public void GetMaxDistributableXP_LimitsACapSkillToTheFreeRanksPlusWhatDecayCanTake()
    {
        FillOthers(49);
        Give(SkillType.Armor, 0);

        // 392 ranks leave 8 free ranks and the fillers can give up 392 more: the skill reaches its max rank.
        _service.Rank.GetMaxDistributableXP(_record, SkillType.Armor).Should()
            .Be(Enumerable.Range(0, 50).Sum(rank => _service.Catalog.GetRequiredXP(rank)));

        // Fillers at their limit have nothing to give: only the 8 free ranks remain.
        FillOthers(49, limit: 49);
        _service.Rank.GetMaxDistributableXP(_record, SkillType.Armor).Should()
            .Be(Enumerable.Range(0, 8).Sum(rank => _service.Catalog.GetRequiredXP(rank)));
    }

    [Test]
    public void GetMaxDistributableXP_AcceptsOneXpWhenStoredXpAlreadyPaysTheNextRank()
    {
        FillOthers(49, limit: 49);
        Give(SkillType.Armor, 8, _service.Catalog.GetRequiredXP(8) + 5);

        // 400 ranks are in use and nothing decays, but the skill is not blocked outright: no reachable rank is left.
        _service.Rank.GetMaxDistributableXP(_record, SkillType.Armor).Should().BeOneOf(0, 1);
        Give(SkillType.Armor, 7, _service.Catalog.GetRequiredXP(7) + 5);
        _service.Rank.GetMaxDistributableXP(_record, SkillType.Armor).Should().Be(1);
    }

    // ---- decay

    [TestCase(true, SkillService.SkillCap, false)]
    [TestCase(true, SkillService.SkillCap + 1, true)]
    [TestCase(false, SkillService.SkillCap + 1, false)]
    public void ShouldDecaySkillRank_NeedsACapSkillOverTheCap(bool contributes, int totalRanks, bool expected)
    {
        _service.Rank.ShouldDecaySkillRank(contributes, totalRanks).Should().Be(expected);
    }

    [Test]
    public void GetDecayCandidates_PrefersSkillsAboveTheirLimit_AndSkipsLockedEmptyAndTheGainingSkill()
    {
        Give(SkillType.Armor, 10);
        Give(SkillType.Piloting, 10, limit: 4);
        Give(SkillType.FirstAid, 10, limit: 10);
        Give(SkillType.Smithery, 10, locked: true);
        Give(SkillType.Fabrication, 0);
        Give(SkillType.Mirialan, 10);

        _service.Rank.GetDecayCandidates(_record, SkillType.Armor).Should().Equal(SkillType.Piloting);

        _record.Skills[SkillType.Piloting].RankLimit = 10;
        _service.Rank.GetDecayCandidates(_record, SkillType.Armor).Should().BeEmpty("a skill at or below its limit never decays");

        _record.Skills[SkillType.Piloting].RankLimit = null;
        _record.Skills[SkillType.FirstAid].RankLimit = null;
        _service.Rank.GetDecayCandidates(_record, SkillType.Armor).Should().BeEquivalentTo(new[] { SkillType.Piloting, SkillType.FirstAid });
    }

    [Test]
    public void TheCapSkillSet_IsReadFromTheCatalogOnceItIsCached_AndFromTheAttributesBefore()
    {
        var uncached = NewService();
        Give(SkillType.Armor, 7);
        Give(SkillType.Mirialan, 9);

        uncached.Rank.GetTotalContributingSkillRanks(_record).Should().Be(7);
        _service.Rank.GetTotalContributingSkillRanks(_record).Should().Be(7);
    }
}
