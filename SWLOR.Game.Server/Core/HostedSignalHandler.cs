using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The signal callback of a game build the host boots: the body of the native entry's
    /// <c>NativeInteropManager.OnSignal</c> after it has read the signal, with the same signal table
    /// (<see cref="NativeInteropManager.ProcessSignal"/>: <c>ON_MODULE_LOAD_FINISH</c> initializes the script executor).
    /// </summary>
    public sealed class HostedSignalHandler : IHostSignalHandler
    {
        private readonly Action<string> _processSignal;

        /// <summary>Handles signals as the native entry does.</summary>
        public HostedSignalHandler()
            : this(NativeInteropManager.ProcessSignal)
        {
        }

        /// <summary>For tests: <paramref name="processSignal"/> stands in for <see cref="NativeInteropManager.ProcessSignal"/>.</summary>
        public HostedSignalHandler(Action<string> processSignal)
        {
            _processSignal = processSignal ?? throw new ArgumentNullException(nameof(processSignal));
        }

        /// <inheritdoc/>
        public void OnSignal(string signal)
        {
            var entered = ManagedEntryDepth.Enter();
            try
            {
                _processSignal(signal);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"Signal processing exception: {e}", true);
            }
            finally
            {
                ManagedEntryDepth.Exit(entered);
            }
        }
    }
}
