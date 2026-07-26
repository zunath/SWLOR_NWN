namespace SWLOR.Toolset.Domain.Script.Syntax
{
    /// <summary>How loudly a diagnostic is drawn.</summary>
    public enum ScriptDiagnosticSeverity
    {
        Warning,
        Error
    }

    /// <summary>Where a diagnostic came from, which the Problems list shows as a tag.</summary>
    /// <remarks>
    /// The plan's two-tier rule made visible. <see cref="Editor"/> findings come from this project's
    /// own analysis and are advisory; <see cref="Compiler"/> findings come from the official compiler
    /// and are authoritative. Where the two disagree the compiler is right by definition, so the tag
    /// is the difference between "our parser has a gap" and "your code is broken".
    /// </remarks>
    public enum ScriptDiagnosticSource
    {
        Editor,
        Compiler
    }

    /// <summary>One finding against a script, positioned in the source.</summary>
    /// <param name="Message">What is wrong, in plain words.</param>
    /// <param name="Start">Absolute offset the squiggle starts at.</param>
    /// <param name="Length">Length of the squiggle.</param>
    /// <param name="Severity">Error or warning.</param>
    /// <param name="Source">Editor or compiler.</param>
    /// <param name="Line">1-based line, for the Problems list.</param>
    /// <param name="ResRef">The file the compiler named, when different from the requested entry point.</param>
    public sealed record ScriptAnalysisDiagnostic(
        string Message,
        int Start,
        int Length,
        ScriptDiagnosticSeverity Severity,
        ScriptDiagnosticSource Source,
        int Line,
        string? ResRef = null)
    {
        public int End => Start + Length;
    }
}
