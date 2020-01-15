using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class AuxiliaryShieldGeneratorSmall: AbstractQuest
    {
        public AuxiliaryShieldGeneratorSmall()
        {
            CreateQuest(450, "Engineering Guild Task: 1x Auxiliary Shield Generator (Small)", "eng_tsk_450")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 3)


                .AddObjectiveCollectItem(1, "ssshld1", 1, true)

                .AddRewardGold(360)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 78);
        }
    }
}
