using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeSS4: AbstractQuest
    {
        public FinesseVibrobladeSS4()
        {
            CreateQuest(332, "Weaponsmith Guild Task: 1x Finesse Vibroblade SS4", "wpn_tsk_332")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "shortsword_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
