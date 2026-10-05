using System.Diagnostics;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests.Viewport;

[TestFixture]
[Category("NativeViewport")]
public sealed class NativeMaterialViewportTests
{
    [Test]
    public async Task ProductionMaterialRendersInThePackagedSharedViewport()
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_HAKS_ROOT") ?? SWLOR.Toolset.Tests.Support.ToolsetCorpusPaths.HaksRoot;
        Assert.That(root, Is.Not.Null.And.Not.Empty, "Select the read-only corpus with SWLOR_HAKS_ROOT; a real desktop/GPU is required.");
        var framework = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = framework.Parent!;
        var repository = configuration.Parent!.Parent!.Parent!;
        var fixture = Path.Combine(repository.FullName, "tests", "SWLOR.Toolset.PreviewRender", "bin", configuration.Name, framework.Name, "SWLOR.Toolset.PreviewRender.dll");
        Assert.That(File.Exists(fixture), Is.True, "Build the referenced viewport fixture.");
        var output = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"swlor-material-{Guid.NewGuid():N}.png");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(fixture);
        start.Environment["SWLOR_HAKS_ROOT"] = Path.GetFullPath(root!);
        start.Environment["SWLOR_PREVIEW_RENDER_OUTPUT"] = output;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); Assert.Fail("The owned viewport fixture exceeded forty-five seconds."); }
        TestContext.WriteLine(await stdout);
        TestContext.WriteLine(await stderr);
        if (File.Exists(output)) TestContext.AddTestAttachment(output);
        Assert.That(process.ExitCode, Is.Zero, "The real material frame and repeated-frame GPU reuse must pass.");
        Assert.That(File.Exists(output), Is.True, "Retain the real framebuffer image.");
    }
}
