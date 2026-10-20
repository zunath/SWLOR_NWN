using Avalonia.Headless.NUnit;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Distribution;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Tests;

public class WorkspaceSetupWindowTests
{
    [AvaloniaTest]
    public void FirstRunSetupRendersAndOffersBothWorkspacePaths()
    {
        var settingsFile = Path.Combine(Path.GetTempPath(), "swlor_setup_" + Guid.NewGuid().ToString("N") + ".json");
        var window = new WorkspaceSetupWindow(ToolsetSettings.Load(settingsFile));
        try
        {
            window.Show();
            window.Content.Should().BeOfType<StackPanel>();
            var panel = (StackPanel)window.Content!;
            panel.Children.OfType<Button>().Select(button => button.Content)
                .Should().Contain(new object[] { "Create SWLOR workspace", "Open existing workspace", "Locate Neverwinter Nights" });
            var output = Environment.GetEnvironmentVariable("SWLOR_TOOLSET_SETUP_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(output))
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize(560, (int)window.Bounds.Height));
                bitmap.Render(window);
                bitmap.Save(output);
            }
        }
        finally { window.Close(); File.Delete(settingsFile); }
    }
}
