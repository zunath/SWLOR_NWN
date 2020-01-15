using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBS2: AbstractQuest
    {
        public VibrobladeBS2()
        {
            CreateQuest(295, "Weaponsmith Guild Task: 1x Vibroblade BS2", "wpn_tsk_295")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 2)


                .AddObjectiveCollectItem(1, "bst_sword_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
