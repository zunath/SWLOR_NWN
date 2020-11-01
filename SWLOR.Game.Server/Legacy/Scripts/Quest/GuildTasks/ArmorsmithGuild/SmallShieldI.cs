using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
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
