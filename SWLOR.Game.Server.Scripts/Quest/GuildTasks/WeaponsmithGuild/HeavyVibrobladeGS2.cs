using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGS2: AbstractQuest
    {
        public HeavyVibrobladeGS2()
        {
            CreateQuest(284, "Weaponsmith Guild Task: 1x Heavy Vibroblade GS2", "wpn_tsk_284")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "greatsword_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
