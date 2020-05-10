using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
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
