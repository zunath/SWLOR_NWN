using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The main-loop callback of a game build the host boots: the body of the native entry's
    /// <c>NativeInteropManager.OnMainLoop</c>, for the frame number the host is delivering. The host runs it inline, at the
    /// start of the engine's main loop with <c>OBJECT_SELF</c> invalid, from the first frame, as NWNX runs the native
    /// entry's handler.
    /// </summary>
    public sealed class HostedMainLoop : IMainLoopSink
    {
        private readonly IHostCallbacks _host;
        private readonly Action<ulong> _processMainLoop;

        /// <param name="host">Tells the frame number of the callback being delivered.</param>
        public HostedMainLoop(IHostCallbacks host)
            : this(host, frame => ServerManager.MainLoop.ProcessMainLoop(frame))
        {
        }

        /// <summary>For tests: <paramref name="processMainLoop"/> stands in for <see cref="MainLoopProcessor.ProcessMainLoop"/>.</summary>
        public HostedMainLoop(IHostCallbacks host, Action<ulong> processMainLoop)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _processMainLoop = processMainLoop ?? throw new ArgumentNullException(nameof(processMainLoop));
        }

        /// <inheritdoc/>
        public void Tick()
        {
            var frame = _host.Frame;
            ServerFrame.Current = frame;
            var entered = ManagedEntryDepth.Enter();
            try
            {
                _processMainLoop(frame);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"MainLoop exception: {e}", true);
            }
            finally
            {
                ManagedEntryDepth.Exit(entered);
            }
        }
    }
}
