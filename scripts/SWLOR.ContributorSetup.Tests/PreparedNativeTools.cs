namespace SWLOR.ContributorSetup.Tests;

internal sealed class PreparedNativeTools : NativeTools
{
    private readonly string _directory;
    internal bool Prepared { get; private set; }
    public PreparedNativeTools(string root, CommandRunner runner)
        : base(root, runner, NativeToolsTests.Configuration, windows: true)
    {
        _directory = Path.Combine(root, "native");
    }
    public override IEnumerable<string> CheckPrerequisites(string profile) => Array.Empty<string>();
    public override bool IsPrepared() => true;
    public override Task<string> PrepareAsync(CancellationToken cancellation)
    {
        Prepared = true;
        return Task.FromResult(_directory);
    }
}
