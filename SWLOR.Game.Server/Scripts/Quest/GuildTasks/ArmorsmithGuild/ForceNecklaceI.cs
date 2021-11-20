using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceNecklaceI: AbstractQuest
    {
        public ForceNecklaceI()
        {
            CreateQuest(127, "Armorsmith Guild Task: 1x Force Necklace I", "arm_tsk_127")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_neck_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
