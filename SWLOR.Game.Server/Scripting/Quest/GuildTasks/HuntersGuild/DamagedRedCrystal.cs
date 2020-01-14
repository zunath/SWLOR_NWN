using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class DamagedRedCrystal: AbstractQuest
    {
        public DamagedRedCrystal()
        {
            CreateQuest(601, "Hunter's Guild Task: 6x Damaged Red Crystal", "hun_tsk_601")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 2)


                .AddObjectiveCollectItem(1, "p_crystal_red", 6, false)

                .AddRewardGold(122)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 39);
        }
    }
}
