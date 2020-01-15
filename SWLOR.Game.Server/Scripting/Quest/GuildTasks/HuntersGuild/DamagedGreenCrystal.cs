using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class DamagedGreenCrystal: AbstractQuest
    {
        public DamagedGreenCrystal()
        {
            CreateQuest(600, "Hunter's Guild Task: 6x Damaged Green Crystal", "hun_tsk_600")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 2)


                .AddObjectiveCollectItem(1, "p_crystal_green", 6, false)

                .AddRewardGold(122)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 39);
        }
    }
}
