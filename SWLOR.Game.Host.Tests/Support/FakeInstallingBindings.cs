using System.Collections.Concurrent;
using Nwn.Hosting.Contracts.Generations;
using Nwn.Hosting.Testing.Fixtures;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>
/// The package's fixture engine thread (<see cref="FixtureNativeBindings"/>) with the engine fakes kept in step with the
/// generations: before every engine-thread operation, on that thread, <see cref="GenerationEngineFakes.Sync"/> overrides
/// the seams of a new generation and takes them off a retired one. Reports are kept for the test.
/// </summary>
internal sealed class FakeInstallingBindings(FixtureNativeBindings engine, GenerationEngineFakes fakes) : IPermanentNativeBindings
{
    private readonly ConcurrentQueue<(string Category, string Message)> _reports = new();

    /// <summary>The engine thread's managed id.</summary>
    public int EngineThreadId => engine.EngineThreadId;

    /// <summary>Everything the host reported, in order.</summary>
    public IReadOnlyList<(string Category, string Message)> Reports => _reports.ToArray();

    public ValueTask InvokeOnEngineThreadAsync(Func<ValueTask> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        return engine.InvokeOnEngineThreadAsync(() =>
        {
            fakes.Sync();
            return action();
        }, cancellationToken);
    }

    /// <summary>Runs <paramref name="action"/> on the engine thread, with the fakes in step, and waits for it.</summary>
    public async Task RunOnEngineThreadAsync(Action action)
    {
        await InvokeOnEngineThreadAsync(() =>
        {
            action();
            return ValueTask.CompletedTask;
        }, CancellationToken.None);
    }

    public void Report(string category, string message) => _reports.Enqueue((category, message));
}
