namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One item blueprint suitable for a visible creature equipment slot.</summary>
    public sealed record CreatureEquipmentChoice(string ResRef, string Display, int BaseItem)
    {
        public override string ToString() => Display;
    }
}
