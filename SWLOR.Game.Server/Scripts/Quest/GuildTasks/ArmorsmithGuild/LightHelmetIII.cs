using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightHelmetIII: AbstractQuest
    {
        public LightHelmetIII()
        {
            CreateQuest(188, "Armorsmith Guild Task: 1x Light Helmet III", "arm_tsk_188")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_l3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
