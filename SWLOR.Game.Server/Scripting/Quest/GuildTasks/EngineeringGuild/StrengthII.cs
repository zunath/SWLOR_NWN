using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class StrengthII: AbstractQuest
    {
        public StrengthII()
        {
            CreateQuest(446, "Engineering Guild Task: 1x Strength II", "eng_tsk_446")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 2)


                .AddObjectiveCollectItem(1, "rune_str2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
