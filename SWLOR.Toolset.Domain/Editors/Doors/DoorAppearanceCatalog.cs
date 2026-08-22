using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>The single picker presented over the door format's two appearance fields.</summary>
    public static class DoorAppearanceCatalog
    {
        public static IReadOnlyList<DoorAppearanceChoice> Read(DoorTypeService? doors)
        {
            if (doors == null)
                return Array.Empty<DoorAppearanceChoice>();

            var choices = doors.GetGenericAll()
                .Where(row => !string.IsNullOrWhiteSpace(row.Model))
                .Select(row => new DoorAppearanceChoice(
                    DoorAppearanceKind.Generic,
                    row.Id,
                    $"Generic \u25b8 {row.DisplayName}",
                    row.Model,
                    IsDoorTransition: !row.VisibleModel))
                .Concat(
                    doors.GetAll()
                        .Where(row => row.Id > 0 && !string.IsNullOrWhiteSpace(row.Model))
                        .Select(row => new DoorAppearanceChoice(
                            DoorAppearanceKind.Specific,
                            row.Id,
                            $"Specific \u25b8 {row.DisplayName}",
                            row.Model,
                            IsDoorTransition: !row.VisibleModel)))
                .ToList();

            return choices;
        }
    }
}
