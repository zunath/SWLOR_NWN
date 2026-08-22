#nullable disable

using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    public enum StreetDressingKind
    {
        RoadMarking = 0,
        MarginAccent = 1
    }

    public enum DecorationSize
    {
        Medium = 0,
        Small = 1,
        Large = 2,
        Huge = 3
    }

    public enum DistrictFlavor
    {
        None = 0,
        Industrial = 1,
        Commercial = 2,
        Civic = 3
    }

    public enum FeatureZoneDressing
    {
        None = 0,
        Lawn = 1,
        Centerpiece = 2
    }

    public enum DecorationRole
    {
        Fixture = 0,
        Clutter = 1,
        GroundDecal = 2,
        Landmark = 3
    }

    /// <summary>A weighted, multi-placeable decoration grouping.</summary>
    public class DungeonVignette
    {
        public string Key { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public List<DungeonVignetteMember> Members { get; set; } = new();
    }

    public class DungeonVignetteMember
    {
        public string Resref { get; set; } = string.Empty;
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float FacingOffset { get; set; }
    }
}
