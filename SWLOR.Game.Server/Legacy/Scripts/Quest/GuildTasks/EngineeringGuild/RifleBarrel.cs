using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class RifleBarrel: AbstractQuest
    {
        public RifleBarrel()
        {
            CreateQuest(361, "Engineering Guild Task: 1x Rifle Barrel", "eng_tsk_361")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rifle_barrel", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
