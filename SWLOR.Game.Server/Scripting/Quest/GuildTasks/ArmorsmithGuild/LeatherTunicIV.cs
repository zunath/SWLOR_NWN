using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LeatherTunicIV: AbstractQuest
    {
        public LeatherTunicIV()
        {
            CreateQuest(207, "Armorsmith Guild Task: 1x Leather Tunic IV", "arm_tsk_207")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "leather_tunic_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
