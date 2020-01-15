using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeSS2: AbstractQuest
    {
        public FinesseVibrobladeSS2()
        {
            CreateQuest(282, "Weaponsmith Guild Task: 1x Finesse Vibroblade SS2", "wpn_tsk_282")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 2)


                .AddObjectiveCollectItem(1, "shortsword_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
