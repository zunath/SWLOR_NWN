using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Quest encounter activator placeables (OnUsed == "quest_enc") are world-instance-only by
    /// design - the placed area instance is the source of truth. Neither a .utp blueprint file
    /// nor a placeable-palette entry may carry OnUsed == "quest_enc".
    /// </summary>
    public sealed class QuestActivatorNotInPaletteRule : IValidationRule
    {
        private const string QuestEncounterOnUsed = "quest_enc";
        private const string PlaceablePaletteName = "placeablepalcus";

        public string RuleId => "QuestActivatorNotInPalette";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var issues = new List<ValidationIssue>();

            foreach (var resRef in context.ResRefsFor(ResourceType.Utp))
            {
                var (document, error) = context.LoadBlueprint(ResourceType.Utp, resRef);
                if (error != null || document is not UtpDocument utp)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        RuleId,
                        $"Failed to parse placeable blueprint '{resRef}': {error?.Message}",
                        context.Workspace.GetResourcePath(ResourceType.Utp, resRef),
                        resRef));
                    continue;
                }

                if (string.Equals(utp.OnUsed, QuestEncounterOnUsed, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        RuleId,
                        $"Placeable blueprint '{resRef}' has OnUsed='{QuestEncounterOnUsed}'; quest encounter activators must remain world-instance-only and must not be shipped as a reusable blueprint.",
                        context.Workspace.GetResourcePath(ResourceType.Utp, resRef),
                        resRef));
                }
            }

            var (palette, paletteError) = context.LoadPalette(PlaceablePaletteName);
            if (paletteError != null || palette == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    RuleId,
                    $"Failed to parse placeable palette '{PlaceablePaletteName}': {paletteError?.Message}",
                    context.GetPalettePath(PlaceablePaletteName),
                    null));
                return issues;
            }

            foreach (var leaf in PaletteTraversal.EnumerateLeaves(palette))
            {
                var resRef = leaf.ResRef;
                if (string.IsNullOrEmpty(resRef) || !context.ResourceExists(ResourceType.Utp, resRef))
                    continue; // a missing blueprint is PaletteOrphanRule's concern, not this rule's

                var (document, error) = context.LoadBlueprint(ResourceType.Utp, resRef);
                if (error != null ||
                    document is not UtpDocument utp ||
                    !string.Equals(utp.OnUsed, QuestEncounterOnUsed, StringComparison.OrdinalIgnoreCase))
                    continue;

                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    RuleId,
                    $"Placeable palette entry '{leaf.Name}' ('{resRef}') references a quest-encounter-activator blueprint (OnUsed='{QuestEncounterOnUsed}'); it must be removed from the palette and kept as a world instance only.",
                    context.GetPalettePath(PlaceablePaletteName),
                    resRef));
            }

            return issues;
        }
    }
}
