using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class CloakingGeneratorSmall: AbstractQuest
    {
        public CloakingGeneratorSmall()
        {
            CreateQuest(458, "Engineering Guild Task: 1x Cloaking Generator (Small)", "eng_tsk_458")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 3)


                .AddObjectiveCollectItem(1, "ssstlth1", 1, true)

                .AddRewardGold(340)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 73);
        }
    }
}
