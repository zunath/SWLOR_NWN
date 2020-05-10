using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class HelpTheTalyronFamily: AbstractQuest
    {
        public HelpTheTalyronFamily()
        {
            CreateQuest(28, "Help the Talyron Family", "help_talyron_family")

                .AddObjectiveTalkToNPC(1)
                .AddObjectiveKillTarget(2, NPCGroupType.Viscara_ValleyCairnmogs, 10)
                .AddObjectiveTalkToNPC(3)
                .AddObjectiveTalkToNPC(4)

                .AddRewardGold(800)
                .AddRewardFame(4, 30)
                .AddRewardItem("xp_tome_2", 1);
        }
    }
}
