using System.Runtime.ExceptionServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Missing optional creature equipment is common in the module corpus. Previewing those creatures
    /// must fall back to their authored body parts without throwing and catching FileNotFoundException:
    /// first-chance exceptions still interrupt debuggers and flooded Visual Studio during cache warming.
    /// </summary>
    [NonParallelizable]
    public class BlueprintPreviewExceptionTests
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

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        [Test]
        public void MissingEquippedArmorDoesNotThrowAFirstChanceFileNotFoundException()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(), "SWLOR.Toolset.Tests", "preview-missing-armor-" + Guid.NewGuid().ToString("N"));
            var utcDirectory = Path.Combine(moduleRoot, "utc");
            Directory.CreateDirectory(Path.Combine(moduleRoot, "are"));
            Directory.CreateDirectory(utcDirectory);
            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "atris_jedi.utc.json"),
                Path.Combine(utcDirectory, "atris_jedi.utc.json"));

            try
            {
                var twoDa = new TwoDaService(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_2da"));
                var tlk = TlkService.Load(
                    Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json"));
                var index = ResourceIndex.FromHakBuilderConfig(
                    Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                    Path.Combine(RepoRoot, "SWLOR_Haks"));
                index.EnsureInitialized();

                var context = new WorkspaceContext(
                    path => new ModuleWorkspace(path, index),
                    new OutputLogService());
                context.Open(moduleRoot);
                context.Catalog!.BuildTask.GetAwaiter().GetResult();

                var creature = context.Workspace!
                    .LoadBlueprint(ResourceType.Utc, "atris_jedi")
                    .Fields;
                creature.Get("Equip_ItemList")
                    .Elements!
                    .Single(item =>
                        item.TryGet("EquippedRes", out var equipped) &&
                        equipped.GetString() == "atris_robes")
                    .Get("EquippedRes")
                    .SetString("missing_armor");

                var renderer = new BlueprintPreviewRenderer(
                    context,
                    index,
                    appearances: new AppearanceService(twoDa, tlk),
                    twoDa: twoDa,
                    tlk: tlk);
                var firstChanceMisses = new List<FileNotFoundException>();
                EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
                {
                    if (args.Exception is FileNotFoundException missing)
                        firstChanceMisses.Add(missing);
                };

                AppDomain.CurrentDomain.FirstChanceException += handler;
                try
                {
                    renderer.BuildModel(ResourceType.Utc, creature);
                }
                finally
                {
                    AppDomain.CurrentDomain.FirstChanceException -= handler;
                }

                firstChanceMisses.Should().BeEmpty(
                    "missing optional armor is a normal preview fallback, not an exceptional condition");
            }
            finally
            {
                Directory.Delete(moduleRoot, recursive: true);
            }
        }
    }
}
