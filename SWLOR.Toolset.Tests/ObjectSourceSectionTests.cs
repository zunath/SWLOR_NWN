using System.Globalization;
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

        [Test, NonParallelizable]
        public void Position_UsesInvariantFormattingUnderCommaDecimalCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var row = new ObjectPlacementRowViewModel(
                    new ObjectPlacement(
                        ResourceType.Utw, "arrival_wp", "dan_enclave", 7, "", 1f, 2f, 3f),
                    "Dantooine Jedi Enclave");

                row.Position.Should().Be("1.0, 2.0, 3.0");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public async Task SetResRef_InvalidatesBeforeLoadingAndIgnoresStaleResults()
        {
            var oldPlacement = new ObjectPlacement(
                ResourceType.Utw, "old_wp", "old_area", 0, "OLD", 1f, 2f, 3f);
            var newPlacement = new ObjectPlacement(
                ResourceType.Utw, "new_wp", "new_area", 0, "NEW", 4f, 5f, 6f);
            var oldLoad = new TaskCompletionSource<IReadOnlyList<ObjectPlacement>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = new List<string>();
            var source = new ObjectSourceSectionViewModel(
                ResourceType.Utw,
                "old_wp",
                (_, resRef) =>
                {
                    calls.Add("find:" + resRef);
                    return resRef == "old_wp"
                        ? oldLoad.Task
                        : Task.FromResult<IReadOnlyList<ObjectPlacement>>(new[] { newPlacement });
                },
                area => area,
                _ => { },
                () => calls.Add("invalidate"));

            source.SetResRef("new_wp");
            await WaitUntilAsync(() => !source.IsLoading);
            oldLoad.SetResult(new[] { oldPlacement });
            await Task.Delay(20);

            calls.Should().StartWith("find:old_wp", "invalidate", "find:new_wp");
            source.Placements.Should().ContainSingle()
                .Which.Placement.Should().BeSameAs(newPlacement);
        }

        [Test]
        public async Task FailedLoad_ClearsStaleRowsAndReportsTheError()
        {
            var placement = new ObjectPlacement(
                ResourceType.Utw, "old_wp", "old_area", 0, "OLD", 1f, 2f, 3f);
            var fail = false;
            var source = new ObjectSourceSectionViewModel(
                ResourceType.Utw,
                "old_wp",
                (_, _) => fail
                    ? Task.FromException<IReadOnlyList<ObjectPlacement>>(new IOException("broken GIT"))
                    : Task.FromResult<IReadOnlyList<ObjectPlacement>>(new[] { placement }),
                area => area,
                _ => { });
            await WaitUntilAsync(() => !source.IsLoading);
            source.Placements.Should().ContainSingle();

            fail = true;
            await source.RefreshCommand.ExecuteAsync(null);

            source.Placements.Should().BeEmpty();
            source.LoadError.Should().Contain("broken GIT");
            source.Status.Should().Be(source.LoadError);
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
