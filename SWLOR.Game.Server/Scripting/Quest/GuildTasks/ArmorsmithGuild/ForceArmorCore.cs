using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceArmorCore: AbstractQuest
    {
        public ForceArmorCore()
        {
            CreateQuest(115, "Armorsmith Guild Task: 1x Force Armor Core", "arm_tsk_115")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "core_f_armor", 1, true)

                .AddRewardGold(20)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 5);
        }
    }
}
