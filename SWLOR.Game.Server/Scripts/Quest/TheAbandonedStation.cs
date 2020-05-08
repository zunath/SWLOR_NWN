using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class TheAbandonedStation: AbstractQuest
    {
        public TheAbandonedStation()
        {
            CreateQuest(31, "The Abandoned Station", "aban_station")
                .AddPrerequisiteQuest(29)

                .AddObjectiveKillTarget(1, NPCGroupType.AbandonedStation_Boss, 1)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(4000)
                .AddRewardFame(1, 20);
        }
    }
}
