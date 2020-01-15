using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicVibrobladeBS: AbstractQuest
    {
        public BasicVibrobladeBS()
        {
            CreateQuest(237, "Weaponsmith Guild Task: 1x Basic Vibroblade BS", "wpn_tsk_237")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 0)


                .AddObjectiveCollectItem(1, "bst_sword_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
