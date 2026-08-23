#nullable disable
using SWLOR.Toolset.Domain.AreaGeneration.Definitions;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// Whether a given tileset profile can actually realize a given layout profile. The native
    /// generator uses this to hide pairings that would silently downgrade or fail resolution.
    /// </summary>
    public static class LayoutSupportRules
    {
        /// <summary>
        /// An Alley-corridor profile (City Streets) on a tileset without the Alley crosser vocabulary
        /// silently downgrades to Corridor tunnels -- an identical result to the Corridor Complex
        /// profile -- so offering it as a separate choice is misleading. A Tunnel-mode profile
        /// (Complex or Streets after its own Alley downgrade) on a tileset missing the Doorway or
        /// Corridor crosser it needs (e.g. Barrows/tbw01) can never resolve at all, so it is rejected
        /// outright rather than offered and left to fail generation.
        /// </summary>
        public static bool Supports(
            DungeonTilesetProfile tilesetProfile,
            DungeonLayoutProfile layoutProfile,
            TilesetModel model)
        {
            if (tilesetProfile == null || layoutProfile == null) return true;
            if (layoutProfile.Template.CorridorMode != CorridorMode.Tunnel) return true;

            // Shape-aware, not just crosser-name presence: mirrors MacroLayoutGenerator's own
            // downgrade check (TunnelVocabularyCheck.SupportsTunnels) so this never accepts a
            // pairing the engine would itself downgrade away from Tunnel mode.
            var openTerrain = string.IsNullOrEmpty(tilesetProfile.PrimaryOpenTerrain)
                ? model.FloorTerrain
                : tilesetProfile.PrimaryOpenTerrain;
            var solidTerrain = string.IsNullOrEmpty(tilesetProfile.SolidTerrainOverride)
                ? model.DefaultTerrain
                : tilesetProfile.SolidTerrainOverride;
            var crosserType = layoutProfile.Template.CorridorCrosserType;
            var bodyCrosser = layoutProfile.Template.TunnelBodyCrosser;
            var portCrosser = layoutProfile.Template.TunnelPortCrosser;
            if (crosserType == CorridorCrosserType.Corridor &&
                !string.IsNullOrEmpty(tilesetProfile.TunnelBodyCrosser) &&
                !string.IsNullOrEmpty(tilesetProfile.TunnelPortCrosser))
            {
                crosserType = CorridorCrosserType.Custom;
                bodyCrosser = tilesetProfile.TunnelBodyCrosser;
                portCrosser = tilesetProfile.TunnelPortCrosser;
            }

            return TunnelVocabularyCheck.SupportsTunnels(
                model, openTerrain, tilesetProfile.SecondaryOpenTerrain, solidTerrain,
                crosserType, bodyCrosser, portCrosser, tilesetProfile.DoorSlotCrossers);
        }
    }
}
