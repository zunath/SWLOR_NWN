using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A service for a constructor parameter that resolves the container installed at the time of each call, as the static
/// facade it replaces did (<c>Stat.GetStat(...)</c> read <see cref="CompositionRoot.Current"/> on every call). A test that
/// builds a converted class with <c>new</c> and installs a <see cref="FacadeContainer"/> gets the container's instance
/// behind the interface; a test that installs none gets the same <see cref="ContainerNotBuiltException"/> the facade
/// threw, and only if the code under test reaches the service.
/// </summary>
public static class AmbientServices
{
    /// <summary>The service of the installed container, resolved on every call.</summary>
    public static TService Of<TService>()
        where TService : class => DispatchProxy.Create<TService, AmbientProxy<TService>>();

    /// <summary>For a <c>Lazy&lt;IXService&gt;</c> parameter.</summary>
    public static Lazy<TService> LazyOf<TService>()
        where TService : class
    {
        var service = Of<TService>();
        return new Lazy<TService>(() => service);
    }

    private static readonly PropertyInfo CurrentProperty =
        typeof(CompositionRoot).GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Static)!;

    public class AmbientProxy<TService> : DispatchProxy
        where TService : class
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var target = ((IServiceProvider)CurrentProperty.GetValue(null)!).GetRequiredService<TService>();
            try
            {
                return targetMethod!.Invoke(target, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
