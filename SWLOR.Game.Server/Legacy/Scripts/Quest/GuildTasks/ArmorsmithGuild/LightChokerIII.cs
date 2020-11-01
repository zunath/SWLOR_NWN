using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightChokerIII: AbstractQuest
    {
        public LightChokerIII()
        {
            CreateQuest(187, "Armorsmith Guild Task: 1x Light Choker III", "arm_tsk_187")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lt_choker_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
