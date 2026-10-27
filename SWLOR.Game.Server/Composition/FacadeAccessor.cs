using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The one way a static facade reaches the instance behind it. A facade keeps the static API content code has always
    /// called and forwards each member to the service resolved here. Internal on purpose, and the architecture tests
    /// allow only the <c>Composition</c> namespace and the facades of converted services to use it: everything else
    /// receives its dependencies through a constructor.
    /// </summary>
    internal static class FacadeAccessor
    {
        /// <summary>
        /// The container's instance of <typeparamref name="TService"/>. Throws <see cref="ContainerNotBuiltException"/>
        /// before the container is built, and <see cref="InvalidOperationException"/> when the service is not registered.
        /// </summary>
        internal static TService Resolve<TService>()
            where TService : class
        {
            return CompositionRoot.Current.GetRequiredService<TService>();
        }
    }
}
