using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
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
