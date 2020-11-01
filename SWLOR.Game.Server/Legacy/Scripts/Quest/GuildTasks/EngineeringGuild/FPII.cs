using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
