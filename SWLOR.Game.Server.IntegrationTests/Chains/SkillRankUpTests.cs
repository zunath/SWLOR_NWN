using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Domain.Character;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.IntegrationTests.Chains;

/// <summary>XP awards to a skill, across frames, to a rank-up: <c>Skill.GiveSkillXP</c> with the record in Redis.</summary>
[TestFixture]
public class SkillRankUpTests : IntegrationTestBase
{
    private SkillType _skill;

    [SetUp]
    public void SetUpPlayer()
    {
        ServerCaches.EnsureSkills();
        ConfigurePlayer();

        _skill = Enum.GetValues<SkillType>()
            .First(type => type != SkillType.Invalid && Skill.GetSkillDetails(type).ContributesToSkillCap && Skill.GetSkillDetails(type).MaxRank > 5);

        var record = new Player(HeroId);
        foreach (var type in Enum.GetValues<SkillType>().Where(type => type != SkillType.Invalid))
            record.Skills[type] = new PlayerSkill();
        Db.Set(record);
        Db.EndFrame();
    }

    private static int RequiredXP(int rank) => Skill.GetRequiredXP(rank);

    [Test]
    public void AnAwardBelowTheRankCost_IsSavedAsProgress()
    {
        var applied = Skill.GiveSkillXP(Hero, _skill, RequiredXP(0) - 1, ignoreBonuses: true, applyHenchmanPenalty: false);

        applied.Should().Be(RequiredXP(0) - 1);
        Db.EndFrame();
        var record = Db.Get<Player>(HeroId);
        record.Skills[_skill].Rank.Should().Be(0);
        record.Skills[_skill].XP.Should().Be(RequiredXP(0) - 1);
        record.UnallocatedSP.Should().Be(0);
    }

    [Test]
    public void ProgressAcrossFrames_ReachesTheRankAndGrantsASkillPoint()
    {
        var cost = RequiredXP(0);
        Skill.GiveSkillXP(Hero, _skill, cost - 1, ignoreBonuses: true, applyHenchmanPenalty: false);
        Db.EndFrame();

        Skill.GiveSkillXP(Hero, _skill, 1, ignoreBonuses: true, applyHenchmanPenalty: false);

        Db.EndFrame();
        var record = Db.Get<Player>(HeroId);
        record.Skills[_skill].Rank.Should().Be(1);
        record.Skills[_skill].XP.Should().Be(0);
        record.UnallocatedSP.Should().Be(1);
        record.TotalSPAcquired.Should().Be(1);
        Messages().Should().Contain(message => message.Contains("skill experience"));
        Engine.CallsTo("FloatingTextStringOnCreature").Select(call => (string)call.Arguments[0]!)
            .Should().Contain(text => text.Contains("increased to rank 1"));
        JObject.Parse(Db.GetRawJson<Player>(HeroId))["Skills"]![_skill.ToString()]!["Rank"]!.Value<int>().Should().Be(1);
    }

    [Test]
    public void ALargeAward_CrossesSeveralRanksAndKeepsTheRemainder()
    {
        var cost = RequiredXP(0) + RequiredXP(1) + RequiredXP(2) + 5;

        var applied = Skill.GiveSkillXP(Hero, _skill, cost, ignoreBonuses: true, applyHenchmanPenalty: false);

        applied.Should().Be(cost);
        Db.EndFrame();
        var record = Db.Get<Player>(HeroId);
        record.Skills[_skill].Rank.Should().Be(3);
        record.Skills[_skill].XP.Should().Be(5);
        record.UnallocatedSP.Should().Be(3);
    }

    [Test]
    public void XPDebt_IsRepaidBeforeTheSkillGains()
    {
        var record = Db.Get<Player>(HeroId);
        record.XPDebt = 10;
        Db.Set(record);
        Db.EndFrame();

        var applied = Skill.GiveSkillXP(Hero, _skill, 25, ignoreBonuses: true, applyHenchmanPenalty: false);

        applied.Should().Be(25);
        Db.EndFrame();
        var after = Db.Get<Player>(HeroId);
        after.XPDebt.Should().Be(0);
        after.Skills[_skill].XP.Should().Be(15);
    }

    [Test]
    public void ADeadPlayer_GainsNothingAndTheRecordIsUntouched()
    {
        Engine.SetReturn("GetIsDead", true);
        var before = Db.GetRawJson<Player>(HeroId);

        Skill.GiveSkillXP(Hero, _skill, 100).Should().Be(0);

        Db.GetRawJson<Player>(HeroId).Should().Be(before);
    }
}
