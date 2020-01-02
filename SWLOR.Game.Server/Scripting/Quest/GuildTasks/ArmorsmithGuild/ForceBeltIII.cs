using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceBeltIII: AbstractQuest
    {
        public ForceBeltIII()
        {
            CreateQuest(173, "Armorsmith Guild Task: 1x Force Belt III", "arm_tsk_173")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_belt_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
