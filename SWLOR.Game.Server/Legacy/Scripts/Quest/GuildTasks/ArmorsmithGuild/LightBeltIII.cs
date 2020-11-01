using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBeltIII: AbstractQuest
    {
        public LightBeltIII()
        {
            CreateQuest(185, "Armorsmith Guild Task: 1x Light Belt III", "arm_tsk_185")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_belt_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
