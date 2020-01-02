using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicLightHelmet: AbstractQuest
    {
        public BasicLightHelmet()
        {
            CreateQuest(110, "Armorsmith Guild Task: 1x Basic Light Helmet", "arm_tsk_110")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_lb", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
