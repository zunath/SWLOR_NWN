using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Every GFF-backed resource in the module parses.
    /// </summary>
    /// <remarks>
    /// The other rules are conventions - they ask whether a resref is too long, whether an instance
    /// points at a blueprint that exists - and each parses only the handful of files it needs. A
    /// malformed ARE, UTI, UTD, UTM, UTT or UTS was therefore reported by nobody, so a validation pass
    /// over a module with a file broken by an external edit or a bad merge could come back clean. This
    /// is the floor beneath the conventions: whatever else is true of a resource, it has to be readable.
    /// <para>
    /// Reported as an Error rather than a Warning: the file cannot be opened, packed or edited, which is
    /// not a matter of style. One unreadable file must not stop the sweep, so each is caught
    /// individually and the rest still run.
    /// </para>
    /// </remarks>
    public sealed class GffParseRule : IValidationRule
    {
        public string RuleId => "GffParse";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            foreach (var type in GffResourceTypes())
            {
                foreach (var resRef in context.ResRefsFor(type))
                {
                    var issue = TryParse(context, type, resRef);
                    if (issue != null)
                        yield return issue;
                }
            }
        }

        /// <summary>
        /// The area and every blueprint type - the resources stored as GFF that this toolset reads.
        /// </summary>
        private static IEnumerable<ResourceType> GffResourceTypes()
        {
            yield return ResourceType.Area;

            foreach (var type in ModuleWorkspace.BlueprintTypes)
                yield return type;
        }

        private static ValidationIssue? TryParse(ValidationContext context, ResourceType type, string resRef)
        {
            var path = context.Workspace.GetResourcePath(type, resRef);
            if (!File.Exists(path))
                return null; // Missing files are somebody else's rule; this one is about readability.

            try
            {
                JsonGffDocument.Load(path);
                return null;
            }
            catch (Exception ex)
            {
                return new ValidationIssue(
                    ValidationSeverity.Error,
                    "GffParse",
                    $"{type.DisplayName()} '{resRef}' could not be read: {ex.Message}",
                    path,
                    resRef);
            }
        }
    }
}
