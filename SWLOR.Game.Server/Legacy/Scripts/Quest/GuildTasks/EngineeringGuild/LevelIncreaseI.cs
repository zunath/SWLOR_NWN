using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class LevelIncreaseI: AbstractQuest
    {
        public LevelIncreaseI()
        {
            CreateQuest(428, "Engineering Guild Task: 1x Level Increase I", "eng_tsk_428")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_lvlup1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
