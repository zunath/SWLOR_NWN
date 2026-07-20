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
        private const int MaxResRefLength = 16;

        public string RuleId => "ResRefLength";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var issues = new List<ValidationIssue>();

            foreach (var type in AllResourceTypes())
            {
                foreach (var resRef in context.ResRefsFor(type))
                {
                    var path = GetPathSafely(context, type, resRef);

                    if (resRef.Length > MaxResRefLength)
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            RuleId,
                            $"ResRef '{resRef}' ({type}) is {resRef.Length} characters, exceeding NWN's {MaxResRefLength}-character limit.",
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
        }
    }
}
