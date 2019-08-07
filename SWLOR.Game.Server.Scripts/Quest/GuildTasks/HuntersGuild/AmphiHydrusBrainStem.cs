using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class AmphiHydrusBrainStem: AbstractQuest
    {
        public AmphiHydrusBrainStem()
        {
            CreateQuest(607, "Hunter's Guild Task: 6x Amphi-Hydrus Brain Stem", "hun_tsk_607")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "amphi_brain2", 6, true)

                .AddRewardGold(212)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 52);
        }
    }
}
