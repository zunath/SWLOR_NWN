using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class LightsaberRepairKitIII: AbstractQuest
    {
        public LightsaberRepairKitIII()
        {
            CreateQuest(483, "Engineering Guild Task: 1x Lightsaber Repair Kit III", "eng_tsk_483")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ls_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 68);
        }
    }
}
