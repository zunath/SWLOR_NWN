using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
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
