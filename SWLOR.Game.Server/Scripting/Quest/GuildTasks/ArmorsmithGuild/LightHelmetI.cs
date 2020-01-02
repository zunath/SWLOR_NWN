using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightHelmetI: AbstractQuest
    {
        public LightHelmetI()
        {
            CreateQuest(140, "Armorsmith Guild Task: 1x Light Helmet I", "arm_tsk_140")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_l1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
