using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceNecklaceII: AbstractQuest
    {
        public ForceNecklaceII()
        {
            CreateQuest(150, "Armorsmith Guild Task: 1x Force Necklace II", "arm_tsk_150")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_neck_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
