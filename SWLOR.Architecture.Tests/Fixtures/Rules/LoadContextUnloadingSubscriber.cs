using System.Runtime.Loader;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Subscribes to a load context's unloading event.</summary>
public sealed class LoadContextUnloadingSubscriber
{
    public void Hook() => AssemblyLoadContext.Default.Unloading += _ => { };
}
