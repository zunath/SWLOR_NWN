using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class LevelDecreaseI: AbstractQuest
    {
        public LevelDecreaseI()
        {
            CreateQuest(427, "Engineering Guild Task: 1x Level Decrease I", "eng_tsk_427")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_lvldown1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
