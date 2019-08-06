using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
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

                .AddRewardGold(250)
                .AddRewardFame(2, 20)
                
                .OnAccepted((player, questGiver) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, 6);
                });
        }
    }
}
