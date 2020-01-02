using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class KathHoundHunting: AbstractQuest
    {
        public KathHoundHunting()
        {
            CreateQuest(14, "Kath Hound Hunting", "k_hound_hunting")
                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_WildlandKathHounds, 7)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(350)
                .AddRewardFame(3, 10);
        }
    }
}
