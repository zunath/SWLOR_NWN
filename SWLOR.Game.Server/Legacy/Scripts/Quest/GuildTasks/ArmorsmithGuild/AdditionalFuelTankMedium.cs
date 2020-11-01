using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
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
