using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class FiberplastPadding: AbstractQuest
    {
        public FiberplastPadding()
        {
            CreateQuest(114, "Armorsmith Guild Task: 1x Fiberplast Padding", "arm_tsk_114")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "padding_fiber", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 15);
        }
    }
}
