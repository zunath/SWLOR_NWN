using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBootsII: AbstractQuest
    {
        public LightBootsII()
        {
            CreateQuest(162, "Armorsmith Guild Task: 1x Light Boots II", "arm_tsk_162")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_boots_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
