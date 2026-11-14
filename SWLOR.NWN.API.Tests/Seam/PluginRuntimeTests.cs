using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.Tests.Support;

namespace SWLOR.NWN.API.Tests.Seam;

/// <summary>
/// Every plugin runtime defaults to its pass-through implementation, and its override scope swaps and restores the engine per
/// async flow. The cases are the ones <see cref="NWScriptRuntimeTests"/> runs for NWScript, run once per plugin.
/// </summary>
[TestFixture]
public class PluginRuntimeTests
{
    public static IEnumerable<PluginSeam> Seams => PluginSeam.All;

    [TestCaseSource(nameof(Seams))]
    public void DefaultEngine_IsThePassThroughImplementation(PluginSeam seam)
    {
        seam.Current.Should().BeOfType(seam.PassThrough);
        seam.Current.Should().BeSameAs(seam.Current, "the default is created once");
    }

    [TestCaseSource(nameof(Seams))]
    public void Override_InstallsTheEngineAndRestoresTheDefaultOnDispose(PluginSeam seam)
    {
        var before = seam.Current;
        var fake = seam.NewFake();

        using (seam.Override(fake))
        {
            seam.Current.Should().BeSameAs(fake);
        }

        seam.Current.Should().BeSameAs(before);
        seam.Current.Should().BeOfType(seam.PassThrough);
    }

    [TestCaseSource(nameof(Seams))]
    public void NestedOverrides_RestoreTheirPreviousEngine(PluginSeam seam)
    {
        var outer = seam.NewFake();
        var inner = seam.NewFake();

        using (seam.Override(outer))
        {
            using (seam.Override(inner))
            {
                seam.Current.Should().BeSameAs(inner);
            }

            seam.Current.Should().BeSameAs(outer);
        }

        seam.Current.Should().BeOfType(seam.PassThrough);
    }

    [TestCaseSource(nameof(Seams))]
    public void DisposingAScopeTwice_DoesNotDisturbAnotherActiveScope(PluginSeam seam)
    {
        var first = seam.NewFake();
        var second = seam.NewFake();

        var firstScope = seam.Override(first);
        firstScope.Dispose();
        using (seam.Override(second))
        {
            firstScope.Dispose();

            seam.Current.Should().BeSameAs(second);
        }

        seam.Current.Should().BeOfType(seam.PassThrough);
    }

    [TestCaseSource(nameof(Seams))]
    public void Override_RejectsNull(PluginSeam seam)
    {
        var action = () => seam.Override(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [TestCaseSource(nameof(Seams))]
    public async Task Overrides_AreIsolatedBetweenConcurrentFlows(PluginSeam seam)
    {
        using var bothInstalled = new Barrier(2);
        using var bothChecked = new Barrier(2);

        async Task<object> Run(object fake)
        {
            await Task.Yield();
            using (seam.Override(fake))
            {
                bothInstalled.SignalAndWait(TimeSpan.FromSeconds(10)).Should().BeTrue();
                var seen = seam.Current;
                bothChecked.SignalAndWait(TimeSpan.FromSeconds(10)).Should().BeTrue();
                return seen;
            }
        }

        var a = seam.NewFake();
        var b = seam.NewFake();
        var seenByA = Task.Run(() => Run(a));
        var seenByB = Task.Run(() => Run(b));

        (await seenByA).Should().BeSameAs(a);
        (await seenByB).Should().BeSameAs(b);
        seam.Current.Should().BeOfType(seam.PassThrough, "the test's own flow never saw either override");
    }

    [TestCaseSource(nameof(Seams))]
    public async Task ChildThatOutlivesItsScope_NeverSeesAnotherFlowsOverride(PluginSeam seam)
    {
        using var childStarted = new ManualResetEventSlim();
        using var otherInstalled = new ManualResetEventSlim();
        var scopeA = seam.Override(seam.NewFake());

        // The child inherits scope A, outlives it, and reads the engine while an unrelated flow holds scope B.
        var child = Task.Run(() =>
        {
            childStarted.Set();
            otherInstalled.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
            return seam.Current;
        });
        childStarted.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        scopeA.Dispose();

        var other = Task.Run(() =>
        {
            using var scopeB = seam.Override(seam.NewFake());
            otherInstalled.Set();
            return child.GetAwaiter().GetResult();
        });

        (await other).Should().BeOfType(seam.PassThrough, "a disposed scope no longer applies, and scope B belongs to another flow");
    }

    [TestCaseSource(nameof(Seams))]
    public void ScopesDisposedOutOfOrder_StillResolveToTheLiveOne(PluginSeam seam)
    {
        var a = seam.NewFake();
        var b = seam.NewFake();

        var scopeA = seam.Override(a);
        var scopeB = seam.Override(b);
        scopeA.Dispose();
        seam.Current.Should().BeSameAs(b);
        scopeB.Dispose();
        seam.Current.Should().BeOfType(seam.PassThrough);

        scopeA = seam.Override(a);
        scopeB = seam.Override(b);
        scopeB.Dispose();
        seam.Current.Should().BeSameAs(a);
        scopeA.Dispose();
        seam.Current.Should().BeOfType(seam.PassThrough);
    }

    [TestCaseSource(nameof(Seams))]
    public async Task Override_FlowsIntoWorkStartedInsideTheScope(PluginSeam seam)
    {
        var fake = seam.NewFake();
        using var scope = seam.Override(fake);

        var seen = await Task.Run(() => seam.Current);

        seen.Should().BeSameAs(fake);
    }
}
