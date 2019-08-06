using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ShieldRepairKitII: AbstractQuest
    {
        public ShieldRepairKitII()
        {
            CreateQuest(166, "Armorsmith Guild Task: 1x Shield Repair Kit II", "arm_tsk_166")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "sh_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 48);
        }
    }
}
