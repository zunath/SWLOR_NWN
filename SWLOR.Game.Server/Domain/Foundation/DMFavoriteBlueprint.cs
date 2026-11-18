namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// A blueprint a DM saved as a DM Creator favorite. The category, palette, and resref identify it; the name is
    /// only shown when the blueprint is no longer available.
    /// </summary>
    public class DMFavoriteBlueprint
    {
        public DMBlueprintCategory Category { get; set; }
        public DMBlueprintSource Source { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }

        public DMFavoriteBlueprint()
        {
            Resref = string.Empty;
            Name = string.Empty;
        }
    }
}
