using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyCrestII: AbstractQuest
    {
        public HeavyCrestII()
        {
            CreateQuest(155, "Armorsmith Guild Task: 1x Heavy Crest II", "arm_tsk_155")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "h_crest_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
