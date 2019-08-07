using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FPRegen: AbstractQuest
    {
        public FPRegen()
        {
            CreateQuest(423, "Engineering Guild Task: 1x FP Regen", "eng_tsk_423")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_manareg1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
