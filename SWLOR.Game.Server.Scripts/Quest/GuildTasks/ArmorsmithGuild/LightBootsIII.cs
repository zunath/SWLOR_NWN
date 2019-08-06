using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBootsIII: AbstractQuest
    {
        public LightBootsIII()
        {
            CreateQuest(186, "Armorsmith Guild Task: 1x Light Boots III", "arm_tsk_186")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_boots_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
