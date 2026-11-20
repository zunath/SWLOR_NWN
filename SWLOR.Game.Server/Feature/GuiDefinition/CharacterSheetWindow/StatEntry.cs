namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>One row of the stats table: a label, its value and its tooltip.</summary>
    public sealed class StatEntry
    {
        public string Name { get; }
        public string Value { get; }
        public string Tooltip { get; }

        public StatEntry(string name, string value, string tooltip)
        {
            Name = name;
            Value = value;
            Tooltip = tooltip;
        }
    }
}
