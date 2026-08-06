using System.Text;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>The expensive MDL resolver must never hold the item editor's UI thread.</summary>
    [NonParallelizable]
    public class ItemModelPreviewLoadingTests
    {
        [AvaloniaTest]
        public void ModelResolutionIsProgressiveAndDiscardsAnOlderResult()
        {
            using var firstStarted = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var calls = 0;
            using var editor = new ItemEditorViewModel(
                Item(),
                "preview_item",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                resolveModel: (_, female) =>
                {
                    Interlocked.Increment(ref calls);
                    if (!female)
                    {
                        firstStarted.Set();
                        release.Wait();
                    }

                    return new RenderModel { Name = female ? "female" : "male" };
                });

            try
            {
                // Construction returned while the first resolver is still blocked. Changing the
                // mannequin starts a newer generation without waiting for the old one.
                firstStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                editor.IsModelPreviewLoading.Should().BeTrue();
                editor.HasModelPreview.Should().BeFalse();
                editor.PreviewFemale = true;
                Dispatcher.UIThread.RunJobs();
                editor.PreviewFemale = false;
                Dispatcher.UIThread.RunJobs();
                editor.PreviewFemale = true;
                Dispatcher.UIThread.RunJobs();

                release.Set();
                DrainUntil(() => !editor.IsModelPreviewLoading);

                calls.Should().Be(2, "only the active render and the newest queued state should run");
                editor.HasModelPreview.Should().BeTrue();
                editor.PreviewScene!.Instances.Single().Model!.Name.Should().Be("female",
                    "a late completion from the previous mannequin must not replace the current preview");
            }
            finally
            {
                release.Set();
            }
        }

        private static void DrainUntil(Func<bool> condition)
        {
            for (var attempt = 0; attempt < 200 && !condition(); attempt++)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
            }

            Dispatcher.UIThread.RunJobs();
            condition().Should().BeTrue("the background preview should publish promptly");
        }

        private static JsonGffStruct Item() =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
            {
              "__data_type": "UTI ",
              "TemplateResRef": { "type": "resref", "value": "preview_item" },
              "Tag": { "type": "cexostring", "value": "preview_item" }
            }
            """)).Root;
    }
}
