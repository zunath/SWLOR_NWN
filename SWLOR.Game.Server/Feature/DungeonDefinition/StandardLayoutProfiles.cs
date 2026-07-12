using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Feature.DungeonDefinition
{
    /// <summary>
    /// Layout profiles modeled on hand-built SWLOR areas. Tileset-independent: any profile can
    /// compose with any tileset profile. AccentDensity expresses intent — the terrain name comes
    /// from the composed tileset profile (and is skipped where the tileset has none).
    /// </summary>
    public class StandardLayoutProfiles : IDungeonLayoutProfileListDefinition
    {
        public const string Organic = "organic";
        public const string Warren = "warren";
        public const string Packed = "packed";
        public const string Halls = "halls";

        private readonly DungeonLayoutProfileBuilder _builder = new();

        public Dictionary<string, DungeonLayoutProfile> BuildLayoutProfiles()
        {
            // Winding blobby caverns with pools (reference: Korriban Caverns, Mon Cala caves).
            _builder.Create(Organic, "Organic Cavern")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.OrganicCave;
                    p.OpenFillTarget = 0.48;
                    p.SmoothingPasses = 4;
                    p.CorridorWidth = 2;
                    p.AccentDensity = 0.06;
                });

            // Dense looping corridor maze with small chambers (reference: Veles Sewers).
            _builder.Create(Warren, "Corridor Warren")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.Warren;
                    p.CorridorWidth = 1;
                    p.LoopFactor = 0.3;
                    p.MinRooms = 3;
                    p.MaxRooms = 5;
                    p.MaxRoomCornerSize = 5;
                    p.AccentDensity = 0.05;
                });

            // Wall-sharing packed rooms joined by door gaps (reference: facility interiors).
            _builder.Create(Packed, "Packed Rooms")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.PackedRooms;
                    p.MinRoomCornerSize = 3;
                    p.MaxRoomCornerSize = 6;
                    p.LoopFactor = 0.25;
                    p.CorridorWidth = 1;
                });

            // Many varied chambers joined by broad looping halls (reference: crypt/temple interiors).
            _builder.Create(Halls, "Chambered Halls")
                .Configure(p =>
                {
                    p.Style = DungeonLayoutStyle.RoomsAndCorridors;
                    p.MinRooms = 6;
                    p.MaxRooms = 9;
                    p.MinRoomCornerSize = 3;
                    p.MaxRoomCornerSize = 6;
                    p.CorridorWidth = 2;
                    p.LoopFactor = 0.35;
                });

            return _builder.Build();
        }
    }
}
