using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceBootsII: AbstractQuest
    {
        public ForceBootsII()
        {
            CreateQuest(148, "Armorsmith Guild Task: 1x Force Boots II", "arm_tsk_148")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_boots_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
