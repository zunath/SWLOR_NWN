using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Immutable area-file input read and parsed away from Avalonia's UI thread. DocumentSession is
    /// deliberately created later, on the editor's context, so only I/O and JSON work move to the
    /// worker and the undo guard retains its normal lifetime.
    /// </summary>
    public sealed class AreaEditorDocumentLoad
    {
        private AreaEditorDocumentLoad(
            byte[] areBytes,
            JsonGffDocument are,
            byte[] gitBytes,
            JsonGffDocument git,
            byte[] gicBytes,
            JsonGffDocument gic)
        {
            AreBytes = areBytes;
            Are = are;
            GitBytes = gitBytes;
            Git = git;
            GicBytes = gicBytes;
            Gic = gic;
        }

        public byte[] AreBytes { get; }
        public JsonGffDocument Are { get; }
        public byte[] GitBytes { get; }
        public JsonGffDocument Git { get; }
        public byte[] GicBytes { get; }
        public JsonGffDocument Gic { get; }

        public static AreaEditorDocumentLoad Load(string arePath, string gitPath, string gicPath)
        {
            var areBytes = File.ReadAllBytes(arePath);
            var gitBytes = File.ReadAllBytes(gitPath);
            var gicBytes = File.ReadAllBytes(gicPath);

            // Task.Run inherits the UI execution context, including any open editor's ambient
            // mutation guard. These are brand-new parse graphs, not edits to an open document.
            using var construction = EditScope.EnterConstruction();
            return new AreaEditorDocumentLoad(
                areBytes,
                JsonGffDocument.Parse(areBytes),
                gitBytes,
                JsonGffDocument.Parse(gitBytes),
                gicBytes,
                JsonGffDocument.Parse(gicBytes));
        }
    }
}
