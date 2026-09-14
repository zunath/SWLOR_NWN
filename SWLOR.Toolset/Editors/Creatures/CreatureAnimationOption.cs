namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Builder-facing preview segment mapped to an animation present on the model.</summary>
    public sealed record CreatureAnimationOption(string Display, string? AnimationName)
    {
        public override string ToString() => Display;
    }
}
