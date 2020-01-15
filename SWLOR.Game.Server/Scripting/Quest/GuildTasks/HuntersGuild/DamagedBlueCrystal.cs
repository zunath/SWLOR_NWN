using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class DamagedBlueCrystal: AbstractQuest
    {
        public DamagedBlueCrystal()
        {
            CreateQuest(599, "Hunter's Guild Task: 6x Damaged Blue Crystal", "hun_tsk_599")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 2)


                .AddObjectiveCollectItem(1, "p_crystal_blue", 6, false)

                .AddRewardGold(122)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 39);
        }
    }
}
