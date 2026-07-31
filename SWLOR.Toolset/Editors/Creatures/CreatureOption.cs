namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One compact integer choice used outside the shared behavior row.</summary>
    public sealed record CreatureOption(int Value, string Display)
    {
        public override string ToString() => Display;
    }
}
