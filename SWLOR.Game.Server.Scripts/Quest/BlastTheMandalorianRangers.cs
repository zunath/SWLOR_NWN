using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class BlastTheMandalorianRangers: AbstractQuest
    {
        public BlastTheMandalorianRangers()
        {
            CreateQuest(21, "Blast the Mandalorian Rangers", "blast_mand_rangers")
                .AddPrerequisiteFame(3, 30)
                .AddPrerequisiteQuest(20)

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_MandalorianRangers, 9)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(200)
                .AddRewardFame(3, 20)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
