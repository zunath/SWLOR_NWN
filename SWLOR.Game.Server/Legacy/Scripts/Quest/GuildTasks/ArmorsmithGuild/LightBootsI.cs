using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBootsI: AbstractQuest
    {
        public LightBootsI()
        {
            CreateQuest(138, "Armorsmith Guild Task: 1x Light Boots I", "arm_tsk_138")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_boots_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
