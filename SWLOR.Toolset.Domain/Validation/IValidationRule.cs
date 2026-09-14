namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// One convention check a <see cref="ModuleValidator"/> run can perform over a module
    /// workspace. Implementations must never throw for ordinary bad-content situations (a
    /// malformed file, a missing index) - report a <see cref="ValidationSeverity.Warning"/>
    /// instead, so one broken file cannot abort an entire validation pass.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>A short, stable identifier for this rule (used as <see cref="ValidationIssue.RuleId"/>).</summary>
        string RuleId { get; }

        IEnumerable<ValidationIssue> Validate(ValidationContext context);
    }
}
