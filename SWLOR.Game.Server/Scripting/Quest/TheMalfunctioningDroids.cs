using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class TheMalfunctioningDroids: AbstractQuest
    {
        public TheMalfunctioningDroids()
        {
            CreateQuest(10, "The Malfunctioning Droids", "malfun_droids")
                .AddPrerequisiteQuest(8)

                .AddObjectiveKillTarget(1, NPCGroupType.CZ220_MalfunctioningDroids, 5)
                .AddObjectiveTalkToNPC(2)

                .AddRewardFame(FameRegion.CZ220, 10)
                .AddRewardGold(100)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
