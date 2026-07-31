namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One item blueprint and the UTC slots its base item can occupy.</summary>
    public sealed record CreatureEquipmentChoice(
        string ResRef,
        string Display,
        int BaseItem,
        int EquipableSlots = 0)
    {
        public override string ToString() => Display;
    }
}
