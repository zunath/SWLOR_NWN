using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ScanningArrayMedium: AbstractQuest
    {
        public ScanningArrayMedium()
        {
            CreateQuest(552, "Engineering Guild Task: 1x Scanning Array (Medium)", "eng_tsk_552")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 4)


                .AddObjectiveCollectItem(1, "ssscan2", 1, true)

                .AddRewardGold(510)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 111);
        }
    }
}
