using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
{
    public class TheMalfunctioningDroids: AbstractQuest
    {
        public TheMalfunctioningDroids()
        {
            CreateQuest(10, "The Malfunctioning Droids", "malfun_droids")
                .AddPrerequisiteQuest(8)

                .AddObjectiveKillTarget(1, NPCGroupType.CZ220_MalfunctioningDroids, 5)
                .AddObjectiveTalkToNPC(2)

                .AddRewardFame(2, 10)
                .AddRewardGold(100)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
