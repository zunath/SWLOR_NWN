using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class StarshipAuxiliaryLightCannon: AbstractQuest
    {
        public StarshipAuxiliaryLightCannon()
        {
            CreateQuest(554, "Engineering Guild Task: 1x Starship Auxiliary Light Cannon", "eng_tsk_554")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "sswpn2", 1, true)

                .AddRewardGold(490)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 106);
        }
    }
}
