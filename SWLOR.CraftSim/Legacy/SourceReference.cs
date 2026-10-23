namespace SWLOR.CraftSim.Legacy
{
    /// <summary>
    /// Where a legacy rule lives in the server and the source text that proves it; LegacySourceTests fails when an anchor moves away.
    /// A frozen reference cites a file the server no longer has: it carries the commit that last held the file and no anchors.
    /// </summary>
    public sealed class SourceReference
    {
        public SourceReference(string name, string file, int firstLine, int lastLine, params string[] anchors)
        {
            Name = name;
            File = file;
            FirstLine = firstLine;
            LastLine = lastLine;
            Anchors = anchors;
        }

        private SourceReference(string name, string file, int firstLine, int lastLine, string frozenAt)
            : this(name, file, firstLine, lastLine, Array.Empty<string>())
        {
            FrozenAt = frozenAt;
        }

        /// <summary>A rule of a file the server deleted, cited to the last commit that held the file.</summary>
        public static SourceReference Frozen(string name, string file, int firstLine, int lastLine, string commit) =>
            new(name, file, firstLine, lastLine, commit);

        public string Name { get; }

        /// <summary>The path from the repository root, with forward slashes.</summary>
        public string File { get; }

        public int FirstLine { get; }
        public int LastLine { get; }

        /// <summary>Source text, without indentation, that must appear in the cited lines. Empty for a frozen reference.</summary>
        public IReadOnlyList<string> Anchors { get; }

        /// <summary>The full SHA of the last commit that held the file, or null while the server still has the file.</summary>
        public string FrozenAt { get; }

        public bool IsFrozen => FrozenAt != null;

        /// <summary>The citation as written in code comments and reports, such as <c>CraftViewModel.cs:446-454</c>.</summary>
        public string Citation =>
            $"{System.IO.Path.GetFileName(File)}:{FirstLine}" + (LastLine > FirstLine ? $"-{LastLine}" : string.Empty);

        public override string ToString() => $"{Name} ({Citation})";
    }
}
