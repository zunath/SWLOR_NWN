using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBS4: AbstractQuest
    {
        public VibrobladeBS4()
        {
            CreateQuest(345, "Weaponsmith Guild Task: 1x Vibroblade BS4", "wpn_tsk_345")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bst_sword_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
