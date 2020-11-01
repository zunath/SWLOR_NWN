using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBeltII: AbstractQuest
    {
        public LightBeltII()
        {
            CreateQuest(161, "Armorsmith Guild Task: 1x Light Belt II", "arm_tsk_161")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_belt_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
