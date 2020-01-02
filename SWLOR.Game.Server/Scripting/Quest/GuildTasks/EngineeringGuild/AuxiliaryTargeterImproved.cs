using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class AuxiliaryTargeterImproved: AbstractQuest
    {
        public AuxiliaryTargeterImproved()
        {
            CreateQuest(503, "Engineering Guild Task: 1x Auxiliary Targeter (Improved)", "eng_tsk_503")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssrang2", 1, true)

                .AddRewardGold(450)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 95);
        }
    }
}
