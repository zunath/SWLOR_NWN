using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
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
        }

        private CategoryService OpenService()
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(_module);
            return new CategoryService(context, log);
        }
    }
}
