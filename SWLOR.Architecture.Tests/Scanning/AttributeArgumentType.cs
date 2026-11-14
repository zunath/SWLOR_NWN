namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// A type met while decoding a custom attribute blob: the types of the scanned assembly it stands for, and, for a type
/// of another assembly, the name needed to look up an enum's underlying type.
/// </summary>
/// <param name="Defined">The scanned assembly's types it names (a <c>typeof</c> argument, a generic argument, an enum).</param>
/// <param name="ExternalName">The assembly-qualified name of a type of another assembly, or null.</param>
/// <param name="IsSystemType">True for <c>System.Type</c> itself, whose arguments are stored as type names.</param>
internal sealed record AttributeArgumentType(
    IReadOnlyList<System.Reflection.Metadata.TypeDefinitionHandle> Defined,
    string? ExternalName,
    bool IsSystemType);
