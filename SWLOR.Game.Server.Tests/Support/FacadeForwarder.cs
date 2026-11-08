#nullable enable
using System.Reflection;
using System.Runtime.ExceptionServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The constructor argument a test passes for a service an instance used to reach through its static facade, before the
/// instance took the service by constructor (P2-ZB). Every call resolves the service from the container installed at that
/// moment, through the accessor the facade used, so a test double whose seams fall back to their default bodies reaches
/// the same instance as before: the one a <see cref="FacadeContainer"/> installed when the call runs, or
/// <see cref="ContainerNotBuiltException"/> when none is. A test that scripts the dependency passes its own stand-in instead.
/// </summary>
public class FacadeForwarder : DispatchProxy
{
    private static readonly MethodInfo Resolve = typeof(CompositionRoot).Assembly
        .GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true)!
        .GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic)!;

    private MethodInfo _resolve = null!;

    /// <summary>A stand-in for <typeparamref name="TService"/> that forwards each call to the installed container's instance.</summary>
    public static TService For<TService>()
        where TService : class
    {
        var proxy = Create<TService, FacadeForwarder>();
        ((FacadeForwarder)(object)proxy)._resolve = Resolve.MakeGenericMethod(typeof(TService));
        return proxy;
    }

    /// <summary>The same stand-in behind a <see cref="Lazy{T}"/>, for a constructor that takes the service lazily.</summary>
    public static Lazy<TService> LazyFor<TService>()
        where TService : class => new(For<TService>);

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        try
        {
            return targetMethod!.Invoke(_resolve.Invoke(null, null), args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
