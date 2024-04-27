namespace SWLOR.Game.Server.Service.CraftService
{
    public class BlueprintDetail
    {
        public RecipeType Recipe { get; set; }
        public int Level { get; set; }
        public int LicensedRuns { get; set; }

        public BlueprintDetail()
        {
            Recipe = RecipeType.Invalid;
            Level = -1;
            LicensedRuns = -1;
        }
    }
}
