using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
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
