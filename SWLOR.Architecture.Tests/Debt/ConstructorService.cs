namespace SWLOR.Architecture.Tests.Debt;

/// <summary>A constructor parameter of a server type whose type is a service, directly or deferred.</summary>
/// <param name="Type">The top-level type whose constructor (or a nested type's constructor) takes it.</param>
/// <param name="File">The file of the constructor.</param>
/// <param name="Service">The service's full type name, unwrapped from <c>Lazy&lt;&gt;</c> or <c>Func&lt;&gt;</c>.</param>
/// <param name="Wrapper">Empty for a direct parameter; <c>Lazy</c> or <c>Func</c> for deferred resolution.</param>
internal sealed record ConstructorService(string Type, string File, string Service, string Wrapper)
{
    public bool IsDeferred => Wrapper.Length > 0;
}
