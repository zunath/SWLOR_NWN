using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FrameProfilerService;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public class FrameProfilerTests
{
    [TearDown]
    public void StopCapture()
    {
        if (FrameProfiler.IsCapturing)
            FrameProfiler.EndCapture();
        FrameProfiler.Configure(false, 50, 60);
    }

    [Test]
    public void DisabledProfiler_RecordsNothing()
    {
        FrameProfiler.IsEnabled.Should().BeFalse();
        FrameProfiler.Start().Should().Be(0);
        FrameProfiler.EnterManaged().Should().Be(0);
    }

    [Test]
    public void Capture_CountsCompletedFramesOnly()
    {
        FrameProfiler.BeginCapture();
        FrameProfiler.OnFrameStart(10);
        FrameProfiler.Count(FrameMetric.BroadcastReceiver, 4);
        FrameProfiler.Stop(FrameMetric.DBGet, FrameProfiler.Start(), 2048);
        FrameProfiler.OnFrameStart(11);
        FrameProfiler.Count(FrameMetric.BroadcastReceiver, 100);

        var summary = FrameProfiler.EndCapture();

        summary.Frames.Should().Be(1);
        summary.Totals.Count(FrameMetric.BroadcastReceiver).Should().Be(4);
        summary.Totals.Count(FrameMetric.DBGet).Should().Be(1);
        summary.Totals.ByteCount(FrameMetric.DBGet).Should().Be(2048);
        FrameProfiler.IsEnabled.Should().BeFalse();
    }

    [Test]
    public void CaptureWhileProfilingEnabled_StartsAtNextFrame()
    {
        // Thresholds high enough that no slow-frame or summary log is written.
        FrameProfiler.Configure(true, 100000, 100000);
        FrameProfiler.OnFrameStart(1);
        for (var write = 0; write < 99; write++)
            FrameProfiler.Stop(FrameMetric.DBSet, FrameProfiler.Start(), 10);
        FrameProfiler.Stop(FrameMetric.StatusEffectApply, FrameProfiler.Start(), "Before capture");

        FrameProfiler.BeginCapture();
        FrameProfiler.Stop(FrameMetric.DBSet, FrameProfiler.Start(), 10);
        FrameProfiler.Stop(FrameMetric.StatusEffectApply, FrameProfiler.Start(), "Capture start frame");
        FrameProfiler.OnFrameStart(2);
        FrameProfiler.Stop(FrameMetric.DBSet, FrameProfiler.Start(), 10);
        FrameProfiler.Stop(FrameMetric.DBSet, FrameProfiler.Start(), 10);
        FrameProfiler.Stop(FrameMetric.StatusEffectApply, FrameProfiler.Start(), "Captured frame");
        FrameProfiler.OnFrameStart(3);

        var summary = FrameProfiler.EndCapture();

        summary.Frames.Should().Be(1);
        summary.Totals.Count(FrameMetric.DBSet).Should().Be(2);
        summary.Totals.ByteCount(FrameMetric.DBSet).Should().Be(20);
        summary.Totals.Count(FrameMetric.StatusEffectApply).Should().Be(1);
        summary.TopHandlers.Should().Contain("StatusEffectApply Captured frame")
            .And.NotContain("Before capture")
            .And.NotContain("Capture start frame");
        FrameProfiler.IsEnabled.Should().BeTrue("configured profiling continues after the capture");
    }

    [Test]
    public void NestedManagedWork_IsCountedButTimedOnce()
    {
        FrameProfiler.BeginCapture();
        FrameProfiler.OnFrameStart(1);
        var outer = FrameProfiler.EnterManaged();
        var inner = FrameProfiler.EnterManaged();
        System.Threading.Thread.Sleep(2);
        FrameProfiler.ExitManaged(FrameMetric.Script, inner, "inner");
        FrameProfiler.ExitManaged(FrameMetric.NativeHook, outer);
        FrameProfiler.OnFrameStart(2);

        var totals = FrameProfiler.EndCapture().Totals;

        totals.Count(FrameMetric.Script).Should().Be(1);
        totals.Elapsed(FrameMetric.Script).Should().Be(0);
        totals.Count(FrameMetric.NativeHook).Should().Be(1);
        totals.Elapsed(FrameMetric.NativeHook).Should().BeGreaterThan(0);
    }

    [Test]
    public void ExcludedEngineWork_IsRemovedFromManagedTime()
    {
        FrameProfiler.BeginCapture();
        FrameProfiler.OnFrameStart(1);
        var hook = FrameProfiler.EnterManaged();
        var engine = FrameProfiler.Start();
        System.Threading.Thread.Sleep(50);
        FrameProfiler.ExcludeFromManaged(engine);
        FrameProfiler.ExitManaged(FrameMetric.ObjectUpdateHook, hook);
        FrameProfiler.OnFrameStart(2);

        var totals = FrameProfiler.EndCapture().Totals;

        totals.Count(FrameMetric.ObjectUpdateHook).Should().Be(1);
        totals.Milliseconds(FrameMetric.ObjectUpdateHook).Should().BeLessThan(25);
    }

    [Test]
    public void NamedTimings_OnlyIncludeCompletedFrames()
    {
        FrameProfiler.BeginCapture();
        FrameProfiler.Stop(FrameMetric.StatusEffectApply, FrameProfiler.Start(), "Before first frame");
        FrameProfiler.OnFrameStart(1);
        FrameProfiler.Stop(FrameMetric.StatusEffectApply, FrameProfiler.Start(), "Completed frame");
        FrameProfiler.OnFrameStart(2);
        FrameProfiler.Stop(FrameMetric.StatusEffectApply, FrameProfiler.Start(), "Unfinished frame");

        var summary = FrameProfiler.EndCapture();

        summary.Totals.Count(FrameMetric.StatusEffectApply).Should().Be(1);
        summary.TopHandlers.Should().Contain("StatusEffectApply Completed frame")
            .And.NotContain("Before first frame")
            .And.NotContain("Unfinished frame");
    }

    [Test]
    public void PayloadSize_CountsUtf8BytesOnlyWhileProfiling()
    {
        FrameProfiler.Utf8Bytes("Évrac").Should().Be(0);

        FrameProfiler.BeginCapture();

        FrameProfiler.Utf8Bytes("Évrac").Should().Be(6);
        FrameProfiler.Utf8Bytes(null).Should().Be(0);
    }

    [Test]
    public void Percentile_UsesNearestRank()
    {
        var frames = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 100 };

        FrameCaptureSummary.Percentile(frames, 0.50).Should().Be(5);
        FrameCaptureSummary.Percentile(frames, 0.95).Should().Be(100);
        FrameCaptureSummary.Percentile(frames, 0.10).Should().Be(1);
        FrameCaptureSummary.Percentile(System.Array.Empty<float>(), 0.5).Should().Be(0);
    }
}
