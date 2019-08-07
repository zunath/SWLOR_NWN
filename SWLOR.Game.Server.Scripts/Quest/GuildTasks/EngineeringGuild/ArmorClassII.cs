using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ArmorClassII: AbstractQuest
    {
        public ArmorClassII()
        {
            CreateQuest(448, "Engineering Guild Task: 1x Armor Class II", "eng_tsk_448")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_ac2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
