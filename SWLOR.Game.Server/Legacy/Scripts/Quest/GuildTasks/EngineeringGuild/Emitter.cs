using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class Emitter: AbstractQuest
    {
        public Emitter()
        {
            CreateQuest(388, "Engineering Guild Task: 1x Emitter", "eng_tsk_388")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "emitter", 1, true)

                .AddRewardGold(90)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 20);
        }
    }
}
