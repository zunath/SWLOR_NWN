using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonM2: AbstractQuest
    {
        public BatonM2()
        {
            CreateQuest(275, "Weaponsmith Guild Task: 1x Baton M2", "wpn_tsk_275")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "mace_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
