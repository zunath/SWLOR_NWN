using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Every RESREF entry in every "*palcus.itp.json" palette must resolve to an existing blueprint
    /// file of the matching type. "encounterpalcus.itp.json" is intentionally not checked - this
    /// package has no modeled .ute (encounter) blueprint document type to validate against.
    /// </summary>
    public sealed class PaletteOrphanRule : IValidationRule
    {
        public string RuleId => "PaletteOrphan";

        private static readonly (string PaletteName, ResourceType Type)[] Mappings =
        {
            ("creaturepalcus", ResourceType.Utc),
            ("doorpalcus", ResourceType.Utd),
            ("itempalcus", ResourceType.Uti),
            ("placeablepalcus", ResourceType.Utp),
            ("soundpalcus", ResourceType.Uts),
            ("storepalcus", ResourceType.Utm),
            ("triggerpalcus", ResourceType.Utt),
            ("waypointpalcus", ResourceType.Utw)
        };

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var issues = new List<ValidationIssue>();

            foreach (var (paletteName, type) in Mappings)
            {
                var (palette, error) = context.LoadPalette(paletteName);
                if (error != null || palette == null)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        RuleId,
                        $"Failed to parse palette '{paletteName}': {error?.Message}",
                        context.GetPalettePath(paletteName),
                        null));
                    continue;
                }

                foreach (var leaf in PaletteTraversal.EnumerateLeaves(palette))
                {
                    var resRef = leaf.ResRef;
                    if (string.IsNullOrEmpty(resRef) ||
                        context.ResourceExists(type, resRef) ||
                        context.ResolvableOutsideModule(type, resRef))
                        continue;

                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        RuleId,
                        $"Palette '{paletteName}' entry '{leaf.Name}' references missing {type} blueprint '{resRef}'.",
                        context.GetPalettePath(paletteName),
                        resRef));
                }
            }

            return issues;
        }
    }
}
