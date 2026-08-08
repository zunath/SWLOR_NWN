namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>One option of the combined genericdoors/doortypes appearance picker.</summary>
    public sealed record DoorAppearanceChoice(
        DoorAppearanceKind Kind,
        int Id,
        string Display,
        string? Model,
        bool IsDoorTransition = false)
    {
        public override string ToString() => Display;
    }
}
