using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ArmorClassI: AbstractQuest
    {
        public ArmorClassI()
        {
            CreateQuest(364, "Engineering Guild Task: 1x Armor Class I", "eng_tsk_364")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_ac1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
