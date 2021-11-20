using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class SmallShieldIV: AbstractQuest
    {
        public SmallShieldIV()
        {
            CreateQuest(220, "Armorsmith Guild Task: 1x Small Shield IV", "arm_tsk_220")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "small_shield_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
