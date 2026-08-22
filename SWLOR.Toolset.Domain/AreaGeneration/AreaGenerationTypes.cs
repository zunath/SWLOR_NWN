#nullable disable

using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>Resolved physical tile selection for one generated grid cell.</summary>
    public class ResolvedTile
    {
        public int TileId { get; set; }
        public int Orientation { get; set; }
        public int Height { get; set; }
    }

    public enum RoomRole
    {
        Entrance = 0,
        Standard = 1,
        Boss = 2
    }

    public enum TransitionKind
    {
        Entrance = 0,
        Exit = 1
    }

    /// <summary>How a layout realizes connections between rooms.</summary>
    public enum CorridorMode
    {
        OpenLane = 0,
        Tunnel = 1
    }

    public enum TransitionStyle
    {
        Placeable = 0,
        Door = 1,
        GroupExit = 2
    }

    public enum DungeonLayoutStyle
    {
        RoomsAndCorridors = 0,
        OrganicCave = 1,
        Warren = 2,
        PackedRooms = 3,
        Labyrinth = 4
    }

    /// <summary>Which crosser vocabulary Tunnel-mode corridors carve.</summary>
    public enum CorridorCrosserType
    {
        Corridor = 0,
        Alley = 1,
        Custom = 2
    }

    public class DungeonCreatureEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
    }

    /// <summary>Outcome of one <see cref="LayoutSolver.Solve"/> call.</summary>
    public sealed class LayoutSolverResult
    {
        public bool Success { get; init; }
        public MacroLayout Layout { get; init; }
        public MacroLayoutParameters Parameters { get; init; }
        public ResolvedLayout Resolved { get; init; }
        public int AttemptSeed { get; init; }
        public string FailureReason { get; init; }
    }

    /// <summary>Per-tile lighting written into every generated tile of a theme.</summary>
    public class DungeonTileLighting
    {
        public int MainLight1 { get; set; }
        public int MainLight2 { get; set; }
        public int SourceLight1 { get; set; } = 8;
        public int SourceLight2 { get; set; } = 8;
    }

    /// <summary>A tunnel segment connecting two open regions through solid cells.</summary>
    public class TunnelLink
    {
        public (int X, int Y) CornerA { get; set; }
        public (int X, int Y) CornerB { get; set; }
        public int Length { get; set; }
    }

    /// <summary>Tier-specific dungeon spawn and reward content.</summary>
    public class DungeonTierDetail
    {
        public int Tier { get; set; }
        public List<DungeonCreatureEntry> Creatures { get; set; } = new();
        public int MinCreaturesPerRoom { get; set; } = 1;
        public int MaxCreaturesPerRoom { get; set; } = 2;
        public string BossResref { get; set; } = string.Empty;
        public string TreasureLootTableId { get; set; } = string.Empty;
        public int TreasureItemCount { get; set; } = 1;
        public string LevelNote { get; set; } = string.Empty;
    }

    /// <summary>Smallest square area size each layout style reliably supports.</summary>
    public static class LayoutStyleSizeFloor
    {
        public static int For(DungeonLayoutStyle style)
        {
            return style switch
            {
                DungeonLayoutStyle.OrganicCave => 12,
                DungeonLayoutStyle.Warren => 8,
                DungeonLayoutStyle.PackedRooms => 9,
                DungeonLayoutStyle.RoomsAndCorridors => 11,
                DungeonLayoutStyle.Labyrinth => 8,
                _ => 12
            };
        }
    }

    public class LayoutRoom
    {
        public int Id { get; set; }
        public RoomRole Role { get; set; }
        public (int X, int Y) CenterTile { get; set; }
        public List<(int X, int Y)> Tiles { get; set; } = new();
        public bool IsSetPiece { get; set; }
        public string OpenTerrain { get; set; } = string.Empty;
    }

    /// <summary>A point where the area connects to the outside world.</summary>
    public class TransitionPoint
    {
        public TransitionKind Kind { get; set; }
        public (int X, int Y) Tile { get; set; }
        public int RoomId { get; set; }
        public TransitionStyle Style { get; set; } = TransitionStyle.Placeable;
        public (int X, int Y) DoorCell { get; set; }
        public (int X, int Y) DoorwayCell { get; set; }
        public float DoorX { get; set; }
        public float DoorY { get; set; }
        public float DoorZ { get; set; }
        public float DoorOrientation { get; set; }
        /// <summary>Specific doortypes.2da appearance declared by the selected tileset door slot; 0 uses the theme's generic door.</summary>
        public int DoorType { get; set; }
    }
}
