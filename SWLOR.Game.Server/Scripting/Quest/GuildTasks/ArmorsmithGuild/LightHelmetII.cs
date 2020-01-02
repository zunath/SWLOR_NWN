using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightHelmetII: AbstractQuest
    {
        public LightHelmetII()
        {
            CreateQuest(164, "Armorsmith Guild Task: 1x Light Helmet II", "arm_tsk_164")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_l2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
