using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class ExamineItemWindowTests
{
    [Test]
    public void EveryExamineHandler_IsAppliedByTheItemExamineWindow()
    {
        // Players' native examine panel is disabled, so its examine events never run for them; the NUI
        // window must apply each handler's item details itself.
        var handlerTypes = typeof(ExamineItemViewModel).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(method => method.GetCustomAttributes<NWNEventHandler>()
                .Any(handler => handler.Script == ScriptName.OnExamineObjectBefore))
            .Select(method => method.DeclaringType!.Name)
            .Distinct()
            .ToList();
        handlerTypes.Should().Contain("PowerGlovePairLifecycle");

        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "ExamineItemViewModel.cs"));
        var start = source.IndexOf("public static void ShowExamineWindow(", StringComparison.Ordinal);
        start.Should().BeGreaterThan(0);
        var end = source.IndexOf("protected override void Initialize(", start, StringComparison.Ordinal);
        var showWindow = source[start..end];

        foreach (var type in handlerTypes)
            showWindow.Should().Contain($"{type}.", $"{type} adds examine details that players would otherwise never see");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
