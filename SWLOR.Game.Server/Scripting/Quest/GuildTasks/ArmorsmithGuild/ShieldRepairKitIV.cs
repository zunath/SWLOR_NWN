using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class ShieldRepairKitIV: AbstractQuest
    {
        public ShieldRepairKitIV()
        {
            CreateQuest(219, "Armorsmith Guild Task: 1x Shield Repair Kit IV", "arm_tsk_219")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 4)


                .AddObjectiveCollectItem(1, "sh_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 88);
        }
    }
}
