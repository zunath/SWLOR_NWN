using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class AdditionalFuelTankMedium: AbstractQuest
    {
        public AdditionalFuelTankMedium()
        {
            CreateQuest(193, "Armorsmith Guild Task: 1x Additional Fuel Tank (Medium)", "arm_tsk_193")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssfuel2", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 84);
        }
    }
}
