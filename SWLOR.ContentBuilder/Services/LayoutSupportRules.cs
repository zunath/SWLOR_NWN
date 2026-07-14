using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Whether a given tileset profile can actually realize a given layout profile, shared between
    /// MainWindow's Layout Profile dropdown filter (RepopulateLayoutCombo) and ProjectFileService's
    /// load-time validation so a saved theme:tileset:layout pairing is checked against the identical
    /// rule the dropdown itself enforces, instead of a second hand-rolled copy drifting out of sync.
    /// </summary>
    internal static class LayoutSupportRules
    {
        /// <summary>
        /// An Alley-corridor profile (City Streets) on a tileset without the Alley crosser vocabulary
        /// silently downgrades to Corridor tunnels -- an identical result to the Corridor Complex
        /// profile -- so offering it as a separate choice is misleading. A Tunnel-mode profile
        /// (Complex or Streets after its own Alley downgrade) on a tileset missing the Doorway or
        /// Corridor crosser it needs (e.g. Barrows/tbw01) can never resolve at all, so it is rejected
        /// outright rather than offered and left to fail generation.
        /// </summary>
        public static bool Supports(DungeonTilesetProfile tilesetProfile, DungeonLayoutProfile layoutProfile)
        {
            if (tilesetProfile == null || layoutProfile == null) return true;
            if (layoutProfile.Template.CorridorMode != CorridorMode.Tunnel) return true;

            try
            {
                var model = TilesetModelCache.Get(tilesetProfile.TilesetResref);

                // Shape-aware, not just crosser-name presence: mirrors MacroLayoutGenerator's own
                // downgrade check (TunnelVocabularyCheck.SupportsTunnels) so this never accepts a
                // pairing the engine would itself downgrade away from Tunnel mode.
                var openTerrain = string.IsNullOrEmpty(tilesetProfile.PrimaryOpenTerrain)
                    ? model.FloorTerrain
                    : tilesetProfile.PrimaryOpenTerrain;
                var solidTerrain = string.IsNullOrEmpty(tilesetProfile.SolidTerrainOverride)
                    ? model.DefaultTerrain
                    : tilesetProfile.SolidTerrainOverride;

                return TunnelVocabularyCheck.SupportsTunnels(
                    model, openTerrain, tilesetProfile.SecondaryOpenTerrain, solidTerrain,
                    layoutProfile.Template.CorridorCrosserType);
            }
            catch
            {
                // If the tileset model can't load here, don't reject -- generation itself reports the
                // real failure and the engine downgrade keeps it safe.
                return true;
            }
        }
    }
}
