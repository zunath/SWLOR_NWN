using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.Hutlar
{
    public class StupendiousSlugBile: AbstractQuest
    {
        public StupendiousSlugBile()
        {
            CreateQuest(1002, "Stupendious Slug Bile", "stup_slug_bile")
                .AddObjectiveCollectItem(1, "slug_bile", 5, false)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(742)
                .AddRewardItem("slug_shake", 1);
        }
    }
}
