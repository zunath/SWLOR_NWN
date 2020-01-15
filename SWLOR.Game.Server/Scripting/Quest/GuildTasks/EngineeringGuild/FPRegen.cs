using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class FPRegen: AbstractQuest
    {
        public FPRegen()
        {
            CreateQuest(423, "Engineering Guild Task: 1x FP Regen", "eng_tsk_423")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 2)


                .AddObjectiveCollectItem(1, "rune_manareg1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
