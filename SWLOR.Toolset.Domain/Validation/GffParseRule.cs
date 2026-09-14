using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Every GFF-backed resource in the module parses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other rules are conventions - they ask whether a resref is too long, whether an instance
    /// points at a blueprint that exists - and each parses only the handful of files it needs. A
    /// malformed ARE, UTI, UTD, UTM, UTT or UTS was therefore reported by nobody, so a validation pass
    /// over a module with a file broken by an external edit or a bad merge could come back clean. This
    /// is the floor beneath the conventions: whatever else is true of a resource, it has to be readable.
    /// </para>
    /// <para>
    /// Every folder the packer converts, not only the ones this toolset has an editor for. A malformed
    /// dialog, area-comment file, faction table, module IFO, palette or journal produced no issue at
    /// all and then failed the pack a minute later inside <c>nwn_gff</c>, which is the least useful
    /// place to find out. Enumerated by folder rather than by <c>ResourceType</c> because half of
    /// these have no editor and therefore no enum entry - the packer's folder list is the authority
    /// on what gets converted.
    /// </para>
    /// <para>
    /// Reported as an Error rather than a Warning: the file cannot be opened, packed or edited, which is
    /// not a matter of style. One unreadable file must not stop the sweep, so each is caught
    /// individually and the rest still run.
    /// </para>
    /// </remarks>
    public sealed class GffParseRule : IValidationRule
    {
        /// <summary>
        /// The module subfolders <c>ModulePacker</c> passes through <c>nwn_gff</c>. <c>nss</c> and
        /// <c>ncs</c> are absent because scripts are not GFF.
        /// </summary>
        private static readonly string[] PackedGffFolders =
        {
            "are", "dlg", "fac", "gic", "git", "ifo", "itp", "jrl",
            "utc", "utd", "uti", "utm", "utp", "uts", "utt", "utw"
        };

        public string RuleId => "GffParse";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var moduleRoot = context.Workspace.ModuleRoot;

            foreach (var folder in PackedGffFolders)
            {
                var directory = Path.Combine(moduleRoot, folder);
                if (!Directory.Exists(directory))
                    continue;

                foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
                {
                    var issue = TryParse(path);
                    if (issue != null)
                        yield return issue;
                }
            }
        }

        private static ValidationIssue? TryParse(string path)
        {
            var fileName = Path.GetFileName(path);

            // "moseis_cantina.are.json" -> "moseis_cantina"
            var resRef = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName));

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
                    $"'{fileName}' could not be read: {ex.Message}",
                    path,
                    resRef);
            }
        }
    }
}
