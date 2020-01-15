using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicHeavyVibrobladeGA: AbstractQuest
    {
        public BasicHeavyVibrobladeGA()
        {
            CreateQuest(229, "Weaponsmith Guild Task: 1x Basic Heavy Vibroblade GA", "wpn_tsk_229")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 0)


                .AddObjectiveCollectItem(1, "greataxe_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
