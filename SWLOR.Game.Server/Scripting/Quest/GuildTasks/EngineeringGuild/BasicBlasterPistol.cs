using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicBlasterPistol: AbstractQuest
    {
        public BasicBlasterPistol()
        {
            CreateQuest(349, "Engineering Guild Task: 1x Basic Blaster Pistol", "eng_tsk_349")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "blaster_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 9);
        }
    }
}
