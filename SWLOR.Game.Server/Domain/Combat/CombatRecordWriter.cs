using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Owns one recording's files. The main thread only enqueues event structs; this writer's
    /// thread-pool task serializes them, aggregates the summary and does all file I/O.
    /// </summary>
    public sealed class CombatRecordWriter
    {
        private const int IdleWaitMilliseconds = 250;
        private const byte NewLine = (byte)'\n';

        private readonly ConcurrentQueue<CombatRecordEvent> _queue = new();
        private readonly SemaphoreSlim _wake = new(0);
        private readonly TaskCompletionSource<CombatRecordSummary> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task _run = Task.CompletedTask;
        private volatile bool _completeRequested;
        private volatile string _error;
        private long _eventsWritten;

        public string EventsPath { get; }
        public string SummaryPath { get; }
        public Task<CombatRecordSummary> Completion => _completion.Task;
        public string Error => _error;
        public long EventsWritten => Interlocked.Read(ref _eventsWritten);

        public CombatRecordWriter(string eventsPath, string summaryPath)
        {
            EventsPath = eventsPath;
            SummaryPath = summaryPath;
        }

        public void Start()
        {
            _run = Task.Run(RunAsync);
        }

        public void Enqueue(in CombatRecordEvent recordEvent)
        {
            _queue.Enqueue(recordEvent);
        }

        /// <summary>
        /// Requests the writer to flush every queued event, write the summary and exit.
        /// Does not wait; observe <see cref="Completion"/>.
        /// </summary>
        public void Complete()
        {
            _completeRequested = true;
            _wake.Release();
        }

        /// <summary>
        /// Requests completion and waits until every queued event and the summary are written
        /// (or the recording has failed). Never throws; read <see cref="Error"/>.
        /// </summary>
        public Task StopAsync()
        {
            Complete();
            return _run;
        }

        /// <summary>
        /// Blocks until the writer exits. Only for server shutdown, where the main thread has
        /// nothing left to run.
        /// </summary>
        public bool WaitForExit(TimeSpan timeout)
        {
            return _run.Wait(timeout);
        }

        private async Task RunAsync()
        {
            var builder = new CombatRecordSummaryBuilder();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(EventsPath))!);
                using (var stream = new FileStream(EventsPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 65536))
                using (var json = new Utf8JsonWriter(stream))
                {
                    while (true)
                    {
                        // Read the flag before draining: everything enqueued before Complete()
                        // is then guaranteed to be in this final drain.
                        var completing = _completeRequested;
                        Drain(stream, json, builder);
                        stream.Flush();

                        if (completing)
                            break;

                        await _wake.WaitAsync(IdleWaitMilliseconds).ConfigureAwait(false);
                    }
                }

                var summary = builder.Build(DateTime.UtcNow);
                WriteSummary(summary);
                _completion.TrySetResult(summary);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                Log.Write(LogGroup.Error, $"Combat recording writer failed for '{EventsPath}': {ex}", true);
                await DiscardUntilCompleteAsync().ConfigureAwait(false);
                _completion.TrySetException(ex);
            }
        }

        private void Drain(Stream stream, Utf8JsonWriter json, CombatRecordSummaryBuilder builder)
        {
            while (_queue.TryDequeue(out var recordEvent))
            {
                CombatRecordSerializer.Write(json, recordEvent);
                json.Flush();
                json.Reset();
                stream.WriteByte(NewLine);
                builder.Add(recordEvent);
                Interlocked.Increment(ref _eventsWritten);
            }
        }

        private void WriteSummary(CombatRecordSummary summary)
        {
            using var stream = new FileStream(SummaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            CombatRecordSummaryWriter.Write(json, summary);
        }

        private async Task DiscardUntilCompleteAsync()
        {
            while (true)
            {
                var completing = _completeRequested;
                while (_queue.TryDequeue(out _))
                {
                }

                if (completing)
                    return;

                await _wake.WaitAsync(IdleWaitMilliseconds).ConfigureAwait(false);
            }
        }
    }
}
