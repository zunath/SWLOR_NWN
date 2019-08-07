using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FPII: AbstractQuest
    {
        public FPII()
        {
            CreateQuest(470, "Engineering Guild Task: 1x FP II", "eng_tsk_470")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_mana2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
