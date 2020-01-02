using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LargeShieldIV: AbstractQuest
    {
        public LargeShieldIV()
        {
            CreateQuest(206, "Armorsmith Guild Task: 1x Large Shield IV", "arm_tsk_206")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "large_shield_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
