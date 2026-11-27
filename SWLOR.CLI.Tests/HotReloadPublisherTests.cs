using System.Diagnostics;
using FluentAssertions;
using Nwn.Hosting.Contracts.Generations;
using Nwn.Hosting.Reload;
using NUnit.Framework;
using SWLOR.Game.Host.Reload;

namespace SWLOR.CLI.Tests;

/// <summary>
/// <c>SWLOR.CLI --reload</c>: the candidate lands in its own <c>dotnet/generations/&lt;id&gt;/</c> folder where the host looks
/// for it, the signal is the hosting package's request naming it, and the tool prints the receipts the host's own handler
/// writes (the "server" in these tests is SWLOR.Game.Host's <see cref="ReloadSignalHandler"/> with a stand-in reload).
/// </summary>
[TestFixture]
public sealed class HotReloadPublisherTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(10);

    private string _root = null!;
    private string _home = null!;
    private string _output = null!;
    private ReloadLayout _layout = null!;
    private StringWriter _console = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "SWLOR reload tests", Guid.NewGuid().ToString("N"));
        _home = Path.Combine(_root, "server home");
        _output = Path.Combine(_root, "game output");
        _layout = ReloadLayout.ForServerHome(_home);
        _console = new StringWriter();
        Directory.CreateDirectory(Path.Combine(_home, "dotnet", "game"));
        File.WriteAllText(Path.Combine(_home, "dotnet", "SWLOR.Game.Host.dll"), "host");
        WriteBuildOutput(_output, "build one");
    }

    [TearDown]
    public void TearDown()
    {
        _console.Dispose();
        Directory.Delete(_root, true);
    }

    private static void WriteBuildOutput(string directory, string content)
    {
        Directory.CreateDirectory(Path.Combine(directory, "runtimes", "linux-x64", "native"));
        foreach (var name in new[] { "SWLOR.Game.Server.dll", "SWLOR.Game.Server.deps.json", "SWLOR.Game.Server.runtimeconfig.json", "SWLOR.NWN.API.dll" })
            File.WriteAllText(Path.Combine(directory, name), $"{content} {name}");
        File.WriteAllText(Path.Combine(directory, "runtimes", "linux-x64", "native", "libnative.so"), content + " native");
    }

    private HotReloadPublisher Publisher(Func<string, int>? build = null) =>
        new(_console, build ?? (_ => throw new AssertionException("no build expected")), Poll);

    /// <summary>The server side: SWLOR.Game.Host's handler answers the first signal it sees, as its watcher would call it.</summary>
    private Task Serve(Func<string, ReloadAttempt> reload, List<string>? reloaded = null) =>
        Task.Run(async () =>
        {
            var handler = new ReloadSignalHandler(_layout, (candidate, _) =>
            {
                reloaded?.Add(candidate);
                return Task.FromResult(reload(candidate));
            }, (_, _) => { });
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!File.Exists(_layout.SignalPath))
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("The tool never signalled.");
                await Task.Delay(Poll);
            }

            // What the server was signalled to load is the candidate the tool published.
            await handler.HandleAsync(CancellationToken.None);
        });

    private static ReloadAttempt Attempt(ReloadOutcome outcome, string detail, long? active, TimeSpan? pause) =>
        new(new ReloadReport(outcome, 1, active, TimeSpan.FromMilliseconds(900), TimeSpan.Zero, TimeSpan.FromMilliseconds(400), TimeSpan.Zero, true, detail),
            pause, pause is null ? null : 77UL);

    [Test]
    public void Publishing_copies_the_whole_build_into_the_candidates_own_folder_where_the_host_looks_for_it()
    {
        var id = Guid.NewGuid();

        var candidate = HotReloadPublisher.Publish(_output, _layout, id);

        candidate.Should().Be(Path.Combine(_home, "dotnet", "generations", id.ToString("N")));
        File.ReadAllText(_layout.CandidateAssembly(id)).Should().Be("build one SWLOR.Game.Server.dll");
        File.ReadAllText(Path.Combine(candidate, "runtimes", "linux-x64", "native", "libnative.so")).Should().Be("build one native");
        Directory.GetFiles(candidate, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(candidate, file))
            .Should().BeEquivalentTo(Directory.GetFiles(_output, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(_output, file)));
        Directory.GetDirectories(_layout.GenerationsDirectory).Should().Equal(new[] { candidate }, "no partial copy is left beside it");
        Directory.GetFiles(Path.Combine(_home, "dotnet", "game")).Should().BeEmpty("the running build's folder is never written");

        var again = () => HotReloadPublisher.Publish(_output, _layout, id);
        again.Should().Throw<IOException>().WithMessage("*published once*");
    }

    [Test]
    public void A_copy_that_fails_leaves_no_candidate()
    {
        var id = Guid.NewGuid();

        var publish = () => HotReloadPublisher.Publish(Path.Combine(_root, "missing output"), _layout, id);

        publish.Should().Throw<DirectoryNotFoundException>();
        Directory.GetDirectories(_layout.GenerationsDirectory).Should().BeEmpty();
    }

    [Test]
    public async Task The_signal_is_a_whole_request_of_the_hosting_package_naming_the_candidate()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        HotReloadPublisher.Signal(_layout, first);
        HotReloadPublisher.Signal(_layout, second);

        var request = await DevelopmentReloadFiles.ReadRequestAsync(_layout.SignalPath, CancellationToken.None);
        request.Should().Be(new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, second));
        Directory.GetFiles(_layout.ReloadDirectory).Should().Equal(_layout.SignalPath);
    }

    [Test]
    public async Task A_reload_the_server_applies_prints_each_receipt_and_succeeds()
    {
        var reloaded = new List<string>();
        var server = Serve(_ =>
        {
            // A reload takes a while; the tool polls the receipt every 10 ms meanwhile and sees it say Accepted.
            Thread.Sleep(500);
            return Attempt(ReloadOutcome.Applied, "activated", 2, TimeSpan.FromMilliseconds(640));
        }, reloaded);

        var exitCode = Publisher().Process(_output, _home, 30);
        await server;

        exitCode.Should().Be(0, _console.ToString());
        var id = Guid.ParseExact(Path.GetFileName(Path.GetDirectoryName(reloaded.Single()))!, "N");
        reloaded.Single().Should().Be(_layout.CandidateAssembly(id));
        File.ReadAllText(reloaded.Single()).Should().Be("build one SWLOR.Game.Server.dll");
        var output = _console.ToString();
        output.Should().Contain($"Published candidate {id:N}");
        output.Should().Contain($"Accepted: generation={id:N} status=Accepted");
        output.Should().Contain($"Applied: generation={id:N} status=Applied active=2 validateMs=900 pauseMs=640 frame=77");
    }

    [Test]
    public async Task A_rejected_reload_prints_the_reason_and_fails()
    {
        var server = Serve(_ => Attempt(ReloadOutcome.RejectedBeforeTeardown, "Candidate validation failed: boom", 1, null));

        var exitCode = Publisher().Process(_output, _home, 30);
        await server;

        exitCode.Should().Be(1);
        _console.ToString().Should().Contain("Rejected: generation=").And.Contain("Candidate validation failed: boom");
    }

    [Test]
    public void Without_a_receipt_the_tool_gives_up_and_says_what_to_check()
    {
        var exitCode = Publisher().Process(_output, _home, 1);

        exitCode.Should().Be(1);
        _console.ToString().Should().Contain("No final receipt within 1 s").And.Contain("SWLOR_HOT_RELOAD=true").And.Contain("reload-watching");
        File.Exists(_layout.SignalPath).Should().BeTrue("the request stays for a server that has not answered yet");
    }

    [Test]
    public void A_server_home_without_the_host_entry_is_refused_before_anything_is_published()
    {
        File.Delete(Path.Combine(_home, "dotnet", "SWLOR.Game.Host.dll"));

        var exitCode = Publisher().Process(_output, _home, 1);

        exitCode.Should().Be(1);
        _console.ToString().Should().Contain("has no SWLOR.Game.Host.dll").And.Contain("SWLOR_DOTNET_ENTRY=host");
        Directory.Exists(_layout.GenerationsDirectory).Should().BeFalse();
        File.Exists(_layout.SignalPath).Should().BeFalse();
    }

    [Test]
    public void An_incomplete_build_output_is_refused()
    {
        File.Delete(Path.Combine(_output, "SWLOR.Game.Server.deps.json"));

        var exitCode = Publisher().Process(_output, _home, 1);

        exitCode.Should().Be(1);
        _console.ToString().Should().Contain("The build output is incomplete").And.Contain("SWLOR.Game.Server.deps.json");
        Directory.Exists(_layout.GenerationsDirectory).Should().BeFalse();
    }

    [Test]
    public async Task Without_a_build_output_the_tool_builds_the_release_game_of_the_repository_first()
    {
        var repository = Path.Combine(_root, "repository");
        Directory.CreateDirectory(Path.Combine(repository, "Build"));
        File.WriteAllText(Path.Combine(repository, "SWLOR.Game.Server.sln"), "");
        var built = new List<string>();
        var server = Serve(_ => Attempt(ReloadOutcome.Applied, "activated", 2, TimeSpan.Zero));

        var exitCode = Publisher(project =>
        {
            built.Add(project);
            WriteBuildOutput(Path.Combine(repository, "SWLOR.Game.Server", "bin", "Release", "net10.0"), "release build");
            return 0;
        }).Process(null!, _home, 30, Path.Combine(repository, "Build"));
        await server;

        exitCode.Should().Be(0, _console.ToString());
        built.Should().Equal(Path.Combine(repository, "SWLOR.Game.Server", "SWLOR.Game.Server.csproj"));
        Directory.GetDirectories(_layout.GenerationsDirectory).Should().ContainSingle()
            .Which.Should().Match(directory => File.ReadAllText(Path.Combine(directory, "SWLOR.Game.Server.dll")) == "release build SWLOR.Game.Server.dll");
    }

    [Test]
    public void A_failed_build_publishes_nothing_and_a_build_needs_the_repository()
    {
        var repository = Path.Combine(_root, "repository");
        Directory.CreateDirectory(repository);
        File.WriteAllText(Path.Combine(repository, "SWLOR.Game.Server.sln"), "");
        var outside = Path.Combine(_root, "outside");
        Directory.CreateDirectory(outside);

        var failed = Publisher(_ => 1).Process(null!, _home, 1, repository);
        var noRepository = Publisher().Process(null!, _home, 1, outside);
        var noHome = Publisher().Process(_output, null!, 1, outside);

        failed.Should().Be(1);
        noRepository.Should().Be(1);
        noHome.Should().Be(1);
        _console.ToString().Should().Contain("The build failed; nothing was published")
            .And.Contain("Not in the repository")
            .And.Contain("No server home");
        Directory.Exists(_layout.GenerationsDirectory).Should().BeFalse();
    }

    [Test]
    public void The_repositorys_debugserver_is_the_default_server_home()
    {
        var repository = Path.Combine(_root, "repository");
        Directory.CreateDirectory(Path.Combine(repository, "Build"));
        File.WriteAllText(Path.Combine(repository, "SWLOR.Game.Server.sln"), "");

        var exitCode = Publisher().Process(_output, null!, 1, Path.Combine(repository, "Build"));

        exitCode.Should().Be(1);
        _console.ToString().Should().Contain(Path.Combine(repository, "debugserver", "dotnet") + " has no SWLOR.Game.Host.dll");
        HotReloadPublisher.FindRepositoryRoot(Path.Combine(repository, "Build")).Should().Be(repository);
    }

    [Test]
    public void After_an_applied_reload_only_the_newest_candidates_stay()
    {
        var published = new List<string>();
        for (var index = 0; index < 5; index++)
        {
            var candidate = HotReloadPublisher.Publish(_output, _layout, Guid.NewGuid());
            Directory.SetLastWriteTimeUtc(candidate, new DateTime(2026, 11, 12, 10, index, 0, DateTimeKind.Utc));
            published.Add(candidate);
        }
        Directory.CreateDirectory(Path.Combine(_layout.GenerationsDirectory, "not a candidate"));

        var removed = HotReloadPublisher.Prune(_layout, HotReloadPublisher.CandidatesKept);

        removed.Should().BeEquivalentTo(published.Take(2));
        Directory.GetDirectories(_layout.GenerationsDirectory).Should()
            .BeEquivalentTo(published.Skip(2).Append(Path.Combine(_layout.GenerationsDirectory, "not a candidate")));
        HotReloadPublisher.Prune(ReloadLayout.ForServerHome(Path.Combine(_root, "empty home")), 3).Should().BeEmpty();
    }

    [Test]
    public void The_command_line_publishes_signals_and_reports_a_missing_receipt()
    {
        var info = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        info.ArgumentList.Add(typeof(HotReloadPublisher).Assembly.Location);
        foreach (var argument in new[] { "--reload", "--server-home", _home, "--server-output", _output, "--reload-timeout", "1" })
            info.ArgumentList.Add(argument);

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit(60_000).Should().BeTrue();

        process.ExitCode.Should().Be(1, output.Result + error.Result);
        output.Result.Should().Contain("Published candidate").And.Contain("No final receipt within 1 s");
        Directory.GetDirectories(_layout.GenerationsDirectory).Should().ContainSingle();
        File.Exists(_layout.SignalPath).Should().BeTrue();
    }
}
