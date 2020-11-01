using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightHelmetIV: AbstractQuest
    {
        public LightHelmetIV()
        {
            CreateQuest(211, "Armorsmith Guild Task: 1x Light Helmet IV", "arm_tsk_211")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_l4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
