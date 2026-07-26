namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>One named value of a Choice row.</summary>
    /// <remarks>
    /// ToString is the display name and nothing else. A combo box falls back to ToString when it has
    /// no item template, and the record default rendered the whole shape - "TriggerChoice { Value =
    /// 2, Display = Waypoint }" - which is what a builder actually saw in the list.
    /// </remarks>
    public sealed record TriggerChoice(long Value, string Display)
    {
        public override string ToString() => Display;
    }
}
