namespace SWLOR.FacadeRemoval;

/// <summary>What one file's rewrite did.</summary>
/// <param name="Text">The new text (the same string when nothing changed).</param>
/// <param name="UsingsAdded">Using directives added (Apply) or removed (Reverse).</param>
/// <param name="Rewrites">Call sites whose text changed: qualified names, constants and explicit <c>GameServices.</c> forms.</param>
/// <param name="NeedsAccessorImport">
/// True when the file reaches a removed facade through the imported <c>GameServices</c> properties, so its project's global
/// usings must import them when the project imports <c>SWLOR.Game.Server.Service</c> globally.
/// </param>
internal sealed record RewriteResult(string Text, int UsingsAdded, int Rewrites, bool NeedsAccessorImport)
{
    public bool Changed(string original) => !ReferenceEquals(Text, original) && !string.Equals(Text, original, StringComparison.Ordinal);
}
