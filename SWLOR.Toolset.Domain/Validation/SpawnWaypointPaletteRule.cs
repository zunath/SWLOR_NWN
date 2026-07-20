using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Fixed random-creature spawn points use a .utw waypoint blueprint whose Tag equals the spawn
    /// table ID (verified against the corpus: e.g. "wp_deadhand_ms".utw.json has
    /// Tag="CAPSTONE_DEADHAND_MS_SPAWN"). Every such waypoint blueprint's own resref must also
    /// appear as a RESREF entry in the waypoint palette ("waypointpalcus.itp.json") so it is
    /// placeable in the toolset. Skipped silently (no issues) when no game-code index with a
    /// successful source scan is available - there is nothing to recognize a "known spawn table
    /// ID" against.
    /// </summary>
    public sealed class SpawnWaypointPaletteRule : IValidationRule
    {
        private const string WaypointPaletteName = "waypointpalcus";

        public string RuleId => "SpawnWaypointPalette";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            var gameCodeIndex = context.GameCodeIndex;
            if (gameCodeIndex is not { IsSourceScanAvailable: true })
                return issues;

            var (palette, paletteError) = context.LoadPalette(WaypointPaletteName);
            if (paletteError != null || palette == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    RuleId,
                    $"Failed to parse waypoint palette '{WaypointPaletteName}': {paletteError?.Message}",
                    context.GetPalettePath(WaypointPaletteName),
                    null));
                return issues;
            }

            var paletteResRefs = new HashSet<string>(
                PaletteTraversal.EnumerateLeaves(palette).Select(node => node.ResRef!),
                StringComparer.OrdinalIgnoreCase);

            foreach (var resRef in context.ResRefsFor(ResourceType.Utw))
            {
                var (document, error) = context.LoadBlueprint(ResourceType.Utw, resRef);
                if (error != null || document is not UtwDocument utw)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        RuleId,
                        $"Failed to parse waypoint blueprint '{resRef}': {error?.Message}",
                        context.Workspace.GetResourcePath(ResourceType.Utw, resRef),
                        resRef));
                    continue;
                }

                var tag = utw.Tag;
                if (string.IsNullOrEmpty(tag) || !gameCodeIndex.IsValidSpawnTableId(tag))
                    continue;

                if (!paletteResRefs.Contains(resRef))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        RuleId,
                        $"Waypoint blueprint '{resRef}' (Tag='{tag}') matches a known spawn table ID but has no entry in the waypoint palette '{WaypointPaletteName}'.",
                        context.Workspace.GetResourcePath(ResourceType.Utw, resRef),
                        resRef));
                }
            }

            return issues;
        }
    }
}
