using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class LargeBlade: AbstractQuest
    {
        public LargeBlade()
        {
            CreateQuest(240, "Weaponsmith Guild Task: 1x Large Blade", "wpn_tsk_240")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 0)


                .AddObjectiveCollectItem(1, "large_blade", 1, true)

                .AddRewardGold(20)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 5);
        }
    }
}
