using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ActivationSpeedI: AbstractQuest
    {
        public ActivationSpeedI()
        {
            CreateQuest(363, "Engineering Guild Task: 1x Cooldown Reduction I", "eng_tsk_363")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cstspd1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
