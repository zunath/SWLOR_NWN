using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyCrestIII: AbstractQuest
    {
        public HeavyCrestIII()
        {
            CreateQuest(181, "Armorsmith Guild Task: 1x Heavy Crest III", "arm_tsk_181")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "h_crest_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
