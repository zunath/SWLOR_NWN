using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ScanningArraySmall: AbstractQuest
    {
        public ScanningArraySmall()
        {
            CreateQuest(494, "Engineering Guild Task: 1x Scanning Array (Small)", "eng_tsk_494")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssscan1", 1, true)

                .AddRewardGold(340)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 73);
        }
    }
}
