using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class LevelDecreaseI: AbstractQuest
    {
        public LevelDecreaseI()
        {
            CreateQuest(427, "Engineering Guild Task: 1x Level Decrease I", "eng_tsk_427")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 2)


                .AddObjectiveCollectItem(1, "rune_lvldown1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
