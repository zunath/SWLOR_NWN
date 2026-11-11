namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// The native hook entry points of a compiled assembly (the methods that carry <c>[UnmanagedCallersOnly]</c>), the types
/// that implement a sink interface and the sink interfaces themselves. Read from metadata and IL; the assembly is never
/// loaded.
/// </summary>
internal sealed record NativeHookFacts(
    IReadOnlyList<NativeHook> Hooks,
    IReadOnlyList<SinkImplementer> Implementers,
    IReadOnlyList<TypeKey> SinkInterfaces);
