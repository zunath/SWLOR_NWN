namespace SWLOR.Toolset.AreaGeneration;

/// <summary>Runs CPU- and I/O-heavy area-generator phases away from the UI thread.</summary>
public interface IAreaGeneratorBackgroundTaskRunner
{
    Task<T> RunAsync<T>(Func<T> operation);
}

/// <summary>Default thread-pool implementation used by the Area Generator window.</summary>
public sealed class AreaGeneratorBackgroundTaskRunner : IAreaGeneratorBackgroundTaskRunner
{
    public Task<T> RunAsync<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Task.Run(operation);
    }
}
