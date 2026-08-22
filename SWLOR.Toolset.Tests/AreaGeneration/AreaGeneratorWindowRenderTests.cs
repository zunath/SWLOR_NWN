using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.AreaGeneration;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public sealed class AreaGeneratorWindowRenderTests
{
    [AvaloniaTest]
    public void Window_LoadsItsCompiledXaml()
    {
        var window = new AreaGeneratorWindow();

        window.Title.Should().Contain("Area Generator");
        window.Content.Should().NotBeNull();

        window.Close();
    }
}
