using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class CategoryServiceTests
    {
        private string _root = string.Empty;
        private string _module = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "swlor-category-service-" + Guid.NewGuid().ToString("N"));
            _module = Path.Combine(_root, "Module");
            Directory.CreateDirectory(Path.Combine(_module, "itp"));
            Directory.CreateDirectory(Path.Combine(_module, "are"));
            Directory.CreateDirectory(Path.Combine(_module, "utc"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void MissingPaletteIsRetriedInsteadOfBeingPermanentlyMarkedSeeded()
        {
            var service = OpenService();

            service.Section(ResourceType.Utp)!.IsSeeded.Should().BeFalse();

            File.WriteAllText(Path.Combine(_module, "itp", "placeablepalcus.itp.json"), """
                {
                  "__data_type": "ITP ",
                  "MAIN": { "type": "list", "value": [
                    { "__struct_id": 1,
                      "NAME": { "type": "cexostring", "value": "Cargo" },
                      "DELETE_ME": { "type": "byte", "value": 0 },
                      "LIST": { "type": "list", "value": [
                        { "__struct_id": 0,
                          "NAME": { "type": "cexostring", "value": "Crate" },
                          "RESREF": { "type": "resref", "value": "crate_01" } }
                      ] } }
                  ] }
                }
                """);

            var retried = service.Section(ResourceType.Utp)!;

            retried.IsSeeded.Should().BeTrue();
            retried.Find("Cargo").Should().NotBeNull();
        }

        [Test]
        public void DeletingTheLoadedSidecarIsDetectedAsAnExternalChange()
        {
            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, """
                { "version": 1, "sections": {
                    "utp": { "seeded": true, "folders": [ { "name": "Cargo" } ] }
                } }
                """);
            var service = OpenService();
            var section = service.Section(ResourceType.Utp)!;
            File.Delete(sidecar);
            section.AddFolder("Interiors");

            var result = service.SaveChanges();

            result.Saved.Should().BeFalse();
            result.Problem.Should().Contain("changed outside");
            File.Exists(sidecar).Should().BeFalse("an external deletion must not be silently recreated");
            service.Section(ResourceType.Utp)!.Find("Cargo").Should().NotBeNull();
            service.Section(ResourceType.Utp)!.Find("Interiors").Should().BeNull(
                "a rejected edit must not remain live and leak into a later save");
        }

        [Test]
        public void ReadOnlySidecarRollsBackRejectedInMemoryEdits()
        {
            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, """
                { "version": 2, "sections": {
                    "utp": { "seeded": true, "folders": [ { "name": "Cargo" } ] }
                } }
                """);
            var service = OpenService();
            service.Section(ResourceType.Utp)!.Find("Cargo")!.Rename("Rejected rename");

            var result = service.SaveChanges();

            result.Saved.Should().BeFalse();
            service.Section(ResourceType.Utp)!.Find("Cargo").Should().NotBeNull();
            service.Section(ResourceType.Utp)!.Find("Rejected rename").Should().BeNull();
        }

        /// <summary>
        /// A name resolved out of the TLK is no more trustworthy than one read out of a palette - several
        /// of the base game's carry a path separator. This repair runs over a tree that is already loaded
        /// and on screen, so throwing here took the open module down after it had opened cleanly.
        /// </summary>
        [Test]
        public void APlaceholderResolvingToANameWithThePathSeparator_IsRepairedRatherThanThrownAt()
        {
            const uint StrRef = TlkService.CustomTlkBase + 42;

            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, $$"""
                { "version": 1, "sections": { "utp": {
                    "pinned": [ "Category {{StrRef}}" ],
                    "folders": [ { "name": "Category {{StrRef}}", "members": [ "crate01" ] } ] } } }
                """);

            var service = OpenService(Tlk(42, "Skin/Hide"));

            var section = service.Section(ResourceType.Utp)!;
            var folder = section.Folders.Should().ContainSingle().Subject;
            folder.Name.Should().Be("Skin-Hide");
            folder.Members.Should().Contain("crate01", "repairing the name must not lose the contents");
            section.Pinned.Should().Equal(new[] { "Skin-Hide" },
                because: "a pin is stored by path, so it has to move with the name");
        }

        private static TlkService Tlk(int entryId, string text) =>
            new(TlkJsonFile.Parse($$"""
                { "language": 0, "entries": [ { "id": {{entryId}}, "text": "{{text}}" } ] }
                """));

        private CategoryService OpenService(TlkService? tlk = null)
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(_module);
            return new CategoryService(context, log, tlk);
        }
    }
}
