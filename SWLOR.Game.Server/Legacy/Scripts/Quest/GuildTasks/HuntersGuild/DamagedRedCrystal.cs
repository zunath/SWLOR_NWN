using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class DamagedRedCrystal: AbstractQuest
    {
        public DamagedRedCrystal()
        {
            CreateQuest(601, "Hunter's Guild Task: 6x Damaged Red Crystal", "hun_tsk_601")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "p_crystal_red", 6, false)

                .AddRewardGold(122)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 39);
        }
    }
}
