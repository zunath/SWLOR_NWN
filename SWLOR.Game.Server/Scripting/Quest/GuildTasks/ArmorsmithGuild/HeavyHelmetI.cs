using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyHelmetI: AbstractQuest
    {
        public HeavyHelmetI()
        {
            CreateQuest(133, "Armorsmith Guild Task: 1x Heavy Helmet I", "arm_tsk_133")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_h1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
