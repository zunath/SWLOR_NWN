namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>One row of the crafting table.</summary>
    public sealed class CraftEntry
    {
        public string Name { get; }
        public string Control { get; }
        public string Craftsmanship { get; }

        public CraftEntry(string name, string control, string craftsmanship)
        {
            Name = name;
            Control = control;
            Craftsmanship = craftsmanship;
        }
    }
}
