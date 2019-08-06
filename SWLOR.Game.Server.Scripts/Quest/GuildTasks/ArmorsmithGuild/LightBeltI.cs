using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBeltI: AbstractQuest
    {
        public LightBeltI()
        {
            CreateQuest(137, "Armorsmith Guild Task: 1x Light Belt I", "arm_tsk_137")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_belt_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
