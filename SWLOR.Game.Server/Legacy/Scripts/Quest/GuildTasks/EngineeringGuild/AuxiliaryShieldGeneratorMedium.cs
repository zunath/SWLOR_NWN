using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class AuxiliaryShieldGeneratorMedium: AbstractQuest
    {
        public AuxiliaryShieldGeneratorMedium()
        {
            CreateQuest(502, "Engineering Guild Task: 1x Auxiliary Shield Generator (Medium)", "eng_tsk_502")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssshld2", 1, true)

                .AddRewardGold(460)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 98);
        }
    }
}
