using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    public sealed class BlueprintCopyFactoryTests
    {
        [TestCase(ResourceType.Utc)]
        [TestCase(ResourceType.Uti)]
        [TestCase(ResourceType.Utp)]
        [TestCase(ResourceType.Utd)]
        [TestCase(ResourceType.Utm)]
        [TestCase(ResourceType.Utt)]
        [TestCase(ResourceType.Uts)]
        [TestCase(ResourceType.Utw)]
        public void CopyChangesOnlyTheBlueprintIdentity(ResourceType type)
        {
            const string sourceResRef = "source_probe";
            const string copyResRef = "source_probe001";
            var source = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(type, sourceResRef, "Source Probe"));
            using (EditScope.EnterConstruction())
            {
                source.Root.SetString("Tag", GffFieldType.CExoString, "authored_tag");
                source.Root.SetString("COPY_SENTINEL", GffFieldType.CExoString, "preserve me");
            }

            var sourceBefore = source.ToBytes();
            var copy = JsonGffDocument.Parse(
                BlueprintCopyFactory.CreateFileContent(type, source, copyResRef));

            source.ToBytes().Should().Equal(sourceBefore, "Edit Copy must not mutate its source document");
            copy.Root.GetStringOrNull(BlueprintCopyFactory.IdentityFieldName(type))
                .Should().Be(copyResRef);
            copy.Root.GetStringOrNull("Tag").Should().Be("authored_tag");
            copy.Root.GetStringOrNull("COPY_SENTINEL").Should().Be("preserve me");
        }

        [TestCase("crate", "crate001")]
        [TestCase("crate001", "crate002")]
        [TestCase("crate099", "crate100")]
        [TestCase("crate12", "crate12001")]
        [TestCase("abcdefghijklmnop", "abcdefghijklm001")]
        [TestCase("UPPER_CASE", "upper_case001")]
        public void NextResRefMatchesAuroraNumbering(string source, string expected)
        {
            BlueprintCopyFactory.NextResRef(source, Array.Empty<string>()).Should().Be(expected);
        }

        [Test]
        public void NextResRefSkipsExistingCopiesCaseInsensitively()
        {
            BlueprintCopyFactory.NextResRef(
                    "crate",
                    new[] { "crate001", "CRATE002", "crate003" })
                .Should().Be("crate004");
        }

        [Test]
        public void WorkspaceCopyNumberSkipsModuleAndIndexedBlueprints()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "swlor-copy-collision-" + Guid.NewGuid().ToString("N"));
            var moduleRoot = Path.Combine(root, "Module");
            var indexedRoot = Path.Combine(root, "Indexed");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(moduleRoot, folder));
            Directory.CreateDirectory(indexedRoot);
            File.WriteAllBytes(
                Path.Combine(moduleRoot, "utp", "crate001.utp.json"),
                BlueprintTemplateFactory.CreateFileContent(ResourceType.Utp, "crate001", "Module Copy"));
            File.WriteAllBytes(Path.Combine(indexedRoot, "crate002.utp"), Array.Empty<byte>());

            try
            {
                var resources = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[] { new ResourceIndex.HakLayer("fixture", indexedRoot) });
                var workspace = new ModuleWorkspace(moduleRoot, resources);

                BlueprintCopyFactory.NextResRef(workspace, ResourceType.Utp, "crate")
                    .Should().Be("crate003");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void CopyRejectsNonBlueprintTypes()
        {
            var source = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(ResourceType.Utp, "probe", "Probe"));

            var copy = () => BlueprintCopyFactory.CreateFileContent(
                ResourceType.Area,
                source,
                "probe001");

            copy.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
