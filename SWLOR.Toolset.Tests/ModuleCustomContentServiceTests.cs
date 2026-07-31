using FluentAssertions;
using NUnit.Framework;
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
    }
}
