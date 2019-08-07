using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyCrestIV: AbstractQuest
    {
        public HeavyCrestIV()
        {
            CreateQuest(203, "Armorsmith Guild Task: 1x Heavy Crest IV", "arm_tsk_203")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "h_crest_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
