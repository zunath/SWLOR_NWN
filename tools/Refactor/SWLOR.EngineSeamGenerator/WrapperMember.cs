namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// One wrapper member read from a hand-written source file: its signature as text, its documentation, and where its
/// body sits in the file.
/// </summary>
internal sealed class WrapperMember
{
    public required MemberKind Kind { get; init; }

    public required string Name { get; init; }

    /// <summary>The return type (methods) or the property type, as written.</summary>
    public required string ReturnType { get; init; }

    public required IReadOnlyList<WrapperParameter> Parameters { get; init; }

    /// <summary>Identifies the member across files: the name plus the parameter types without whitespace.</summary>
    public required string Key { get; init; }

    /// <summary>The `///` documentation lines, trimmed, in order.</summary>
    public required IReadOnlyList<string> DocLines { get; init; }

    /// <summary>The whitespace in front of the member declaration.</summary>
    public required string Indent { get; init; }

    /// <summary>Where the body starts in the file: the `{` of a block body, or the first token of a property expression.</summary>
    public required int BodyStart { get; init; }

    /// <summary>The length of the body text in the file.</summary>
    public required int BodyLength { get; init; }

    /// <summary>The body exactly as written, with line endings normalized to '\n'.</summary>
    public required string BodyText { get; init; }

    public bool ReturnsValue => Kind == MemberKind.Property || ReturnType != "void";

    public string ParameterList => string.Join(", ", Parameters.Select(p => p.Text));

    public string ParameterListWithoutDefaults => string.Join(", ", Parameters.Select(p => p.TextWithoutDefault));

    public string ArgumentList => string.Join(", ", Parameters.Select(p => p.Name));

    /// <summary>The declaration without modifiers or body, e.g. `uint GetMaster(uint oCreature = OBJECT_INVALID)`.</summary>
    public string Signature => Kind == MemberKind.Property ? $"{ReturnType} {Name}" : $"{ReturnType} {Name}({ParameterList})";

    /// <summary>The same declaration without default values; the fake omits them so they need not resolve in its namespace.</summary>
    public string SignatureWithoutDefaults => Kind == MemberKind.Property ? Signature : $"{ReturnType} {Name}({ParameterListWithoutDefaults})";
}
