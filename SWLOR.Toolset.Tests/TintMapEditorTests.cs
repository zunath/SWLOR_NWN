using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Creatures;
using SWLOR.Toolset.Editors.TintMaps;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class TintMapEditorTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root.");
            }
        }

        private static ResourceIndex Resources() =>
            ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"));

        private static RenderModel ModelWith(string material) =>
            new()
            {
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "sample",
                        TextureName = material,
                        Positions = Array.Empty<float>(),
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = Array.Empty<int>(),
                        Transform = System.Numerics.Matrix4x4.Identity
                    }
                }
            };

        [Test]
        public void PickerWritesPackedRgbAndResetRemovesIt()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var root = JsonGffDocument.Parse(
                Encoding.UTF8.GetBytes("""{"__data_type":"UTI "}""")).Root;
            var variables = new VarTable(root);
            var edits = new List<string>();
            var editor = new TintMapEditorViewModel(
                variables,
                (description, mutation) =>
                {
                    edits.Add(description);
                    mutation();
                    return true;
                },
                catalog!);

            editor.Reload(ModelWith("pmo0_footl10"));
            editor.Colors.Select(row => row.Layer)
                .Should().BeEquivalentTo(
                    new[] { TintMapLayerType.Leather1, TintMapLayerType.Leather2 });

            var leather = editor.Colors.Single(row => row.Layer == TintMapLayerType.Leather1);
            leather.Color = Color.FromRgb(12, 34, 56);

            var stored = variables.GetInt(leather.Key);
            stored.Should().NotBeNull();
            TintMapColor.TryFromStoredValue(stored!.Value, out var color).Should().BeTrue();
            color.Should().Be(new TintMapColor(12, 34, 56));
            leather.IsCustom.Should().BeTrue();

            leather.ResetCommand.Execute(null);
            variables.GetInt(leather.Key).Should().BeNull();
            leather.IsCustom.Should().BeFalse();
            edits.Should().HaveCount(2);
        }

        [AvaloniaTest]
        public void TintEditorViewLoadsTheAvaloniaColorPicker()
        {
            var view = new TintMapEditorView
            {
                DataContext = new TintMapEditorViewModel(
                    new VarTable(new JsonGffStruct()),
                    (_, mutation) =>
                    {
                        mutation();
                        return true;
                    },
                    TintMapCatalog.Load(Resources())!)
            };
            ((TintMapEditorViewModel)view.DataContext!).Reload(ModelWith("pmo0_footl10"));
            var window = new Window { Content = view };

            window.Show();
            try
            {
                view.GetVisualDescendants().OfType<ColorPicker>().Should().NotBeEmpty();
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest]
        public void ApplicationTemplateRendersTintEditorInsideAContentControl()
        {
            var editor = new TintMapEditorViewModel(
                new VarTable(new JsonGffStruct()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                TintMapCatalog.Load(Resources())!);
            editor.Reload(ModelWith("pmh0_robe010"));
            var host = new ContentControl { Content = editor };
            var window = new Window { Content = host };

            window.Show();
            try
            {
                Dispatcher.UIThread.RunJobs();
                host.GetVisualDescendants().OfType<TintMapEditorView>().Should().ContainSingle(
                    "the shared application template must replace the view model object with its view");
                host.GetVisualDescendants().OfType<ColorPicker>().Should().NotBeEmpty(
                    "item editors must expose unrestricted custom RGB controls");
                host.GetVisualDescendants().OfType<TextBlock>()
                    .Select(text => text.Text)
                    .Should()
                    .NotContain(typeof(TintMapEditorViewModel).FullName);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest]
        public void DedicatedCreatureEditorRendersTintControls()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "tint_creature", "Tint Creature"));
            var resources = Resources();
            var model = ModelWith("pmh0_robe010");
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(Path.GetTempPath(), "utc", "tint_creature.utc.json"),
                "tint_creature",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                resources,
                _ => model,
                _ => (AppearanceRow?)null,
                null,
                tintMapCatalog: TintMapCatalog.Load(resources));
            editor.TintMapEditor.Should().NotBeNull();
            editor.TintMapEditor!.Reload(model);
            editor.IsAppearanceTabSelected = true;
            editor.SelectedAppearanceSectionIndex = 3;

            var view = new CreatureEditorView { DataContext = editor };
            var window = new Window { Width = 1280, Height = 800, Content = view };
            window.Show();
            try
            {
                Dispatcher.UIThread.RunJobs();
                view.GetVisualDescendants().OfType<TintMapEditorView>().Should().ContainSingle(
                    "the dedicated UTC editor must retain the tint surface after its base refactor");
                view.GetVisualDescendants().OfType<ColorPicker>().Should().NotBeEmpty(
                    "creature materials must expose unrestricted custom RGB controls");
            }
            finally
            {
                window.Close();
            }
        }
    }
}
