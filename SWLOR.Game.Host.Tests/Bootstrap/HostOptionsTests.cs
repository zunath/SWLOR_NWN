using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using Nwn.Hosting.Bootstrap;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Contracts;
using SWLOR.Game.Host.Bootstrap;
using SWLOR.Game.Host.Hooks;
using SWLOR.Game.NativeHooks;

namespace SWLOR.Game.Host.Tests.Bootstrap;

[TestFixture]
public class HostOptionsTests
{
    [Test]
    public void The_host_runs_frames_inline_drops_refused_closures_and_keeps_closures_unbounded()
    {
        var options = HostOptions.Create(_ => throw new InvalidOperationException("not created here"), new NativeHookTable());

        options.FrameDelivery.Should().Be(FrameDelivery.Inline,
            "the native entry runs its frame work at the start of the main loop with OBJECT_SELF invalid, from the first frame");
        options.RejectedClosures.Should().Be(RejectedClosureHandling.Drop,
            "the native entry drops a closure NWNX refuses without an exception");
        options.MaximumPendingClosures.Should().BeNull("the native entry's closure dictionary is unbounded");
        options.LogPrefix.Should().Be(HostOptions.LogPrefix);
        options.ModeName.Should().Be(HostOptions.ModeName);
    }

    [Test]
    public void The_host_has_one_native_hook_extension_per_hook_file_in_the_order_of_the_ids()
    {
        var hooks = new NativeHookTable();

        var options = HostOptions.Create(_ => throw new InvalidOperationException("not created here"), hooks);

        options.Extensions.Should().AllBeOfType<NativeHookExtension>();
        options.Extensions.Cast<NativeHookExtension>().Select(extension => extension.Hook)
            .Should().Equal(Enum.GetValues<NativeHookId>(), "the 17 hook files of the game, each resolved when the host starts");
    }

    [Test]
    public void Creating_the_options_needs_a_mode_factory_and_the_hooks()
    {
        var withoutMode = () => HostOptions.Create(null!, new NativeHookTable());
        var withoutHooks = () => HostOptions.Create(_ => throw new InvalidOperationException("not created here"), null!);

        withoutMode.Should().Throw<ArgumentNullException>();
        withoutHooks.Should().Throw<ArgumentNullException>();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void The_game_loads_collectible_as_the_settings_say_and_shares_the_contracts_and_redis(bool collectible)
    {
        var settings = HostSettings.Read(
            name => name == HostSettings.CollectibleVariable ? collectible.ToString() : null,
            AppContext.BaseDirectory);

        var options = HostOptions.LoadOptions(settings);

        options.Collectible.Should().Be(collectible);
        options.ContextNamePrefix.Should().Be(HostOptions.ContextNamePrefix);
        options.SharedAssemblyNames.Should().Equal(HostOptions.SharedAssemblyNames);
    }

    [Test]
    public void The_shared_assemblies_are_the_contracts_the_native_hooks_and_the_redis_client_with_its_whole_dependency_closure()
    {
        var expected = new SortedSet<string>(StringComparer.Ordinal) { "SWLOR.Game.Contracts", "SWLOR.Game.NativeHooks" };
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<Assembly>([typeof(ConnectionMultiplexer).Assembly]);
        while (pending.TryPop(out var assembly))
        {
            var name = assembly.GetName().Name!;
            if (!visited.Add(name))
                continue;

            expected.Add(name);
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var loaded = Assembly.Load(reference);
                if (!IsFramework(loaded))
                    pending.Push(loaded);
            }
        }

        HostOptions.SharedAssemblyNames.Should().BeEquivalentTo(expected,
            "a type the game and the host's Redis connection both use must come from one copy, and the game must reach the " +
            "trampolines NWNX calls, not a copy of its own");
    }

    [Test]
    public void Load_options_need_settings()
    {
        var load = () => HostOptions.LoadOptions(null!);

        load.Should().Throw<ArgumentNullException>();
    }

    private static bool IsFramework(Assembly assembly) =>
        string.Equals(
            Path.GetDirectoryName(assembly.Location),
            RuntimeEnvironment.GetRuntimeDirectory().TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
