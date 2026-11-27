namespace SWLOR.TypeMoves;

/// <summary>The text of a qualifier a moved type's name stands behind, and the move.</summary>
/// <param name="Start">Where the qualifier starts (with a <c>global::</c>).</param>
/// <param name="End">Where it ends (before the dot).</param>
/// <param name="Move">The moved type.</param>
internal sealed record QualifierSpan(int Start, int End, TypeMove Move);
