using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Sources;

namespace SWLOR.Toolset.Tests
{
    public class ObjectSourceSectionTests
    {
        [Test]
        public async Task LoadedPlacement_UsesAreaNameAndGoToCallback()
        {
            var placement = new ObjectPlacement(
                ResourceType.Utw, "arrival_wp", "dan_enclave", 7, "ARRIVAL", 1f, 2f, 3f);
            ObjectPlacement? navigated = null;
            var source = new ObjectSourceSectionViewModel(
                ResourceType.Utw,
                "arrival_wp",
                (_, _) => Task.FromResult<IReadOnlyList<ObjectPlacement>>(new[] { placement }),
                area => area == "dan_enclave" ? "Dantooine Jedi Enclave" : area,
                value => navigated = value);

            await WaitUntilAsync(() => !source.IsLoading);

            var row = source.Placements.Should().ContainSingle().Subject;
            row.AreaName.Should().Be("Dantooine Jedi Enclave");
            row.Detail.Should().Contain("ARRIVAL").And.Contain("1.0, 2.0, 3.0");
            source.GoToCommand.Execute(row);
            navigated.Should().BeSameAs(placement);
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow.AddSeconds(2);
            while (!condition() && DateTime.UtcNow < timeout)
                await Task.Delay(10);
            condition().Should().BeTrue();
        }
    }
}
