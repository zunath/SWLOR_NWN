using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.IntegrationTests.Chains;

/// <summary>A quest from acceptance to completion: the quest registry, the quest detail and the player's record in Redis.</summary>
[TestFixture]
public class QuestChainTests : IntegrationTestBase
{
    private const string QuestId = "integration_two_step";

    [SetUp]
    public void RegisterQuest()
    {
        ConfigurePlayer();
        var quests = new QuestBuilder()
            .Create(QuestId, "Two Steps")
            .AddState()
            .SetStateJournalText("Step one.")
            .AddState()
            .SetStateJournalText("Step two.")
            .AddXPReward(250, false)
            .AddGoldReward(40, false)
            .Build();
        Service<IQuestService>().RegisterRuntimeQuest(quests[QuestId]);

        Db.Set(new Player(HeroId));
        Db.EndFrame();
    }

    [Test]
    public void AcceptAdvanceComplete_WritesEachStepToTheRecord()
    {
        var quest = Service<IQuestService>();

        quest.AcceptQuest(Hero, QuestId).Should().BeTrue();
        Db.EndFrame();
        var accepted = Db.Get<Player>(HeroId).Quests[QuestId];
        accepted.CurrentState.Should().Be(1);
        accepted.DateLastCompleted.Should().BeNull();

        quest.AdvanceQuest(Hero, Hero, QuestId).Should().BeTrue();
        Db.EndFrame();
        Db.Get<Player>(HeroId).Quests[QuestId].CurrentState.Should().Be(2);

        quest.AdvanceQuest(Hero, Hero, QuestId).Should().BeTrue("the last state completes the quest");
        Db.EndFrame();
        var completed = Db.Get<Player>(HeroId);
        completed.Quests[QuestId].TimesCompleted.Should().Be(1);
        completed.Quests[QuestId].DateLastCompleted.Should().NotBeNull();
        completed.UnallocatedXP.Should().Be(250);
        Engine.CountOf("GiveGoldToCreature").Should().Be(1);
        Messages().Should().Contain("Quest 'Two Steps' complete!");
    }

    [Test]
    public void ACompletedQuestThatIsNotRepeatable_CannotBeAcceptedAgain()
    {
        var quest = Service<IQuestService>();
        quest.AcceptQuest(Hero, QuestId);
        quest.AdvanceQuest(Hero, Hero, QuestId);
        quest.AdvanceQuest(Hero, Hero, QuestId);
        Db.EndFrame();
        var before = Db.GetRawJson<Player>(HeroId);

        quest.AcceptQuest(Hero, QuestId).Should().BeFalse();

        Db.GetRawJson<Player>(HeroId).Should().Be(before);
    }

    [Test]
    public void AbandoningAnActiveQuest_ClearsItsStateInTheRecord()
    {
        var quest = Service<IQuestService>();
        quest.AcceptQuest(Hero, QuestId);
        Db.EndFrame();

        quest.AbandonQuest(Hero, QuestId);

        Db.EndFrame();
        var record = Db.Get<Player>(HeroId);
        (record.Quests.ContainsKey(QuestId) ? record.Quests[QuestId].CurrentState : 0).Should().Be(0);
    }
}
