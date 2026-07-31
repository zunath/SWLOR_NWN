using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class LookupOptionPresentationTests
    {
        [Test]
        public void PhenotypesUseNamesWithoutExposingTheirEngineRowIds()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"phenotype-presentation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "phenotype.2da"),
                    "2DA V2.0\r\n\r\nLabel Name\r\n" +
                    "0 Normal ****\r\n" +
                    "1 Skinny ****\r\n" +
                    "2 Large ****\r\n");
                var lookup = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
                var provider = new LookupOptionProvider(
                    new WorkspaceContext(
                        path => new Domain.Workspace.ModuleWorkspace(path),
                        new OutputLogService()),
                    twoDaLookups: lookup);

                var options = provider.GetOptions(LookupKeys.Phenotype);

                options.Select(option => option.Id).Should().Equal(0, 1, 2);
                options.Select(option => option.ToString()).Should().Equal("Normal", "Skinny", "Large");
                options.Select(option => option.BehaviorDisplay).Should().Equal("Normal", "Skinny", "Large");
                options.Should().OnlyContain(option => !option.ShowId);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void OtherLookupsKeepTheirIdPresentationByDefault()
        {
            var option = new LookupOption(7, "Default");

            option.ToString().Should().Be("7: Default");
            option.BehaviorDisplay.Should().Be("Default (7)");
        }
    }
}
