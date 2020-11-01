using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class AuxiliaryThrusterMedium: AbstractQuest
    {
        public AuxiliaryThrusterMedium()
        {
            CreateQuest(504, "Engineering Guild Task: 1x Auxiliary Thruster (Medium)", "eng_tsk_504")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssspd2", 1, true)

                .AddRewardGold(510)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 111);
        }
    }
}
