using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class ModuleCustomContentServiceTests
    {
        [Test]
        public async Task ReloadsRemainSerializedThroughResultPublication()
        {
            var context = new WorkspaceContext(
                _ => throw new NotSupportedException(),
                new OutputLogService());
            var iniPath = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"missing-nwn-{Guid.NewGuid():N}.ini");
            var service = new ModuleCustomContentService(
                context,
                new OutputLogService(),
                iniPathOverride: iniPath);
            using var releaseFirstPublication = new ManualResetEventSlim();
            var firstPublicationStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var publicationCount = 0;

            service.Reloaded += _ =>
            {
                if (Interlocked.Increment(ref publicationCount) == 1)
                {
                    firstPublicationStarted.SetResult();
                    releaseFirstPublication.Wait();
                }
            };

            var first = Task.Run(() => service.ReloadAsync(Array.Empty<string>(), null));
            Task<ModuleCustomContentReloadResult>? second = null;
            try
            {
                await firstPublicationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                second = Task.Run(() => service.ReloadAsync(Array.Empty<string>(), null));

                var completed = await Task.WhenAny(second, Task.Delay(250));
                completed.Should().NotBe(second,
                    "the first reload still owns publication of its HAK/TLK generation");
            }
            finally
            {
                releaseFirstPublication.Set();
            }

            await first;
            if (second != null)
                await second;
            publicationCount.Should().Be(2);
        }

        [Test]
        public async Task IncompleteSavedHakStackRetainsTheRepositoryFallback()
        {
            var root = Path.Combine(
                Path.GetTempPath(), "swlor-custom-content-fallback-" + Guid.NewGuid().ToString("N"));
            var fallback = Path.Combine(root, "fallback");
            var hakDirectory = Path.Combine(root, "hak");
            var iniPath = Path.Combine(root, "nwn.ini");
            Directory.CreateDirectory(fallback);
            Directory.CreateDirectory(hakDirectory);
            File.WriteAllText(Path.Combine(fallback, "fallback.2da"), "2DA V2.0\r\n");
            File.WriteAllText(Path.Combine(hakDirectory, "present.hak"), string.Empty);
            File.WriteAllText(iniPath, $"[Alias]\r\nHAK={hakDirectory}\r\n");

            try
            {
                var index = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[]
                    {
                        new ResourceIndex.HakLayer("repository", fallback)
                    });
                var context = new WorkspaceContext(
                    _ => throw new NotSupportedException(),
                    new OutputLogService());
                var service = new ModuleCustomContentService(
                    context,
                    new OutputLogService(),
                    resourceIndex: index,
                    iniPathOverride: iniPath);

                var result = await service.ReloadAsync(
                    new[] { "present", "missing" },
                    customTlk: null,
                    retainCurrentHaksWhenMissing: true);

                result.RetainedHakLayers.Should().BeTrue();
                result.MissingHaks.Should().Equal("missing");
                index.HakLayers.Should().ContainSingle()
                    .Which.Name.Should().Be("repository");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public async Task ExplicitReloadWithoutAHakAliasClearsThePreviousLayers()
        {
            var root = Path.Combine(
                Path.GetTempPath(), "swlor-custom-content-clear-" + Guid.NewGuid().ToString("N"));
            var fallback = Path.Combine(root, "fallback");
            var iniPath = Path.Combine(root, "nwn.ini");
            Directory.CreateDirectory(fallback);
            File.WriteAllText(Path.Combine(fallback, "fallback.2da"), "2DA V2.0\r\n");
            File.WriteAllText(iniPath, "[Alias]\r\n");

            try
            {
                var index = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[]
                    {
                        new ResourceIndex.HakLayer("repository", fallback)
                    });
                var context = new WorkspaceContext(
                    _ => throw new NotSupportedException(),
                    new OutputLogService());
                var service = new ModuleCustomContentService(
                    context,
                    new OutputLogService(),
                    resourceIndex: index,
                    iniPathOverride: iniPath);

                var result = await service.ReloadAsync(new[] { "missing" }, customTlk: null);

                result.LoadedHakCount.Should().Be(0);
                result.MissingHaks.Should().Equal("missing");
                result.RetainedHakLayers.Should().BeFalse();
                index.HakLayers.Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
