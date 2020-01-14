using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class RangedWeaponCore: AbstractQuest
    {
        public RangedWeaponCore()
        {
            CreateQuest(359, "Engineering Guild Task: 1x Ranged Weapon Core", "eng_tsk_359")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 0)


                .AddObjectiveCollectItem(1, "r_weapon_core", 1, true)

                .AddRewardGold(55)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 14);
        }
    }
}
