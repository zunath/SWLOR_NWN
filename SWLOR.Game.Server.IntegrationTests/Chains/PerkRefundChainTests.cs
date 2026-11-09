using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.IntegrationTests.Chains;

/// <summary>
/// A perk a character owns is refunded when its skill requirement stops being met: the perk registry built from the real
/// perk definitions, the skill-decay handler, and the record in Redis. The purchase itself is a button in the Perks window
/// (<c>PerksViewModel</c>), which needs the NUI engine, so the chain starts from a record that already owns the perk.
/// </summary>
[TestFixture]
public class PerkRefundChainTests : IntegrationTestBase
{
    private PerkType _perk;
    private SkillType _skill;
    private int _price;

    [SetUp]
    public void SetUpPlayer()
    {
        ServerCaches.EnsurePerks();
        ConfigurePlayer();
        Engine.SetReturn("OBJECT_SELF", Hero);

        // The first perk whose level 1 is gated by a skill rank and nothing else.
        foreach (var (type, detail) in Perk.GetAllPerks().OrderBy(pair => (int)pair.Key))
        {
            if (!detail.PerkLevels.TryGetValue(1, out var level) || level.Requirements.Count != 1 || level.Price <= 0)
                continue;
            if (level.Requirements[0] is not PerkRequirementSkill requirement || requirement.RequiredRank <= 0)
                continue;

            _perk = type;
            _skill = requirement.Type;
            _price = level.Price;
            var record = new Player(HeroId);
            foreach (var skill in Enum.GetValues<SkillType>().Where(skill => skill != SkillType.Invalid))
                record.Skills[skill] = new PlayerSkill();
            record.Skills[_skill].Rank = requirement.RequiredRank;
            record.Perks[_perk] = 1;
            Db.Set(record);
            Db.EndFrame();
            return;
        }

        Assert.Inconclusive("No perk with a single skill requirement at level 1.");
    }

    [Test]
    public void ASkillDroppingBelowTheRequirement_RefundsThePerkLevelAsSkillPoints()
    {
        Perk.GetPlayerEffectivePerkLevel(Hero, _perk).Should().Be(1);

        var record = Db.Get<Player>(HeroId);
        record.Skills[_skill].Rank = 0;
        Db.Set(record);
        Db.EndFrame();
        Fakes.Events.SetReturn("GetEventData", ((int)_skill).ToString());

        Perk.RemovePerkLevelOnSkillDecay();

        Db.EndFrame();
        var after = Db.Get<Player>(HeroId);
        after.Perks[_perk].Should().Be(0);
        after.UnallocatedSP.Should().Be(_price);
        Engine.CallsTo("FloatingTextStringOnCreature").Select(call => (string)call.Arguments[0]!)
            .Should().Contain(text => text.Contains("was refunded"));
    }

    [Test]
    public void ASkillThatStillMeetsTheRequirement_RefundsNothing()
    {
        Fakes.Events.SetReturn("GetEventData", ((int)_skill).ToString());
        var before = Db.GetRawJson<Player>(HeroId);

        Perk.RemovePerkLevelOnSkillDecay();

        Db.GetRawJson<Player>(HeroId).Should().Be(before);
    }
}
