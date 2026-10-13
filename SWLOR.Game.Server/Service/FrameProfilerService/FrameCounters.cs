using System.Diagnostics;
using System.Text;

namespace SWLOR.Game.Server.Service.FrameProfilerService
{
    /// <summary>
    /// Call counts, elapsed Stopwatch ticks, and byte totals for each <see cref="FrameMetric"/>.
    /// </summary>
    public sealed class FrameCounters
    {
        private static readonly int MetricCount = Enum.GetValues<FrameMetric>().Length;

        public long[] Counts { get; } = new long[MetricCount];
        public long[] Ticks { get; } = new long[MetricCount];
        public long[] Bytes { get; } = new long[MetricCount];

        public long Count(FrameMetric metric) => Counts[(int)metric];
        public long Elapsed(FrameMetric metric) => Ticks[(int)metric];
        public long ByteCount(FrameMetric metric) => Bytes[(int)metric];

        public double Milliseconds(FrameMetric metric) => TicksToMilliseconds(Ticks[(int)metric]);

        public double ManagedMilliseconds =>
            Milliseconds(FrameMetric.Scheduler) +
            Milliseconds(FrameMetric.Script) +
            Milliseconds(FrameMetric.NativeHook) +
            Milliseconds(FrameMetric.Closure) +
            Milliseconds(FrameMetric.ObjectUpdateHook);

        public static double TicksToMilliseconds(long ticks) => ticks * 1000d / Stopwatch.Frequency;

        public void Clear()
        {
            Array.Clear(Counts);
            Array.Clear(Ticks);
            Array.Clear(Bytes);
        }

        public void AddTo(FrameCounters total)
        {
            for (var index = 0; index < MetricCount; index++)
            {
                total.Counts[index] += Counts[index];
                total.Ticks[index] += Ticks[index];
                total.Bytes[index] += Bytes[index];
            }
        }

        /// <summary>
        /// One-line breakdown. Divide by <paramref name="frames"/> or <paramref name="seconds"/> to report rates.
        /// </summary>
        public string Describe(double divisor = 1d, string unit = "")
        {
            string Rate(FrameMetric metric) => Format(Count(metric) / divisor);
            string Ms(FrameMetric metric) => Format(Milliseconds(metric) / divisor);
            string Kb(FrameMetric metric) => Format(ByteCount(metric) / 1024d / divisor);

            var text = new StringBuilder();
            text.Append($"managed {Format(ManagedMilliseconds / divisor)} ms{unit} (scheduler {Ms(FrameMetric.Scheduler)}, ");
            text.Append($"scripts {Ms(FrameMetric.Script)}/{Rate(FrameMetric.Script)}, ");
            text.Append($"hooks {Ms(FrameMetric.NativeHook)}/{Rate(FrameMetric.NativeHook)}, ");
            text.Append($"closures {Ms(FrameMetric.Closure)}/{Rate(FrameMetric.Closure)}, ");
            text.Append($"object updates {Ms(FrameMetric.ObjectUpdateHook)}/{Rate(FrameMetric.ObjectUpdateHook)}); ");
            text.Append($"DB get {Rate(FrameMetric.DBGet)} miss {Ms(FrameMetric.DBGet)} ms (deserialize {Ms(FrameMetric.DBDeserialize)}) {Kb(FrameMetric.DBGet)} KB + {Rate(FrameMetric.DBGetCached)} cached, ");
            text.Append($"set {Rate(FrameMetric.DBSet)} {Ms(FrameMetric.DBSet)} ms {Kb(FrameMetric.DBSet)} KB, ");
            text.Append($"search {Rate(FrameMetric.DBSearch)} {Ms(FrameMetric.DBSearch)} ms + {Rate(FrameMetric.DBSearchDocument)} docs {Ms(FrameMetric.DBSearchDocument)} ms, ");
            text.Append($"retries {Rate(FrameMetric.DBSearchRetry)}, other {Rate(FrameMetric.DBOther)} {Ms(FrameMetric.DBOther)} ms; ");
            text.Append($"broadcasts {Rate(FrameMetric.Broadcast)} {Ms(FrameMetric.Broadcast)} ms -> {Rate(FrameMetric.BroadcastReceiver)} receivers; ");
            text.Append($"name lookups {Rate(FrameMetric.PlayerNameLookup)}; ");
            text.Append($"status apply {Rate(FrameMetric.StatusEffectApply)} {Ms(FrameMetric.StatusEffectApply)} ms, ");
            text.Append($"remove {Rate(FrameMetric.StatusEffectRemove)} {Ms(FrameMetric.StatusEffectRemove)} ms; ");
            text.Append($"stat adjustments {Rate(FrameMetric.StatAdjustment)} {Ms(FrameMetric.StatAdjustment)} ms; ");
            text.Append($"sheet refreshes {Rate(FrameMetric.CharacterSheetRefresh)}, binds {Rate(FrameMetric.GuiBindSend)}; ");
            text.Append($"telegraph {Rate(FrameMetric.TelegraphPlayerUpdate)} players {Ms(FrameMetric.TelegraphPlayerUpdate)} ms {Rate(FrameMetric.TelegraphUniform)} uniforms; ");
            text.Append($"log {Rate(FrameMetric.LogWrite)} lines {Ms(FrameMetric.LogWrite)} ms");
            return text.ToString();
        }

        private static string Format(double value) =>
            value.ToString(value >= 100 ? "0" : value >= 10 ? "0.0" : "0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
