namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Asks a service provider for a service directly.</summary>
public sealed class ProviderUser
{
    public object? Get(IServiceProvider provider) => provider.GetService(typeof(string));
}
