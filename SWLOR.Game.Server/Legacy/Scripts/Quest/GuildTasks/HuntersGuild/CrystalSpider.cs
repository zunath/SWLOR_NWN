using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class CrystalSpider: AbstractQuest
    {
        public CrystalSpider()
        {
            CreateQuest(603, "Hunter's Guild Task: 10x Crystal Spider", "hun_tsk_603")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_CrystalSpider, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(122)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 39);
        }
    }
}
