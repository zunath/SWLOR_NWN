using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
{
    public class MandalorianDogTags: AbstractQuest
    {
        public MandalorianDogTags()
        {
            CreateQuest(19, "Mandalorian Dog Tags", "mand_dog_tags")
                .AddPrerequisiteQuest(17)

                .AddObjectiveCollectItem(1, "man_tags", 5, false)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(350)
                .AddRewardFame(3, 30);
        }
    }
}
