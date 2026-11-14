using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The host entry's run-script callback carries the script executor's -1/0/1 as "handled" and the conditional result,
/// which the host turns back into the same number for the engine.
/// </summary>
[TestFixture]
public class HostedScriptDispatcherTests
{
    private FacadeContainer _container;
    private IDisposable _installed;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void Install() => _installed = _container.Install();

    [TearDown]
    public void Uninstall() => _installed.Dispose();

    [TestCase(-1, false, false)]
    [TestCase(0, true, false)]
    [TestCase(1, true, true)]
    public void The_executors_result_is_split_into_handled_and_the_conditional_answer(int executorResult, bool handled, bool condition)
    {
        var calls = new List<(string, uint)>();
        var dispatcher = new HostedScriptDispatcher((script, self) =>
        {
            calls.Add((script, self));
            return executorResult;
        });

        dispatcher.TryHandle("appears", 0x10, out var conditionalResult).Should().Be(handled);

        conditionalResult.Should().Be(condition);
        calls.Should().Equal(("appears", 0x10u));
    }

    [Test]
    public void An_executor_that_throws_is_logged_and_the_script_is_not_handled()
    {
        var dispatcher = new HostedScriptDispatcher((_, _) => throw new InvalidOperationException("executor"));

        dispatcher.TryHandle("mod_load", 0, out var conditionalResult).Should().BeFalse();

        conditionalResult.Should().BeFalse();
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [Test]
    public void The_dispatcher_needs_an_executor()
    {
        var create = () => new HostedScriptDispatcher(null);

        create.Should().Throw<ArgumentNullException>();
    }
}
