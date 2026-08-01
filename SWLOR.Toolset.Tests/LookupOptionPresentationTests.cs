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
        public void GendersUseNamesWithoutExposingTheirEngineRowIds()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"gender-presentation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "gender.2da"),
                    "2DA V2.0\r\n\r\nCONSTANT NAME\r\n" +
                    "0 Male ****\r\n" +
                    "1 Female ****\r\n" +
                    "2 Both ****\r\n" +
                    "3 Other ****\r\n" +
                    "4 None ****\r\n");
                var lookup = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
                var provider = new LookupOptionProvider(
                    new WorkspaceContext(
                        path => new Domain.Workspace.ModuleWorkspace(path),
                        new OutputLogService()),
                    twoDaLookups: lookup);

                var options = provider.GetOptions(LookupKeys.Gender);

                options.Select(option => option.Id).Should().Equal(0, 1, 2, 3, 4);
                options.Select(option => option.ToString())
                    .Should().Equal("Male", "Female", "Both", "Other", "None");
                options.Select(option => option.BehaviorDisplay)
                    .Should().Equal("Male", "Female", "Both", "Other", "None");
                options.Should().OnlyContain(option => !option.ShowId);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void CreatureMovementRatesUseNamesWithoutExposingTheirEngineRowIds()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"movement-presentation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "creaturespeed.2da"),
                    "2DA V2.0\r\n\r\nLabel Name\r\n" +
                    "0 PC_Movement ****\r\n" +
                    "1 Immobile ****\r\n" +
                    "2 Very_Slow ****\r\n" +
                    "3 Slow ****\r\n" +
                    "4 Normal ****\r\n" +
                    "5 Fast ****\r\n" +
                    "6 Very_Fast ****\r\n" +
                    "7 Default ****\r\n" +
                    "8 DM_Fast ****\r\n" +
                    "9 Aircraft ****\r\n");
                var lookup = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
                var provider = new LookupOptionProvider(
                    new WorkspaceContext(
                        path => new Domain.Workspace.ModuleWorkspace(path),
                        new OutputLogService()),
                    twoDaLookups: lookup);

                var options = provider.GetOptions(LookupKeys.CreatureMovementRates);

                options.Select(option => option.Id).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
                options.Select(option => option.ToString()).Should().Equal(
                    "PC_Movement", "Immobile", "Very_Slow", "Slow", "Normal",
                    "Fast", "Very_Fast", "Default", "DM_Fast", "Aircraft");
                options.Select(option => option.BehaviorDisplay)
                    .Should().Equal(options.Select(option => option.Display));
                options.Should().OnlyContain(option => !option.ShowId);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void RacesUseNamesWithoutExposingTheirEngineRowIds()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"race-presentation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "racialtypes.2da"),
                    "2DA V2.0\r\n\r\nLabel Name Constant\r\n" +
                    "0 Dwarf **** RACIAL_TYPE_DWARF\r\n" +
                    "6 Human **** RACIAL_TYPE_HUMAN\r\n");
                var lookup = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
                var provider = new LookupOptionProvider(
                    new WorkspaceContext(
                        path => new Domain.Workspace.ModuleWorkspace(path),
                        new OutputLogService()),
                    twoDaLookups: lookup);

                var options = provider.GetOptions(LookupKeys.Races);

                options.Select(option => option.Id).Should().Equal(0, 1);
                options.Select(option => option.BehaviorDisplay).Should().Equal("Dwarf", "Human");
                options.Should().OnlyContain(option => !option.ShowId);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void SoundSetsUseNamesWithoutExposingTheirEngineRowIds()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"soundset-presentation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "soundset.2da"),
                    "2DA V2.0\r\n\r\nLABEL STRREF RESREF\r\n" +
                    "0 Female_Seductress **** ss_femsed\r\n" +
                    "85 Monodrone **** ss_monodrone\r\n");
                var lookup = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
                var provider = new LookupOptionProvider(
                    new WorkspaceContext(
                        path => new Domain.Workspace.ModuleWorkspace(path),
                        new OutputLogService()),
                    twoDaLookups: lookup);

                var options = provider.GetOptions(LookupKeys.SoundSets);

                options.Select(option => option.Id).Should().Equal(0, 1);
                options.Select(option => option.BehaviorDisplay)
                    .Should().Equal("Female_Seductress", "Monodrone");
                options.Should().OnlyContain(option => !option.ShowId);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public async Task FactionsUseNamesWithoutExposingTheirInternalIds()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"faction-presentation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(scratch, "are"));
            Directory.CreateDirectory(Path.Combine(scratch, "utc"));
            Directory.CreateDirectory(Path.Combine(scratch, "fac"));
            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "fac", "repute.fac.json"),
                Path.Combine(scratch, "fac", "repute.fac.json"));

            var context = new WorkspaceContext(
                path => new Domain.Workspace.ModuleWorkspace(path),
                new OutputLogService());
            try
            {
                context.Open(scratch);
                var options = new LookupOptionProvider(context).GetOptions(LookupKeys.Factions);

                options.Should().NotBeEmpty();
                options.Select(option => option.Id).Should().ContainInOrder(0, 1, 2);
                options.Select(option => option.BehaviorDisplay).Should().ContainInOrder(
                    "PC", "Hostile", "Commoner");
                options.Should().OnlyContain(option => !option.ShowId);

                await context.Catalog!.BuildTask;
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
