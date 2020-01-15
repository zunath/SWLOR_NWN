using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightArmorRepairKitII: AbstractQuest
    {
        public LightArmorRepairKitII()
        {
            CreateQuest(160, "Armorsmith Guild Task: 1x Light Armor Repair Kit II", "arm_tsk_160")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 2)


                .AddObjectiveCollectItem(1, "la_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 48);
        }
    }
}
