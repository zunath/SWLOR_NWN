using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ScanningArrayMedium: AbstractQuest
    {
        public ScanningArrayMedium()
        {
            CreateQuest(552, "Engineering Guild Task: 1x Scanning Array (Medium)", "eng_tsk_552")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssscan2", 1, true)

                .AddRewardGold(510)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 111);
        }
    }
}
