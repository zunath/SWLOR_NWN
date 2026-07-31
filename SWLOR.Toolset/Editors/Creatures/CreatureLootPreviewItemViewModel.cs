using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Readable catalog projection of one possible loot-table result.</summary>
    public sealed class CreatureLootPreviewItemViewModel
    {
        public string Name { get; }
        public string ResRef { get; }
        public int Weight { get; }
        public int MaximumQuantity { get; }
        public bool IsRare { get; }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? ResRef : Name;
        public bool ShowsResRef => !string.Equals(DisplayName, ResRef, StringComparison.OrdinalIgnoreCase);
        public string WeightDisplay => $"Weight {Weight}";
        public string QuantityDisplay => MaximumQuantity <= 1 ? "1 each" : $"1–{MaximumQuantity} each";
        public string RareDisplay => IsRare ? "Rare" : string.Empty;

        public CreatureLootPreviewItemViewModel(CreatureLootTableItemInfo item, Func<string, string>? resolveName)
        {
            ResRef = item.ResRef;
            Name = resolveName?.Invoke(item.ResRef) ?? item.ResRef;
            Weight = item.Weight;
            MaximumQuantity = item.MaximumQuantity;
            IsRare = item.IsRare;
        }
    }
}
