using Nwn.Hosting.Contracts.Generations;

namespace SWLOR.Game.Host.Services;

/// <summary>
/// The process-lifetime objects the host hands every generation (<see cref="GenerationActivation.HostServices"/>), keyed by
/// the shared type the game asks for.
/// </summary>
public sealed class HostServices : IHostServices
{
    private readonly Dictionary<Type, object> _services = [];

    /// <summary>Registers <paramref name="service"/> under <typeparamref name="TService"/>.</summary>
    public HostServices Add<TService>(TService service)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(service);
        _services.Add(typeof(TService), service);
        return this;
    }

    /// <inheritdoc/>
    public object? Resolve(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return _services.GetValueOrDefault(serviceType);
    }
}
