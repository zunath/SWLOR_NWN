namespace SWLOR.ContentInjection;

/// <summary>What the forward conversion did to one file.</summary>
/// <param name="Text">The new text; the original when nothing changed or anything was refused (a file converts whole or not at all).</param>
/// <param name="Refusals">What a person must do in this file.</param>
/// <param name="Edits">How many places changed.</param>
internal sealed record FileRewrite(string Text, IReadOnlyList<Refusal> Refusals, int Edits);
