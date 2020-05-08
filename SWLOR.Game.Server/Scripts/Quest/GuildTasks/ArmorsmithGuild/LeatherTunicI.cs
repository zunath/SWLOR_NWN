using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
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
