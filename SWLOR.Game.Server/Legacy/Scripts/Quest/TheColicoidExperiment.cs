using SWLOR.Game.Server.Legacy.Quest;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
{
    public class TheColicoidExperiment: AbstractQuest
    {
        public TheColicoidExperiment()
        {
            CreateQuest(11, "The Colicoid Experiment", "the_colicoid_experiment")
                .AddPrerequisiteQuest(8)
                .AddPrerequisiteQuest(10)

                .AddObjectiveKillTarget(1, NPCGroupType.CZ220_ColicoidExperiment, 1)
                .AddObjectiveTalkToNPC(2)

                .EnableRewardSelection()
                .AddRewardGold(250)
                .AddRewardFame(2, 20)
                .AddRewardItem("colicoid_cap_y", 1)
                .AddRewardItem("colicoid_cap_b", 1)
                .AddRewardItem("colicoid_cap_r", 1)
                .AddRewardItem("colicoid_cap_g", 1)
                
                .OnAccepted((player, questGiver) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, 6);
                });
        }
    }
}
