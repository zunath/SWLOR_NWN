using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyCrestI: AbstractQuest
    {
        public HeavyCrestI()
        {
            CreateQuest(132, "Armorsmith Guild Task: 1x Heavy Crest I", "arm_tsk_132")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "h_crest_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
