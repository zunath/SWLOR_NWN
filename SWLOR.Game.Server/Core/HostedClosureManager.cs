using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The <see cref="ClosureManager"/> of a game build the host boots. <c>OBJECT_SELF</c> stays here, set and read exactly
    /// as on the native entry (<see cref="ScriptExecutor"/> and <see cref="ScriptExecutionProviderImpl"/> use the same
    /// <see cref="ClosureManager.ObjectSelf"/>), so NWN.Core sees the same object in every script, closure, frame and
    /// script context. Only the scheduling differs: each closure goes to the host's handler, which asks NWNX and tracks
    /// it for the active generation, and runs back through <see cref="RunClosure"/>, the body of the native entry's
    /// closure callback (<c>NativeInteropManager.OnClosure</c> and <c>ClosureManager.OnClosure</c>): the same
    /// <c>OBJECT_SELF</c>, profiling, managed-entry count and error log. A closure NWNX refuses is dropped by the host
    /// without an exception, as the native entry drops it.
    /// </summary>
    public sealed class HostedClosureManager : ClosureManager, global::NWN.Core.ICoreFunctionHandler
    {
        private readonly global::NWN.Core.ICoreFunctionHandler _host;

        /// <param name="host">The host's closure handler.</param>
        public HostedClosureManager(global::NWN.Core.ICoreFunctionHandler host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        uint global::NWN.Core.ICoreFunctionHandler.ObjectSelf => ObjectSelf;

        void global::NWN.Core.ICoreFunctionHandler.ClosureAssignCommand(uint obj, Action func)
        {
            _host.ClosureAssignCommand(obj, () => RunClosure(func));
        }

        void global::NWN.Core.ICoreFunctionHandler.ClosureDelayCommand(uint obj, float duration, Action func)
        {
            _host.ClosureDelayCommand(obj, duration, () => RunClosure(func));
        }

        void global::NWN.Core.ICoreFunctionHandler.ClosureActionDoCommand(uint obj, Action func)
        {
            _host.ClosureActionDoCommand(obj, () => RunClosure(func));
        }

        /// <summary>
        /// Runs one closure the engine called back. The host has made the callback's object its own <c>OBJECT_SELF</c>
        /// for the call; it becomes the game's for the closure, as <c>ClosureManager.OnClosure</c> does with the object
        /// NWNX passes.
        /// </summary>
        internal void RunClosure(Action func)
        {
            var entered = ManagedEntryDepth.Enter();
            try
            {
                var old = ObjectSelf;
                ObjectSelf = _host.ObjectSelf;
                var profile = FrameProfiler.EnterManaged();

                try
                {
                    func.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, ex.ToMessageAndCompleteStacktrace());
                }
                finally
                {
                    FrameProfiler.ExitManaged(FrameMetric.Closure, profile);
                }

                ObjectSelf = old;
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"Closure processing exception: {e}", true);
            }
            finally
            {
                ManagedEntryDepth.Exit(entered);
            }
        }
    }
}
