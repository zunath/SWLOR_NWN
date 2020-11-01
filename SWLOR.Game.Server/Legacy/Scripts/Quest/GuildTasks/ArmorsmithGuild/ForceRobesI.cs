using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceRobesI: AbstractQuest
    {
        public ForceRobesI()
        {
            CreateQuest(128, "Armorsmith Guild Task: 1x Force Robes I", "arm_tsk_128")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_robe_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
