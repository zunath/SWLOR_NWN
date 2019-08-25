using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.Hutlar
{
    public class HutlarPowerInvestigation: AbstractQuest
    {
        public HutlarPowerInvestigation()
        {
            CreateQuest(1003, "Hutlar Power Investigation", "hut_power_invest")
                .AddPrerequisiteQuest(1000)
                .AddPrerequisiteQuest(1001)
                .AddPrerequisiteQuest(1002)
                
                .AddObjectiveUseObject(1)
                .AddObjectiveUseObject(2)
                .AddObjectiveUseObject(3)
                .AddObjectiveUseObject(4)
                .AddObjectiveUseObject(5)
                .AddObjectiveTalkToNPC(6)
                
                .AddRewardGold(1200);
        }
    }
}
