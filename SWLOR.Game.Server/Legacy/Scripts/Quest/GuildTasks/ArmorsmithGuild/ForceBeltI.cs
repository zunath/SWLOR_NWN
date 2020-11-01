using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceBeltI: AbstractQuest
    {
        public ForceBeltI()
        {
            CreateQuest(124, "Armorsmith Guild Task: 1x Force Belt I", "arm_tsk_124")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_belt_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
