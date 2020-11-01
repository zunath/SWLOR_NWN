using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class Hyperdrive: AbstractQuest
    {
        public Hyperdrive()
        {
            CreateQuest(531, "Engineering Guild Task: 1x Hyperdrive", "eng_tsk_531")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "hyperdrive", 1, true)

                .AddRewardGold(490)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 107);
        }
    }
}
