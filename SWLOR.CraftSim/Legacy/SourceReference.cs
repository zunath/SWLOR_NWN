namespace SWLOR.CraftSim.Legacy
{
    /// <summary>Where a legacy rule lives in the server and the source text that proves it; LegacySourceTests fails when an anchor moves away.</summary>
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

        public string Name { get; }

        /// <summary>The path from the repository root, with forward slashes.</summary>
        public string File { get; }

        public int FirstLine { get; }
        public int LastLine { get; }

        /// <summary>Source text, without indentation, that must appear in the cited lines.</summary>
        public IReadOnlyList<string> Anchors { get; }

        /// <summary>The citation as written in code comments and reports, such as <c>CraftViewModel.cs:446-454</c>.</summary>
        public string Citation =>
            $"{System.IO.Path.GetFileName(File)}:{FirstLine}" + (LastLine > FirstLine ? $"-{LastLine}" : string.Empty);

        public override string ToString() => $"{Name} ({Citation})";
    }
}
