using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class WarocasMeat: AbstractQuest
    {
        public WarocasMeat()
        {
            CreateQuest(583, "Hunter's Guild Task: 6x Warocas Meat", "hun_tsk_583")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveCollectItem(1, "warocas_meat", 6, false)

                .AddRewardGold(67)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 14);
        }
    }
}
