using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeSS1: AbstractQuest
    {
        public FinesseVibrobladeSS1()
        {
            CreateQuest(257, "Weaponsmith Guild Task: 1x Finesse Vibroblade SS1", "wpn_tsk_257")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "shortsword_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
