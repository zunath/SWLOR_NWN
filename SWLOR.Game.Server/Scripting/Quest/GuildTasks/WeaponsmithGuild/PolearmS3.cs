using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmS3: AbstractQuest
    {
        public PolearmS3()
        {
            CreateQuest(314, "Weaponsmith Guild Task: 1x Polearm S3", "wpn_tsk_314")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 3)


                .AddObjectiveCollectItem(1, "spear_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
