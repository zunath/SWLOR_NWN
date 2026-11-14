namespace SWLOR.EngineSeamGenerator;

/// <summary>A member read back from generated output: signature text and, for implementations, the body text.</summary>
/// <param name="Kind">Method or property.</param>
/// <param name="Key">Matches <see cref="WrapperMember.Key"/>.</param>
/// <param name="Signature">Return type, name and parameter list as written, without modifiers.</param>
/// <param name="BodyText">The block (methods) or expression (properties) as written, '\n' line endings; null for interface members.</param>
internal sealed record GeneratedMember(MemberKind Kind, string Key, string Signature, string? BodyText);
