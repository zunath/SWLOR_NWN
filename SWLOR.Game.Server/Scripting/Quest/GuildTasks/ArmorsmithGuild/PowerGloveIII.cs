using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class PowerGloveIII: AbstractQuest
    {
        public PowerGloveIII()
        {
            CreateQuest(189, "Armorsmith Guild Task: 1x Power Glove III", "arm_tsk_189")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "powerglove_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
