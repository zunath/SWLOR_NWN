using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyHelmetIII: AbstractQuest
    {
        public HeavyHelmetIII()
        {
            CreateQuest(157, "Armorsmith Guild Task: 1x Heavy Helmet III", "arm_tsk_157")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_h3", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
