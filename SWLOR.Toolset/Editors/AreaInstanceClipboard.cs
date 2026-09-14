using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// The toolset-session clipboard for one placed area object.
    /// </summary>
    /// <remarks>
    /// Kept separate from the operating-system text clipboard: the payload is a deep GFF instance,
    /// its paired GIC comment, and a render preview. One clipboard is shared by every area editor
    /// opened through <see cref="EditorService"/>, so an object copied in one area may be pasted in
    /// another area of the same module without exposing a lossy text representation to other
    /// applications.
    /// </remarks>
    public sealed class AreaInstanceClipboard
    {
        internal AreaInstanceClipboardEntry? Content { get; private set; }

        internal void Set(AreaInstanceClipboardEntry content)
        {
            ArgumentNullException.ThrowIfNull(content);
            Content = content;
        }
    }

    /// <summary>An independent clipboard snapshot. Its GFF values are private deep clones.</summary>
    internal sealed record AreaInstanceClipboardEntry(
        string ModuleRoot,
        ResourceType Type,
        JsonGffStruct Instance,
        JsonGffStruct? Comment,
        InstanceMarker Preview);
}
