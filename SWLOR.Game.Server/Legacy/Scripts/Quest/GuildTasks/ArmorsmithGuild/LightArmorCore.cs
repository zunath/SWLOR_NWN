using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightArmorCore: AbstractQuest
    {
        public LightArmorCore()
        {
            CreateQuest(119, "Armorsmith Guild Task: 1x Light Armor Core", "arm_tsk_119")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "core_l_armor", 1, true)

                .AddRewardGold(20)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 5);
        }
    }
}
