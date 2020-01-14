using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceScannerI: AbstractQuest
    {
        public ResourceScannerI()
        {
            CreateQuest(404, "Engineering Guild Task: 1x Resource Scanner I", "eng_tsk_404")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 1)


                .AddObjectiveCollectItem(1, "scanner_r_1", 1, true)

                .AddRewardGold(110)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}
