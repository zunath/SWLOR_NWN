using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class CloakingGeneratorMedium: AbstractQuest
    {
        public CloakingGeneratorMedium()
        {
            CreateQuest(511, "Engineering Guild Task: 1x Cloaking Generator (Medium)", "eng_tsk_511")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 4)


                .AddObjectiveCollectItem(1, "ssstlth2", 1, true)

                .AddRewardGold(510)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 111);
        }
    }
}
