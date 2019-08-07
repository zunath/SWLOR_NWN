using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class SmallShieldI: AbstractQuest
    {
        public SmallShieldI()
        {
            CreateQuest(143, "Armorsmith Guild Task: 1x Small Shield I", "arm_tsk_143")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "small_shield_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
