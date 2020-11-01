using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
