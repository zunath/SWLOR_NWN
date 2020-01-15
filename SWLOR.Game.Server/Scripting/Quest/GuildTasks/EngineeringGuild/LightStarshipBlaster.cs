using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class LightStarshipBlaster: AbstractQuest
    {
        public LightStarshipBlaster()
        {
            CreateQuest(536, "Engineering Guild Task: 1x Light Starship Blaster", "eng_tsk_536")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 4)


                .AddObjectiveCollectItem(1, "ship_blaster_1", 1, true)

                .AddRewardGold(470)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 101);
        }
    }
}
