using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>One place a script is named by a resource's script slot.</summary>
    /// <param name="ResourceType">The kind of resource referencing it.</param>
    /// <param name="ResRef">Which resource.</param>
    /// <param name="FieldName">The slot, e.g. <c>ScriptSpawn</c> or <c>OnUsed</c>.</param>
    public sealed record ScriptUsage(ResourceType ResourceType, string ResRef, string FieldName);

    /// <summary>
    /// Which blueprints and areas name each script in their event slots.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Worth building because the answer is large and invisible: <b>2,250 module resources</b>
    /// reference the legacy <c>dmfi_*</c>, <c>zep_*</c>, <c>nw_*</c> and <c>d1_card*</c> scripts by
    /// name. Editing one of those blind is guesswork, and Aurora could not answer the question either.
    /// </para>
    /// <para>
    /// Slots are found by field-name convention rather than a hardcoded list per resource type: every
    /// GFF script slot is a <c>resref</c> field whose name starts with <c>Script</c> or <c>On</c>.
    /// A hardcoded list would need extending for every new resource type and would silently miss any
    /// slot someone forgot to add.
    /// </para>
    /// </remarks>
    public sealed class ScriptUsageIndex
    {
        private readonly Dictionary<string, List<ScriptUsage>> _byScript;

        private ScriptUsageIndex(Dictionary<string, List<ScriptUsage>> byScript) => _byScript = byScript;

        /// <summary>Resource types that carry script slots.</summary>
        public static readonly IReadOnlyList<ResourceType> ScriptedTypes = new[]
        {
            ResourceType.Utc, ResourceType.Utd, ResourceType.Utp, ResourceType.Utt,
            ResourceType.Uts, ResourceType.Utm, ResourceType.Uti, ResourceType.Utw,
            ResourceType.Area, ResourceType.Dlg
        };

        /// <summary>True when a GFF field name looks like a script slot.</summary>
        public static bool IsScriptSlotField(string fieldName) =>
            fieldName.StartsWith("Script", StringComparison.Ordinal) ||
            fieldName.StartsWith("On", StringComparison.Ordinal);

        /// <summary>Scans the module's blueprints and areas. Expensive; call off the UI thread.</summary>
        public static ScriptUsageIndex Build(ModuleWorkspace workspace, CancellationToken cancellationToken = default)
        {
            var byScript = new Dictionary<string, List<ScriptUsage>>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in ScriptedTypes)
            {
                foreach (var resRef in workspace.EnumerateResRefs(type))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Scan(workspace.GetResourcePath(type, resRef), type, resRef, byScript);

                    // Area script hooks live on the placed instances in the paired GIT, not in the
                    // ARE resource returned by GetResourcePath(ResourceType.Area, ...).
                    if (type == ResourceType.Area)
                    {
                        Scan(
                            Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json"),
                            type,
                            resRef,
                            byScript);
                    }
                }
            }

            return new ScriptUsageIndex(byScript);
        }

        /// <summary>Everything that names <paramref name="scriptResRef"/>.</summary>
        public IReadOnlyList<ScriptUsage> UsagesOf(string scriptResRef) =>
            _byScript.TryGetValue(scriptResRef, out var list) ? list : Array.Empty<ScriptUsage>();

        /// <summary>How many resources name each script; used to rank the picker.</summary>
        public IReadOnlyDictionary<string, int> UsageCounts() =>
            _byScript.ToDictionary(p => p.Key, p => p.Value.Count, StringComparer.OrdinalIgnoreCase);

        private static void Scan(
            string path,
            ResourceType type,
            string resRef,
            Dictionary<string, List<ScriptUsage>> byScript)
        {
            if (!File.Exists(path))
                return;

            JsonGffDocument document;
            try
            {
                document = JsonGffDocument.Load(path);
            }
            catch (Exception)
            {
                // One malformed resource must not take out the whole index.
                return;
            }

            foreach (var (field, value) in EnumerateScriptSlots(document.Root, type))
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (!byScript.TryGetValue(value, out var list))
                    byScript[value] = list = new List<ScriptUsage>();

                list.Add(new ScriptUsage(type, resRef, field));
            }
        }

        private static IEnumerable<(string Field, string Value)> EnumerateScriptSlots(
            JsonGffStruct root,
            ResourceType type,
            string prefix = "")
        {
            foreach (var entry in root.Entries)
            {
                var path = prefix.Length == 0 ? entry.Key : prefix + "." + entry.Key;
                if (entry.Value.Type == GffFieldType.ResRef &&
                    (IsScriptSlotField(entry.Key) ||
                     type == ResourceType.Dlg &&
                     entry.Key is "Active" or "Script" or "EndConversation" or "EndConverAbort"))
                {
                    string value;
                    try
                    {
                        value = entry.Value.GetString();
                    }
                    catch (Exception)
                    {
                        value = string.Empty;
                    }

                    if (value.Length > 0)
                        yield return (path, value);
                }

                if (entry.Value.Struct != null)
                {
                    foreach (var nested in EnumerateScriptSlots(entry.Value.Struct, type, path))
                        yield return nested;
                }

                if (entry.Value.Elements == null)
                    continue;

                for (var i = 0; i < entry.Value.Elements.Count; i++)
                {
                    foreach (var nested in EnumerateScriptSlots(
                                 entry.Value.Elements[i], type, $"{path}[{i}]"))
                        yield return nested;
                }
            }
        }
    }
}
