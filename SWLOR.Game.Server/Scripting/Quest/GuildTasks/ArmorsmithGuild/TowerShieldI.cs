using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class TowerShieldI: AbstractQuest
    {
        public TowerShieldI()
        {
            CreateQuest(144, "Armorsmith Guild Task: 1x Tower Shield I", "arm_tsk_144")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 1)


                .AddObjectiveCollectItem(1, "tower_shield_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 28);
        }
    }
}
