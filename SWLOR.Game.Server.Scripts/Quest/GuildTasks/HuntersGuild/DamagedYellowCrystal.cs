using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class DamagedYellowCrystal: AbstractQuest
    {
        public DamagedYellowCrystal()
        {
            CreateQuest(602, "Hunter's Guild Task: 6x Damaged Yellow Crystal", "hun_tsk_602")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "p_crystal_yellow", 6, true)

                .AddRewardGold(122)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 39);
        }
    }
}
