using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class LightsaberRepairKitIV: AbstractQuest
    {
        public LightsaberRepairKitIV()
        {
            CreateQuest(541, "Engineering Guild Task: 1x Lightsaber Repair Kit IV", "eng_tsk_541")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 4)


                .AddObjectiveCollectItem(1, "ls_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 88);
        }
    }
}
