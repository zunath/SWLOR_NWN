using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Factions;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [NonParallelizable]
    public sealed class FactionEditorWindowTests
    {
        [AvaloniaTest]
        public void WindowRendersTheSimpleAndAdvancedWorkflows()
        {
            var root = Path.Combine(
                Path.GetTempPath(), "swlor-faction-window-" + Guid.NewGuid().ToString("N"));
            var facDirectory = Path.Combine(root, "fac");
            Directory.CreateDirectory(facDirectory);
            var facPath = Path.Combine(facDirectory, "repute.fac.json");
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "fac", "repute.fac.json"), facPath);
            var factionCount = FacDocument.Load(facPath).FactionList.Count;
            var usage = Enumerable.Range(0, factionCount).ToDictionary(
                id => id,
                _ => FactionReferenceUsage.Unknown);
            var viewModel = new FactionEditorViewModel(
                root,
                usage,
                new OutputLogService(),
                new EditorPromptService());
            var window = new FactionEditorWindow(viewModel);
            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                window.GetVisualDescendants().Should().NotBeEmpty();
                window.GetVisualDescendants().OfType<TabItem>()
                    .Select(item => item.Header?.ToString())
                    .Should().Contain(new[] { "Simple editor", "Advanced" });
                var factionList = window.FindControl<ListBox>("FactionList");
                factionList.Should().NotBeNull();
                factionList!.ItemCount.Should().Be(factionCount);
                factionList.GetVisualDescendants().OfType<ListBoxItem>()
                    .Should().OnlyContain(item =>
                        item.HorizontalContentAlignment == Avalonia.Layout.HorizontalAlignment.Stretch);
                viewModel.Factions.Should().OnlyContain(faction =>
                    faction.UsageSummary == string.Empty);
                viewModel.HasUsageSummary.Should().BeFalse();
                window.GetVisualDescendants().OfType<TextBlock>()
                    .Select(text => text.Text)
                    .Should().NotContain("References updated automatically");

                viewModel.SelectedFaction = viewModel.Factions[5];
                viewModel.RequestRemoveFactionCommand.Execute(null);
                viewModel.RemoveSummary.Should().Contain(
                    "Every blueprint and placed object using this faction will move to Merchant");
                viewModel.CancelRemoveFactionCommand.Execute(null);

                viewModel.EditorPage = 1;
                Dispatcher.UIThread.RunJobs();
                window.GetVisualDescendants().OfType<NumericUpDown>()
                    .Should().NotBeEmpty("Advanced exposes the directional 0–100 values");
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void OpeningPathDoesNotPerformTheModuleWideUsageScan()
        {
            var path = Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset",
                "Factions",
                "FactionEditorWindow.axaml.cs");
            var source = File.ReadAllText(path);

            source.Should().NotContain("ScanUsage(",
                "opening must not parse the module's hundreds of megabytes of GIT resources");
            source.Should().Contain("FactionReferenceUsage.Unknown");
        }

        [Test]
        public void MainMenuPutsErfManagerInFileAndFactionEditorInTools()
        {
            var path = Path.Combine(
                CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Shell", "MainWindow.axaml");
            var document = XDocument.Load(path);
            XNamespace axaml = "https://github.com/avaloniaui";
            var topLevel = document.Root!
                .Descendants(axaml + "Menu")
                .Single()
                .Elements(axaml + "MenuItem")
                .ToList();

            var file = topLevel.Single(item => (string?)item.Attribute("Header") == "_File");
            var tools = topLevel.Single(item => (string?)item.Attribute("Header") == "_Tools");

            file.Elements(axaml + "MenuItem")
                .Select(item => (string?)item.Attribute("Header"))
                .Should().Contain("ERF _Manager...");
            tools.Elements(axaml + "MenuItem")
                .Select(item => (string?)item.Attribute("Header"))
                .Should().Contain("_Faction Editor...")
                .And.NotContain("ERF _Manager...");
        }
    }
}
