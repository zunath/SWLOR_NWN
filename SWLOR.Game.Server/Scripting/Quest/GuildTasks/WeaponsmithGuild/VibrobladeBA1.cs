using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBA1: AbstractQuest
    {
        public VibrobladeBA1()
        {
            CreateQuest(269, "Weaponsmith Guild Task: 1x Vibroblade BA1", "wpn_tsk_269")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "battleaxe_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
