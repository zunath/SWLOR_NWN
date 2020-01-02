using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.Hutlar
{
    public class CullTheTundraThreat: AbstractQuest
    {
        public CullTheTundraThreat()
        {
            CreateQuest(1001, "Cull the Tundra Tiger Threat", "tundra_tiger_threat")
                .AddObjectiveKillTarget(1, NPCGroupType.Hutlar_QionTigers, 10)
                .AddObjectiveTalkToNPC(2)
                
                .AddRewardGold(550)
                .AddRewardItem("qion_enh_device", 1);
        }
    }
}
