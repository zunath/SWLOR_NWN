using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGA1: AbstractQuest
    {
        public HeavyVibrobladeGA1()
        {
            CreateQuest(258, "Weaponsmith Guild Task: 1x Heavy Vibroblade GA1", "wpn_tsk_258")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "greataxe_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
