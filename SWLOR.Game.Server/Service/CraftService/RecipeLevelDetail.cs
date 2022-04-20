namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipeLevelDetail
    {
        public int Progress { get; set; }
        public int Quality { get; set; }
        public int Durability { get; set; }
        public float DifficultyAdjustment { get; set; }

        public RecipeLevelDetail(int progress, int quality, int durability, float difficultyAdjustment)
        {
            Progress = progress;
            Quality = quality;
            Durability = durability;
            DifficultyAdjustment = difficultyAdjustment;
        }
    }
}
