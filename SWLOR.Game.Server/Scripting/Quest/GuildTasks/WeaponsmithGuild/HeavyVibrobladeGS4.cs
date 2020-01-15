using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGS4: AbstractQuest
    {
        public HeavyVibrobladeGS4()
        {
            CreateQuest(334, "Weaponsmith Guild Task: 1x Heavy Vibroblade GS4", "wpn_tsk_334")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 4)


                .AddObjectiveCollectItem(1, "greatsword_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
