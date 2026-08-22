namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Baseline per-kill estimate for one item in the selected configured drop.</summary>
    public sealed class CreatureExpectedLootItemViewModel
    {
        public string Name { get; }
        public string ResRef { get; }
        public double ExpectedQuantity { get; }

        public bool ShowsResRef => !string.Equals(Name, ResRef, StringComparison.OrdinalIgnoreCase);
        public string ExpectedDisplay => $"≈ {ExpectedQuantity:0.##} per kill";

        public CreatureExpectedLootItemViewModel(string name, string resRef, double expectedQuantity)
        {
            Name = string.IsNullOrWhiteSpace(name) ? resRef : name;
            ResRef = resRef;
            ExpectedQuantity = expectedQuantity;
        }
    }
}
