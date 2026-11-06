using System.Reflection;
using FluentAssertions;
using NRediSearch;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public class DBSearchRetryTests
{
    [Test]
    public void SuccessfulSearch_DoesNotWaitOrRetry()
    {
        var attempts = 0;
        var expected = EmptySearchResult();
        var result = DBSearchRetry.Execute(() => { attempts++; return expected; }, "QuestContract",
            _ => Assert.Fail("A successful read must not wait."),
            _ => Assert.Fail("A successful read must not report a failure."));

        result.Should().BeSameAs(expected);
        attempts.Should().Be(1);
    }

    [TestCaseSource(nameof(TransientFailures))]
    public void TransientFailure_RetriesTheReadAndReturnsItsResult(Exception failure)
    {
        var attempts = 0;
        var delays = new List<int>();
        var reports = new List<string>();
        var expected = EmptySearchResult();

        var result = DBSearchRetry.Execute(() =>
        {
            if (++attempts < 3) throw failure;
            return expected;
        }, "QuestContract", delays.Add, reports.Add);

        result.Should().BeSameAs(expected);
        attempts.Should().Be(3);
        delays.Should().Equal(250, 500);
        reports.Should().HaveCount(2).And.OnlyContain(message =>
            message.Contains("QuestContract") && message.Contains(failure.GetType().Name));
    }

    [TestCaseSource(nameof(TransientFailures))]
    public void PersistentFailure_StopsAndPreservesTheException(Exception failure)
    {
        var attempts = 0;
        var delays = new List<int>();
        var reports = new List<string>();

        Action search = () => DBSearchRetry.Execute(() =>
        {
            attempts++;
            throw failure;
        }, "QuestContract", delays.Add, reports.Add);

        search.Should().Throw<Exception>().Which.Should().BeSameAs(failure);
        attempts.Should().Be(3);
        delays.Should().Equal(250, 500);
        reports.Should().HaveCount(3);
        reports[^1].Should().Contain("failed after 3 attempts").And.Contain("QuestContract");
    }

    [TestCaseSource(nameof(PermanentFailures))]
    public void PermanentFailure_IsNotRetried(Exception failure)
    {
        var attempts = 0;
        Action search = () => DBSearchRetry.Execute(() =>
        {
            attempts++;
            throw failure;
        }, "QuestContract", _ => Assert.Fail("Permanent errors must not wait."),
            _ => Assert.Fail("Permanent errors must not be reported as transient."));

        search.Should().Throw<Exception>().Which.Should().BeSameAs(failure);
        attempts.Should().Be(1);
    }

    private static SearchResult EmptySearchResult()
    {
        var database = DispatchProxy.Create<IDatabase, SearchDatabase>();
        ((SearchDatabase)database).FailuresRemaining = 0;
        return new Client(nameof(QuestContract), database).Search(new Query("*"));
    }

    private static IEnumerable<Exception> TransientFailures()
    {
        yield return new TaskCanceledException("A task was canceled.");
        yield return new RedisTimeoutException("Search timed out.", CommandStatus.Sent);
        foreach (var kind in new[]
        {
            ConnectionFailureType.UnableToResolvePhysicalConnection,
            ConnectionFailureType.SocketFailure, ConnectionFailureType.SocketClosed,
            ConnectionFailureType.Loading, ConnectionFailureType.UnableToConnect
        })
            yield return new RedisConnectionException(kind, "Connection temporarily unavailable.");
    }

    private static IEnumerable<Exception> PermanentFailures()
    {
        yield return new RedisServerException("Unknown Index name");
        yield return new RedisServerException("Syntax error");
        yield return new RedisConnectionException(ConnectionFailureType.AuthenticationFailure, "Invalid credentials.");
        yield return new RedisConnectionException(ConnectionFailureType.ConnectionDisposed, "Connection disposed.");
        yield return new RedisConnectionException(ConnectionFailureType.ProtocolFailure, "Unexpected response.");
        yield return new InvalidOperationException("Invalid query.");
        yield return new OperationCanceledException("Caller cancellation.");
    }

    public class SearchDatabase : DispatchProxy
    {
        public List<List<string>> Commands { get; } = new();
        public int FailuresRemaining { get; set; } = 1;

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod!.Name != nameof(IDatabase.Execute))
                throw new InvalidOperationException($"Unexpected database call: {targetMethod.Name}");

            Commands.Add(new[] { args[0].ToString()! }
                .Concat(((IEnumerable<object>)args[1]).Select(value => value.ToString()!)).ToList());
            if (FailuresRemaining-- > 0) throw new TaskCanceledException("A task was canceled.");
            return RedisResult.Create(new[] { RedisResult.Create((RedisValue)0) });
        }
    }
}
