using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Every enumerated resref (areas and every blueprint type) must be at most 16 characters
    /// (NWN's resref limit) and lowercase. Purely file-name based - no file is parsed, so this
    /// rule is cheap to run over the entire corpus.
    /// </summary>
    public sealed class ResRefLengthRule : IValidationRule
    {
        public string RuleId => "ResRefLength";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var issues = new List<ValidationIssue>();

            foreach (var type in AllResourceTypes())
            {
                foreach (var resRef in context.ResRefsFor(type))
                {
                    var path = GetPathSafely(context, type, resRef);

                    if (resRef.Length > NwnResRef.MaxLength)
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            RuleId,
                            $"ResRef '{resRef}' ({type}) is {resRef.Length} characters, exceeding NWN's {NwnResRef.MaxLength}-character limit.",
                            path,
                            resRef));
                    }

                    if (resRef != resRef.ToLowerInvariant())
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            RuleId,
                            $"ResRef '{resRef}' ({type}) must be lowercase.",
                            path,
                            resRef));
                    }
                    else if (!NwnResRef.IsCanonical(resRef))
                    {
                        // The length and case checks pass for a short lowercase name like
                        // "bad-name", and nothing else in the default rule set looks at the
                        // character set - so validation reported no resref problem for a resource
                        // the engine cannot address reliably. Same constraint NewAreaWriter applies
                        // to a name the builder types; imports and external renames get it too.
                        // Only raised when the case check did not already fire, so one bad name is
                        // one issue rather than two.
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            RuleId,
                            $"ResRef '{resRef}' ({type}) must use only lowercase letters, digits and underscores.",
                            path,
                            resRef));
                    }
                }
            }

            return issues;
        }

        private static string? GetPathSafely(ValidationContext context, ResourceType type, string resRef)
        {
            try
            {
                return context.Workspace.GetResourcePath(type, resRef);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IEnumerable<ResourceType> AllResourceTypes()
        {
            yield return ResourceType.Area;

            foreach (var type in ModuleWorkspace.BlueprintTypes)
                yield return type;

            // Dialogs and scripts are resources with resrefs and the same 16-character, lowercase limit,
            // but they are not blueprints, so they fell outside this list and an overlength or uppercase
            // name imported or renamed outside the toolset was reported as no issue at all.
            yield return ResourceType.Dlg;
            yield return ResourceType.Nss;
        }
    }
}
