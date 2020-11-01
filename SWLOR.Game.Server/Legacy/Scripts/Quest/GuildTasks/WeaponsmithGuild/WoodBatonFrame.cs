using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class WoodBatonFrame: AbstractQuest
    {
        public WoodBatonFrame()
        {
            CreateQuest(248, "Weaponsmith Guild Task: 1x Wood Baton Frame", "wpn_tsk_248")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "w_baton_frame", 1, true)

                .AddRewardGold(40)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 10);
        }
    }
}
