using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ActivationSpeedIII: AbstractQuest
    {
        public ActivationSpeedIII()
        {
            CreateQuest(498, "Engineering Guild Task: 1x Cooldown Reduction III", "eng_tsk_498")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cstspd3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
