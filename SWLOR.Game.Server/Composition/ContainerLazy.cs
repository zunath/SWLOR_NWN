using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// What a constructor gets for a <c>Lazy&lt;IXService&gt;</c> parameter: the container's instance of
    /// <typeparamref name="T"/>, resolved on the first read of <see cref="Lazy{T}.Value"/> (decision D3 of
    /// <c>Readmes/MaintainabilityRefactorFacadeRemoval.md</c>). It breaks a constructor cycle between two services that call
    /// each other, and nothing else: a test (<c>LazyDependencyCycleTests</c>) fails for a <c>Lazy&lt;&gt;</c> parameter that
    /// would make no cycle if it were direct. Registered once for every <c>T</c>
    /// (<see cref="ServiceRegistrations"/>); only <c>Composition</c> touches the provider, so no service locates another.
    /// </summary>
    internal sealed class ContainerLazy<T> : Lazy<T>
        where T : class
    {
        public ContainerLazy(IServiceProvider provider)
            : base(provider.GetRequiredService<T>, LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }
    }
}
