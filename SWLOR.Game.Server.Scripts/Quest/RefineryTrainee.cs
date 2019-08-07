using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class RefineryTrainee: AbstractQuest
    {
        public RefineryTrainee()
        {
            CreateQuest(12, "Refinery Trainee", "refinery_trainee")
                .AddPrerequisiteQuest(1)

                .AddObjectiveCollectItem(1, "ref_veldite", 10, false)
                .AddObjectiveTalkToNPC(2)

                .EnableRewardSelection()
                .AddRewardGold(85)
                .AddRewardFame(2, 20)
                .AddRewardItem("rune_cstspd1", 1)
                .AddRewardItem("rune_mining1", 1)
                .AddRewardItem("run_dmg1", 1)
                .AddRewardItem("rune_dur1", 1);

        }
    }
}
