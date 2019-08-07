using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class WarWithTheMandalorianWarriors: AbstractQuest
    {
        public WarWithTheMandalorianWarriors()
        {
            CreateQuest(20, "War With the Mandalorian Warriors", "war_mand_warriors")
                .AddPrerequisiteFame(3, 30)
                .AddPrerequisiteQuest(17)

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_MandalorianWarriors, 9)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(200)
                .AddRewardFame(3, 20)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
