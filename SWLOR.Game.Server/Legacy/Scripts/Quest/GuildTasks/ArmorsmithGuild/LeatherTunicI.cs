using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LeatherTunicI: AbstractQuest
    {
        public LeatherTunicI()
        {
            CreateQuest(135, "Armorsmith Guild Task: 1x Leather Tunic I", "arm_tsk_135")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "leather_tunic_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
