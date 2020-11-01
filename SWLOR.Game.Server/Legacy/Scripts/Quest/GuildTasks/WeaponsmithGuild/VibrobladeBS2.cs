using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBS2: AbstractQuest
    {
        public VibrobladeBS2()
        {
            CreateQuest(295, "Weaponsmith Guild Task: 1x Vibroblade BS2", "wpn_tsk_295")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bst_sword_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
