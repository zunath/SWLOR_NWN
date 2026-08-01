using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Runtime travel systems resolve these destinations with GetWaypointByTag, so a declared
    /// landing, orbit, taxi, death-respawn, or rebuild tag must identify one placement.
    /// </summary>
    public sealed class SingletonWaypointDestinationRule : IValidationRule
    {
        public string RuleId => "SingletonWaypointDestination";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            if (context.GameCodeIndex is not { IsSourceScanAvailable: true } gameCodeIndex)
                return Array.Empty<ValidationIssue>();

            var catalog = new WaypointBehaviorCatalog(gameCodeIndex, transitionDestinationTags: null);
            return context.Workspace.TagIndex.WaypointTagCounts
                .Where(pair => pair.Value > 1 && catalog.IsSingletonDestinationTag(pair.Key))
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair =>
                {
                    var area = context.Workspace.TagIndex.FindAreaDefiningTag(pair.Key, ResourceType.Utw);
                    return new ValidationIssue(
                        ValidationSeverity.Error,
                        RuleId,
                        $"Waypoint destination tag '{pair.Key}' is placed {pair.Value} times; this runtime destination must be unique.",
                        area == null ? null : context.GetGitPath(area),
                        area);
                })
                .ToList();
        }
    }
}
