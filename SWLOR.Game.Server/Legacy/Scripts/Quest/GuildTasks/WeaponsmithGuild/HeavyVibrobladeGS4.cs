using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGS4: AbstractQuest
    {
        public HeavyVibrobladeGS4()
        {
            CreateQuest(334, "Weaponsmith Guild Task: 1x Heavy Vibroblade GS4", "wpn_tsk_334")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "greatsword_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
