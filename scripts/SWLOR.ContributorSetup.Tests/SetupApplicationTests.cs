using NUnit.Framework;

namespace SWLOR.ContributorSetup.Tests;

public class SetupApplicationTests
{
    private string _root = null!;
    private RecordingRunner _runner = null!;
    private PreparedNativeTools _native = null!;
    private SetupApplication _app = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("swlor setup with spaces ").FullName;
        Directory.CreateDirectory(Path.Combine(_root, "SWLOR_Haks", "sw_tlk"));
        File.WriteAllText(Path.Combine(_root, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json"), "{}");
        _runner = new RecordingRunner();
        _native = new PreparedNativeTools(_root, _runner);
        _app = new SetupApplication(_root, _runner, _native);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public async Task DoctorNeverInitializesAssetsBuildsOrPreparesTools()
    {
        Assert.That(await _app.RunAsync(new("Check", "Server"), CancellationToken.None), Is.Zero);
        Assert.That(_native.Prepared, Is.False);
        Assert.That(_runner.Commands.All(command => command.Arguments[0] is "--version" or "info" or "compose"), Is.True);
    }

    [TestCase("9.0.100")]
    [TestCase("10.0.100-preview.1")]
    [TestCase("11.0.100")]
    public async Task UnsupportedSdkStopsBeforeAnyMutation(string version)
    {
        _runner.Version = version;
        Assert.That(await _app.RunAsync(new("Setup", "Toolset"), CancellationToken.None), Is.EqualTo(1));
        Assert.That(_runner.Commands.Count, Is.EqualTo(1));
        Assert.That(_native.Prepared, Is.False);
    }

    [TestCase("git")]
    [TestCase("docker")]
    public async Task MissingPrerequisitePreventsServerPreparation(string command)
    {
        _runner.MissingCommand = command;
        Assert.That(await _app.RunAsync(new("Setup", "Server"), CancellationToken.None), Is.EqualTo(1));
        Assert.That(_native.Prepared, Is.False);
        Assert.That(_runner.Commands.Any(item => item.Arguments[0] == "build"), Is.False);
    }

    [Test]
    public async Task ToolsetSetupNeedsNoDockerAndDisablesBuildDeployment()
    {
        _runner.MissingCommand = "docker";
        Assert.That(await _app.RunAsync(new("Setup", "Toolset"), CancellationToken.None), Is.Zero);
        Assert.That(_native.Prepared, Is.True);
        var build = _runner.Commands.Single(item => item.Arguments[0] == "build");
        Assert.That(build.Arguments, Does.Contain("-p:RunPostBuildEvent=Never"));
        Assert.That(build.Arguments, Does.Contain("SWLOR.Toolset/SWLOR.Toolset.csproj"));
        Assert.That(build.Environment!["SWLOR_NATIVE_TOOLS"], Is.EqualTo(Path.Combine(_root, "native")));
        Assert.That(_runner.Commands.Any(item => item.Name == "docker"), Is.False);
    }

    [Test]
    public void FailedBuildPreventsPackingDeploymentAndLaunch()
    {
        _runner.FailBuild = true;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _app.RunAsync(new("Server", "Server"), CancellationToken.None));
        Assert.That(_runner.Commands.Any(item => item.Arguments.Contains("--server-output") || item.Arguments[0] == "run"), Is.False);
    }

    [Test]
    public async Task ServerBuildsPacksAndDeploysBeforeLaunchingWithChildExitCode()
    {
        _runner.LaunchExitCode = 23;
        Assert.That(await _app.RunAsync(new("Server", "Server"), CancellationToken.None), Is.EqualTo(23));
        var pack = _runner.Commands.Single(item => item.Arguments.Contains("--no-prompt"));
        Assert.That(pack.Directory, Is.EqualTo(Path.Combine(_root, "Module")));
        Assert.That(pack.Arguments, Does.Contain("./Star Wars LOR v2.mod"));
        var deploy = _runner.Commands.Single(item => item.Arguments.Contains("--server-output"));
        Assert.That(deploy.Arguments, Does.Contain(Path.Combine(_root, "SWLOR.Game.Server", "bin", "Debug", "net10.0")));
        Assert.That(deploy.Directory, Is.EqualTo(Path.Combine(_root, "Build")));
        Assert.That(_runner.Commands.Last().Arguments, Is.EqualTo(new[] { "run", "--no-build", "--project", "SWLOR.Runner/SWLOR.Runner.csproj" }));
        Assert.That(_runner.Commands.Last().Timeout, Is.Null);
        Assert.That(_runner.Commands.Where(item => item.Arguments[0] == "build").All(item => item.Arguments.Contains("-p:RunPostBuildEvent=Never")), Is.True);
    }
}
