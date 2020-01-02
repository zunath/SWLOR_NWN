using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class AuxiliaryTargeterBasic: AbstractQuest
    {
        public AuxiliaryTargeterBasic()
        {
            CreateQuest(451, "Engineering Guild Task: 1x Auxiliary Targeter (Basic)", "eng_tsk_451")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssrang1", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
