using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class WarWithTheMandalorianWarriors: AbstractQuest
    {
        public WarWithTheMandalorianWarriors()
        {
            CreateQuest(20, "War With the Mandalorian Warriors", "war_mand_warriors")
                .AddPrerequisiteQuest(17)

                .AddObjectiveKillTarget(1, NPCGroup.MandalorianWarriors, 9)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(200)
                .AddRewardFame(FameRegion.VelesColony, 20)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
