using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class StarshipAuxiliaryBlaster: AbstractQuest
    {
        public StarshipAuxiliaryBlaster()
        {
            CreateQuest(496, "Engineering Guild Task: 1x Starship Auxiliary Blaster", "eng_tsk_496")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "sswpn1", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 68);
        }
    }
}
