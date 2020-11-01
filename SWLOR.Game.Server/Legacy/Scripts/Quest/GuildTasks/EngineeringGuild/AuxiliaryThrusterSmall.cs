using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class AuxiliaryThrusterSmall: AbstractQuest
    {
        public AuxiliaryThrusterSmall()
        {
            CreateQuest(452, "Engineering Guild Task: 1x Auxiliary Thruster (Small)", "eng_tsk_452")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssspd1", 1, true)

                .AddRewardGold(340)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 73);
        }
    }
}
