using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class LightsaberRepairKitI: AbstractQuest
    {
        public LightsaberRepairKitI()
        {
            CreateQuest(400, "Engineering Guild Task: 1x Lightsaber Repair Kit I", "eng_tsk_400")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 1)


                .AddObjectiveCollectItem(1, "ls_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 28);
        }
    }
}
