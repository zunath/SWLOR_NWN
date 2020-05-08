using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class SmallShieldII: AbstractQuest
    {
        public SmallShieldII()
        {
            CreateQuest(167, "Armorsmith Guild Task: 1x Small Shield II", "arm_tsk_167")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "small_shield_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
