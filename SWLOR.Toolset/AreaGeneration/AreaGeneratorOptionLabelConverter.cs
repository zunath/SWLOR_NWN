using System.Globalization;
using Avalonia.Data.Converters;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;

namespace SWLOR.Toolset.AreaGeneration;

/// <summary>Provides builder-facing labels for enum-backed Area Generator choices.</summary>
public sealed class AreaGeneratorOptionLabelConverter : IValueConverter
{
    public static AreaGeneratorOptionLabelConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            AreaPreviewMode.Schematic => "Schematic",
            AreaPreviewMode.MapGraphics => "Map graphics",
            DungeonLayoutStyle.RoomsAndCorridors => "Rooms and corridors",
            DungeonLayoutStyle.OrganicCave => "Organic cave",
            DungeonLayoutStyle.Warren => "Warren",
            DungeonLayoutStyle.PackedRooms => "Packed rooms",
            DungeonLayoutStyle.Labyrinth => "Labyrinth",
            _ => value?.ToString() ?? string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
