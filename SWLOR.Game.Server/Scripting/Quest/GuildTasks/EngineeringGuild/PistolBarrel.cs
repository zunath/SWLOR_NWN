using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class PistolBarrel: AbstractQuest
    {
        public PistolBarrel()
        {
            CreateQuest(357, "Engineering Guild Task: 1x Pistol Barrel", "eng_tsk_357")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "pistol_barrel", 1, true)

                .AddRewardGold(40)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 10);
        }
    }
}
