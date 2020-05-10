using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ArmorsmithIII: AbstractQuest
    {
        public ArmorsmithIII()
        {
            CreateQuest(500, "Engineering Guild Task: 1x Armorsmith III", "eng_tsk_500")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_armsmth3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
