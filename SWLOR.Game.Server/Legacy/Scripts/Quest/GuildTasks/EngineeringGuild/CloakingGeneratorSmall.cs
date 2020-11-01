using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class CloakingGeneratorSmall: AbstractQuest
    {
        public CloakingGeneratorSmall()
        {
            CreateQuest(458, "Engineering Guild Task: 1x Cloaking Generator (Small)", "eng_tsk_458")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssstlth1", 1, true)

                .AddRewardGold(340)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 73);
        }
    }
}
