using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.NWN.API.Tests.Seam;

/// <summary>The runtime defaults to the pass-through engine, and the override scope swaps and restores it per async flow.</summary>
[TestFixture]
public class NWScriptRuntimeTests
{
    [Test]
    public void DefaultEngine_IsThePassThroughImplementation()
    {
        NWScriptRuntime.Current.Should().BeOfType<NwnCoreNWScript>();
        NWScriptRuntime.Current.Should().BeSameAs(NWScriptRuntime.Current, "the default is created once");
    }

    [Test]
    public void Override_InstallsTheEngineAndRestoresTheDefaultOnDispose()
    {
        var before = NWScriptRuntime.Current;
        var fake = new FakeNWScript();

        using (NWScriptRuntime.Override(fake))
        {
            NWScriptRuntime.Current.Should().BeSameAs(fake);
        }

        NWScriptRuntime.Current.Should().BeSameAs(before);
        NWScriptRuntime.Current.Should().BeOfType<NwnCoreNWScript>();
    }

    [Test]
    public void NestedOverrides_RestoreTheirPreviousEngine()
    {
        var outer = new FakeNWScript();
        var inner = new FakeNWScript();

        using (NWScriptRuntime.Override(outer))
        {
            using (NWScriptRuntime.Override(inner))
            {
                NWScriptRuntime.Current.Should().BeSameAs(inner);
            }

            NWScriptRuntime.Current.Should().BeSameAs(outer);
        }

        NWScriptRuntime.Current.Should().BeOfType<NwnCoreNWScript>();
    }

    [Test]
    public void DisposingAScopeTwice_DoesNotDisturbAnotherActiveScope()
    {
        var first = new FakeNWScript();
        var second = new FakeNWScript();

        var firstScope = NWScriptRuntime.Override(first);
        firstScope.Dispose();
        using (NWScriptRuntime.Override(second))
        {
            firstScope.Dispose();

            NWScriptRuntime.Current.Should().BeSameAs(second);
        }

        NWScriptRuntime.Current.Should().BeOfType<NwnCoreNWScript>();
    }

    [Test]
    public void Override_RejectsNull()
    {
        var action = () => NWScriptRuntime.Override(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Overrides_AreIsolatedBetweenConcurrentFlows()
    {
        using var bothInstalled = new Barrier(2);
        using var bothChecked = new Barrier(2);

        async Task<INWScript> Run(FakeNWScript fake)
        {
            await Task.Yield();
            using (NWScriptRuntime.Override(fake))
            {
                bothInstalled.SignalAndWait(TimeSpan.FromSeconds(10)).Should().BeTrue();
                var seen = NWScriptRuntime.Current;
                bothChecked.SignalAndWait(TimeSpan.FromSeconds(10)).Should().BeTrue();
                return seen;
            }
        }

        var a = new FakeNWScript();
        var b = new FakeNWScript();
        var seenByA = Task.Run(() => Run(a));
        var seenByB = Task.Run(() => Run(b));

        (await seenByA).Should().BeSameAs(a);
        (await seenByB).Should().BeSameAs(b);
        NWScriptRuntime.Current.Should().BeOfType<NwnCoreNWScript>("the test's own flow never saw either override");
    }

    [Test]
    public async Task Override_FlowsIntoWorkStartedInsideTheScope()
    {
        var fake = new FakeNWScript();
        using var scope = NWScriptRuntime.Override(fake);

        var seen = await Task.Run(() => NWScriptRuntime.Current);

        seen.Should().BeSameAs(fake);
    }

    [Test]
    public void StaticWrapperCalls_FollowTheInstalledEngine()
    {
        var first = new FakeNWScript();
        var second = new FakeNWScript();

        using (NWScriptRuntime.Override(first))
        {
            SWLOR.NWN.API.NWScript.NWScript.GetTag(1001u);
            using (NWScriptRuntime.Override(second))
            {
                SWLOR.NWN.API.NWScript.NWScript.GetTag(1002u);
            }

            SWLOR.NWN.API.NWScript.NWScript.GetTag(1003u);
        }

        first.CallsTo("GetTag").Select(c => c.Arguments[0]).Should().Equal(1001u, 1003u);
        second.CallsTo("GetTag").Select(c => c.Arguments[0]).Should().Equal(1002u);
    }
}
