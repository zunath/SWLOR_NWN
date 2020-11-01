using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class RangedWeaponCore: AbstractQuest
    {
        public RangedWeaponCore()
        {
            CreateQuest(359, "Engineering Guild Task: 1x Ranged Weapon Core", "eng_tsk_359")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "r_weapon_core", 1, true)

                .AddRewardGold(55)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 14);
        }
    }
}
