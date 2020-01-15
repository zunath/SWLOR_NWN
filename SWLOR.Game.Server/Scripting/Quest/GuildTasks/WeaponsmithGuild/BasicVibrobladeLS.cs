using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicVibrobladeLS: AbstractQuest
    {
        public BasicVibrobladeLS()
        {
            CreateQuest(239, "Weaponsmith Guild Task: 1x Basic Vibroblade LS", "wpn_tsk_239")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 0)


                .AddObjectiveCollectItem(1, "longsword_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
