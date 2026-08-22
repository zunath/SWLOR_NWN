using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Every placed-object instance in every area's .git file must reference a blueprint that
    /// actually exists on disk. Covers creatures, doors, placeables, sounds, stores, triggers, and
    /// waypoints (the loose "List" item instances are skipped - items have no blueprint-existence
    /// convention worth enforcing here, and encounters are skipped because this package has no
    /// modeled .ute blueprint type to check against).
    /// </summary>
    /// <remarks>
    /// Quest-encounter activator placeables are world-instance-only by design (see
    /// <see cref="QuestActivatorNotInPaletteRule"/>): a placeable instance whose "OnUsed" field is
    /// "quest_enc" is exempt from this check even though no matching .utp file should exist.
    /// </remarks>
    public sealed class DanglingInstanceTemplateRule : IValidationRule
    {
        private const string QuestEncounterOnUsed = "quest_enc";

        public string RuleId => "DanglingInstanceTemplate";

        private sealed record ListMapping(
            string ListName,
            ResourceType Type,
            string TemplateFieldName,
            Func<GitDocument, IReadOnlyList<JsonGffStruct>> Selector);

        private static readonly ListMapping[] Mappings =
        {
            new("Creature List", ResourceType.Utc, "TemplateResRef", git => git.Creatures),
            new("Door List", ResourceType.Utd, "TemplateResRef", git => git.Doors),
            new("Placeable List", ResourceType.Utp, "TemplateResRef", git => git.Placeables),
            new("SoundList", ResourceType.Uts, "TemplateResRef", git => git.Sounds),
            // .utm blueprints use "ResRef" for their own template ResRef, not "TemplateResRef" -
            // git store instances follow the same naming.
            new("StoreList", ResourceType.Utm, "ResRef", git => git.Stores),
            new("TriggerList", ResourceType.Utt, "TemplateResRef", git => git.Triggers),
            new("WaypointList", ResourceType.Utw, "TemplateResRef", git => git.Waypoints)
        };

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var issues = new List<ValidationIssue>();

            foreach (var areaResRef in context.AreaResRefs)
            {
                var (git, error) = context.LoadGit(areaResRef);
                if (error != null || git == null)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        RuleId,
                        $"Failed to parse area '{areaResRef}' git file: {error?.Message}",
                        context.GetGitPath(areaResRef),
                        areaResRef));
                    continue;
                }

                foreach (var mapping in Mappings)
                {
                    foreach (var instance in mapping.Selector(git))
                    {
                        if (mapping.Type == ResourceType.Utp &&
                            string.Equals(
                                instance.GetStringOrNull("OnUsed"),
                                QuestEncounterOnUsed,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var templateResRef = instance.GetStringOrNull(mapping.TemplateFieldName);
                        if (string.IsNullOrEmpty(templateResRef))
                            continue;

                        if (context.ResourceExists(mapping.Type, templateResRef))
                            continue;

                        // Base-game and hak-provided templates legitimately have no module
                        // file; only flag templates that resolve nowhere.
                        if (context.ResolvableOutsideModule(mapping.Type, templateResRef))
                            continue;

                        var tag = instance.GetStringOrNull("Tag");

                        // Warning, not Error: git instances are self-contained in NWN — the
                        // template resref is provenance, and legacy areas accumulate references
                        // to long-deleted blueprints that cause no runtime failure. The signal
                        // matters mainly for freshly placed content (typo detection).
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Warning,
                            RuleId,
                            $"Area '{areaResRef}' {mapping.ListName} instance (Tag='{tag}') references missing {mapping.Type} blueprint '{templateResRef}'.",
                            context.GetGitPath(areaResRef),
                            templateResRef));
                    }
                }
            }

            return issues;
        }
    }
}
