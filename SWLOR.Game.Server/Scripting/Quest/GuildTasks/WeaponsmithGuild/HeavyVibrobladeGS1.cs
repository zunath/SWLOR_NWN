using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGS1: AbstractQuest
    {
        public HeavyVibrobladeGS1()
        {
            CreateQuest(259, "Weaponsmith Guild Task: 1x Heavy Vibroblade GS1", "wpn_tsk_259")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 1)


                .AddObjectiveCollectItem(1, "greatsword_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
