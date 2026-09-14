namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>How serious a <see cref="ValidationIssue"/> is: <see cref="Error"/> is a broken
    /// convention that should be fixed before shipping content; <see cref="Warning"/> is either a
    /// softer convention concern or a diagnostic (e.g. a file failed to parse) that a rule could not
    /// fully evaluate.</summary>
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    /// <summary>
    /// One finding produced by an <see cref="IValidationRule"/>: what rule found it, how severe it
    /// is, a human-readable explanation, and (when known) the on-disk file and resref/context the
    /// finding is about.
    /// </summary>
    public sealed record ValidationIssue(
        ValidationSeverity Severity,
        string RuleId,
        string Message,
        string? FilePath,
        string? ResRef);
}
