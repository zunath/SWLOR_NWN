using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Counts expensive work per server frame, logs frames slower than the configured threshold, and
    /// writes a frame-time summary on an interval. Off unless SWLOR_FRAME_PROFILER_ENABLED is set or a
    /// capture is running; while off every hook costs one branch.
    /// </summary>
    public static class FrameProfiler
    {
        private const int TopHandlerCount = 5;
        private const int CaptureTopHandlerCount = 12;
        private static readonly FrameCounters _frame = new();
        private static readonly FrameCounters _window = new();
        private static readonly FrameCounters _capture = new();
        private static readonly List<float> _windowFrames = new();
        private static readonly List<float> _captureFrames = new();
        private static readonly Dictionary<string, (int Calls, long Ticks)> _handlers = new();
        private static readonly Dictionary<string, (int Calls, long Ticks)> _captureHandlers = new();
        private static readonly Dictionary<string, (int Calls, long Ticks)> _captureNamed = new();
        private static readonly Dictionary<string, (int Calls, long Ticks)> _frameNamed = new();
        private static bool _configured;
        private static bool _capturing;
        private static double _slowFrameMilliseconds = 50;
        private static double _summarySeconds = 60;
        private static long _frameStart;
        private static long _windowStart;
        private static long _captureStart;
        private static int _managedDepth;
        private static long _excludedTicks;

        public static bool IsEnabled { get; private set; }
        public static bool IsCapturing => _capturing;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Load()
        {
            var settings = ApplicationSettings.Get();
            _configured = settings.FrameProfilerEnabled;
            _slowFrameMilliseconds = settings.FrameProfilerSlowFrameMilliseconds;
            _summarySeconds = settings.FrameProfilerSummarySeconds;
            IsEnabled = _configured || _capturing;

            if (_configured)
                Log.Write(LogGroup.Performance,
                    $"Frame profiler enabled: slow frames over {_slowFrameMilliseconds} ms, summary every {_summarySeconds} s.", true);
        }

        /// <summary>
        /// Called once at the start of every server frame. Closes the previous frame.
        /// </summary>
        public static void OnFrameStart(ulong frame)
        {
            if (!IsEnabled)
            {
                _frameStart = 0;
                return;
            }

            var now = Stopwatch.GetTimestamp();
            if (_frameStart == 0)
            {
                _frame.Clear();
                _handlers.Clear();
                _frameNamed.Clear();
                _windowStart = now;
            }
            else
            {
                CompleteFrame(frame - 1, now);
                // The profiler's own log writes must not count toward the next frame.
                now = Stopwatch.GetTimestamp();
            }

            _frameStart = now;
        }

        public static long Start() => IsEnabled ? Stopwatch.GetTimestamp() : 0;

        public static void Stop(FrameMetric metric, long start, long bytes = 0)
        {
            if (start == 0)
                return;

            var index = (int)metric;
            _frame.Counts[index]++;
            _frame.Ticks[index] += Stopwatch.GetTimestamp() - start;
            _frame.Bytes[index] += bytes;
        }

        /// <summary>
        /// Stops a timer and, during a capture, also totals it under a name such as a status effect type.
        /// </summary>
        public static void Stop(FrameMetric metric, long start, string name)
        {
            if (start == 0)
                return;

            var elapsed = Stopwatch.GetTimestamp() - start;
            var index = (int)metric;
            _frame.Counts[index]++;
            _frame.Ticks[index] += elapsed;

            if (_capturing)
            {
                var key = $"{metric} {name}";
                _frameNamed.TryGetValue(key, out var total);
                _frameNamed[key] = (total.Calls + 1, total.Ticks + elapsed);
            }
        }

        /// <summary>
        /// UTF-8 size of a payload, computed only while profiling.
        /// </summary>
        public static int Utf8Bytes(string text) =>
            IsEnabled && text != null ? System.Text.Encoding.UTF8.GetByteCount(text) : 0;

        public static void Count(FrameMetric metric, long amount = 1)
        {
            if (IsEnabled)
                _frame.Counts[(int)metric] += amount;
        }

        /// <summary>
        /// Starts timing managed work entered from the engine. Only the outermost entry adds time,
        /// so nested scripts, hooks and closures are counted but not double-timed.
        /// </summary>
        public static long EnterManaged()
        {
            if (!IsEnabled)
                return 0;

            if (_managedDepth == 0)
                _excludedTicks = 0;
            _managedDepth++;
            return Stopwatch.GetTimestamp();
        }

        public static void ExitManaged(FrameMetric metric, long start, string handler = null)
        {
            if (start == 0)
                return;

            var elapsed = Stopwatch.GetTimestamp() - start;
            _managedDepth--;
            var index = (int)metric;
            _frame.Counts[index]++;
            if (_managedDepth <= 0)
            {
                _managedDepth = 0;
                _frame.Ticks[index] += Math.Max(0, elapsed - _excludedTicks);
                _excludedTicks = 0;
            }

            if (handler != null)
            {
                _handlers.TryGetValue(handler, out var total);
                _handlers[handler] = (total.Calls + 1, total.Ticks + elapsed);
            }
        }

        /// <summary>
        /// Removes engine work called from inside a managed scope, such as a hooked native function's
        /// original implementation, from that scope's managed time.
        /// </summary>
        public static void ExcludeFromManaged(long start)
        {
            if (start == 0 || _managedDepth == 0)
                return;

            _excludedTicks += Stopwatch.GetTimestamp() - start;
        }

        /// <summary>
        /// Starts recording every frame until <see cref="EndCapture"/>, enabling the profiler if needed.
        /// </summary>
        public static void BeginCapture()
        {
            _capture.Clear();
            _captureFrames.Clear();
            _captureHandlers.Clear();
            _captureNamed.Clear();
            _captureStart = Stopwatch.GetTimestamp();
            if (!IsEnabled)
                _frameStart = 0;
            _capturing = true;
            IsEnabled = true;
        }

        public static FrameCaptureSummary EndCapture()
        {
            var seconds = FrameCounters.TicksToMilliseconds(Stopwatch.GetTimestamp() - _captureStart) / 1000d;
            var totals = new FrameCounters();
            _capture.AddTo(totals);
            var summary = new FrameCaptureSummary(_captureFrames, seconds, _slowFrameMilliseconds, totals,
                $"{DescribeHandlers(_captureHandlers, CaptureTopHandlerCount)}; top named: {DescribeHandlers(_captureNamed, CaptureTopHandlerCount)}");
            _capturing = false;
            IsEnabled = _configured;
            if (!IsEnabled)
                _frameStart = 0;
            return summary;
        }

        private static void CompleteFrame(ulong frame, long now)
        {
            var milliseconds = (float)FrameCounters.TicksToMilliseconds(now - _frameStart);
            string slowFrame = null;
            string summary = null;

            if (milliseconds > _slowFrameMilliseconds)
                slowFrame = string.Create(CultureInfo.InvariantCulture,
                    $"Slow frame {frame}: {milliseconds:0.0} ms; {_frame.Describe()}; top handlers: {DescribeHandlers(_handlers, TopHandlerCount)}");

            if (_configured)
            {
                _frame.AddTo(_window);
                _windowFrames.Add(milliseconds);
                var windowSeconds = FrameCounters.TicksToMilliseconds(now - _windowStart) / 1000d;
                if (windowSeconds >= _summarySeconds)
                {
                    summary = $"Frame summary: {new FrameCaptureSummary(_windowFrames, windowSeconds, _slowFrameMilliseconds, _window)}";
                    _window.Clear();
                    _windowFrames.Clear();
                    _windowStart = now;
                }
            }

            if (_capturing)
            {
                _frame.AddTo(_capture);
                _captureFrames.Add(milliseconds);
                foreach (var (handler, total) in _handlers)
                {
                    _captureHandlers.TryGetValue(handler, out var captured);
                    _captureHandlers[handler] = (captured.Calls + total.Calls, captured.Ticks + total.Ticks);
                }

                foreach (var (name, total) in _frameNamed)
                {
                    _captureNamed.TryGetValue(name, out var captured);
                    _captureNamed[name] = (captured.Calls + total.Calls, captured.Ticks + total.Ticks);
                }
            }

            if (slowFrame != null)
                Log.Write(LogGroup.Performance, slowFrame);
            if (summary != null)
                Log.Write(LogGroup.Performance, summary);

            // Cleared after the writes above so their LogWrite counts are dropped with the frame.
            _frame.Clear();
            _handlers.Clear();
            _frameNamed.Clear();
        }

        private static string DescribeHandlers(Dictionary<string, (int Calls, long Ticks)> handlers, int count)
        {
            if (handlers.Count == 0)
                return "none";

            return string.Join(", ", handlers
                .OrderByDescending(handler => handler.Value.Ticks)
                .Take(count)
                .Select(handler => string.Create(CultureInfo.InvariantCulture,
                    $"{handler.Key} {FrameCounters.TicksToMilliseconds(handler.Value.Ticks):0.0} ms x{handler.Value.Calls}")));
        }
    }
}
