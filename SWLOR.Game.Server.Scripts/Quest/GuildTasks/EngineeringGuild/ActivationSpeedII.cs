using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ActivationSpeedII: AbstractQuest
    {
        public ActivationSpeedII()
        {
            CreateQuest(411, "Engineering Guild Task: 1x Activation Speed II", "eng_tsk_411")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cstspd2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
