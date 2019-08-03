namespace SWLOR.Game.Server.Quest
{
    public class TestQuest: AbstractQuest
    {
        public TestQuest()
        {
            CreateQuest("my quest name", "my_journal_tag")
                .AddObjectiveKillTarget(1, "mynock", 5)
                .AddRewardGold(100);

        }
    }
}
