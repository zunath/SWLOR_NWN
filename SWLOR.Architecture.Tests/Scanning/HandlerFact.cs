namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A method that carries the <c>[NWNEventHandler]</c> attribute.</summary>
/// <param name="Owner">The top-level type that declares the method.</param>
/// <param name="Method">The method name.</param>
internal sealed record HandlerFact(TypeKey Owner, string Method);
