using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceHelmetIV: AbstractQuest
    {
        public ForceHelmetIV()
        {
            CreateQuest(198, "Armorsmith Guild Task: 1x Force Helmet IV", "arm_tsk_198")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_f4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
