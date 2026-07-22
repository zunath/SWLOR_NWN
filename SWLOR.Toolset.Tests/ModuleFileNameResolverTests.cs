using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class ModuleFileNameResolverTests
    {
        private string _directory = null!;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"module-filename-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void ConfiguredNameWinsBeforeTheConfiguredArchiveExists()
        {
            File.WriteAllText(Path.Combine(_directory, "Star Wars LOR.mod"), "legacy");
            File.WriteAllText(
                Path.Combine(_directory, "config.json"),
                "{\"ModuleFileName\":\"Star Wars LOR v2.mod\"}");

            ModuleFileNameResolver.Read(_directory).Should().Be("Star Wars LOR v2.mod");
        }

        [Test]
        public void MalformedOrUnsafeConfigFallsBackToAnExistingArchive()
        {
            File.WriteAllText(Path.Combine(_directory, "Star Wars LOR.mod"), "legacy");
            File.WriteAllText(
                Path.Combine(_directory, "config.json"),
                "{\"ModuleFileName\":\"..\\\\outside.mod\"}");

            ModuleFileNameResolver.Read(_directory).Should().Be("Star Wars LOR.mod");
        }
    }
}
