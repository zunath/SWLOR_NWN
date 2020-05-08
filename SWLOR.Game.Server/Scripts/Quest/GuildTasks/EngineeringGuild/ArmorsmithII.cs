using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ArmorsmithII: AbstractQuest
    {
        public ArmorsmithII()
        {
            CreateQuest(449, "Engineering Guild Task: 1x Armorsmith II", "eng_tsk_449")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_armsmth2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
