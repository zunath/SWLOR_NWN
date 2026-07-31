using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One item blueprint and the UTC slots its base item can occupy.</summary>
    public sealed record CreatureEquipmentChoice(
        string ResRef,
        string Display,
        int BaseItem,
        int EquipableSlots = 0,
        IReadOnlyList<ItemStatSummaryGroup>? StatGroups = null)
    {
        public IReadOnlyList<ItemStatSummaryGroup> Stats =>
            StatGroups ?? Array.Empty<ItemStatSummaryGroup>();

        public string StatSummary => ItemStatSummary.Compact(Stats);

        public override string ToString() => Display;
    }
}
