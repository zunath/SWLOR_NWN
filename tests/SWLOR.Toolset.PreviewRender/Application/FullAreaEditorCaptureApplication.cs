using Avalonia.Controls.ApplicationLifetimes;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.PreviewRender.Application;

internal sealed class FullAreaEditorCaptureApplication : SWLOR.Toolset.App
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("A real desktop is required.");
        var repositoryRoot = Environment.GetEnvironmentVariable("SWLOR_TEST_REPOSITORY_ROOT")
            ?? throw new InvalidOperationException("Select the real SWLOR repository with SWLOR_TEST_REPOSITORY_ROOT.");
        var haksRoot = Environment.GetEnvironmentVariable("SWLOR_HAKS_ROOT")
            ?? throw new InvalidOperationException("Select the actual HAK corpus with SWLOR_HAKS_ROOT.");
        var output = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_OUTPUT")
            ?? throw new InvalidOperationException("Select the full AreaEditorView image with SWLOR_AREA_EDITOR_CAPTURE_OUTPUT.");
        var configPath = Path.Combine(Path.GetFullPath(repositoryRoot), "Build", "hakbuilder.json");
        if (!File.Exists(configPath))
            throw new FileNotFoundException("The selected SWLOR repository must contain Build/hakbuilder.json.", configPath);

        var resources = ResourceIndex.FromHakBuilderConfig(configPath, haksRoot);
        resources.InitializationTask.GetAwaiter().GetResult();
        Console.WriteLine("The configured SWLOR HAK stack is indexed; constructing the real module area editor.");
        FullAreaEditorCapture.Start(desktop, resources, output);
    }
}
