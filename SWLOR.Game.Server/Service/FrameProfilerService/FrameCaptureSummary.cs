using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWLOR.Game.Server.Service.FrameProfilerService
{
    /// <summary>
    /// Frame-time distribution and counter totals over a capture or summary window.
    /// </summary>
    public sealed class FrameCaptureSummary
    {
        public int Frames { get; }
        public double Seconds { get; }
        public double P50Milliseconds { get; }
        public double P95Milliseconds { get; }
        public double P99Milliseconds { get; }
        public double MaxMilliseconds { get; }
        public int SlowFrames { get; }
        public double SlowFrameThresholdMilliseconds { get; }
        public FrameCounters Totals { get; }
        public string TopHandlers { get; }

        public FrameCaptureSummary(IReadOnlyList<float> frameMilliseconds, double seconds, double slowFrameThresholdMilliseconds,
            FrameCounters totals, string topHandlers = null)
        {
            var sorted = frameMilliseconds.OrderBy(value => value).ToArray();
            Frames = sorted.Length;
            Seconds = seconds;
            P50Milliseconds = Percentile(sorted, 0.50);
            P95Milliseconds = Percentile(sorted, 0.95);
            P99Milliseconds = Percentile(sorted, 0.99);
            MaxMilliseconds = sorted.Length == 0 ? 0 : sorted[^1];
            SlowFrameThresholdMilliseconds = slowFrameThresholdMilliseconds;
            SlowFrames = sorted.Count(value => value > slowFrameThresholdMilliseconds);
            Totals = totals;
            TopHandlers = topHandlers;
        }

        /// <summary>
        /// Nearest-rank percentile of an ascending array.
        /// </summary>
        public static double Percentile(IReadOnlyList<float> ascending, double fraction)
        {
            if (ascending.Count == 0)
                return 0;

            var rank = (int)Math.Ceiling(fraction * ascending.Count);
            return ascending[Math.Clamp(rank, 1, ascending.Count) - 1];
        }

        public override string ToString()
        {
            var seconds = Math.Max(Seconds, 0.001);
            var handlers = TopHandlers == null ? string.Empty : $"; top handlers: {TopHandlers}";
            return string.Create(CultureInfo.InvariantCulture,
                $"{Frames} frames in {Seconds:0.0} s ({Frames / seconds:0.0} fps); frame ms p50 {P50Milliseconds:0.00} p95 {P95Milliseconds:0.00} " +
                $"p99 {P99Milliseconds:0.00} max {MaxMilliseconds:0.00}; {SlowFrames} frames over {SlowFrameThresholdMilliseconds:0} ms; " +
                $"per second: {Totals.Describe(seconds, "/s")}{handlers}");
        }
    }
}
