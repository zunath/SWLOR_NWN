using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class AdditionalFuelTankMedium: AbstractQuest
    {
        public AdditionalFuelTankMedium()
        {
            CreateQuest(193, "Armorsmith Guild Task: 1x Additional Fuel Tank (Medium)", "arm_tsk_193")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 4)


                .AddObjectiveCollectItem(1, "ssfuel2", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 84);
        }
    }
}
