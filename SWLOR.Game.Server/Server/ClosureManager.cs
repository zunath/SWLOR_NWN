using System;
using System.Collections.Generic;
using NWNX.NET;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Shared.Core.Server
{
    /// <summary>
    /// Simple GameManager implementation. Used by default if no manager is specified during bootstrap.
    /// </summary>
    public class ClosureManager : global::NWN.Core.ICoreFunctionHandler, IClosureManager
    {
        private readonly ILogger _logger;
        public ClosureManager(ILogger logger)
        {
            _logger = logger;
        }

        // Hook-able Events
        public delegate void ServerLoopEvent(ulong frame);

        public delegate void SignalEvent(string signal);

        public delegate void RunScriptEvent(string scriptName, uint objectSelf, out int scriptHandleResult);

        // Native Management
        private readonly Dictionary<ulong, Action> _closures = new Dictionary<ulong, Action>();
        private ulong _nextEventId;

        private const uint ObjectInvalid = 0x7F000000;
        private uint _objectSelf = ObjectInvalid;

        // NWN.Core interface implementation
        uint global::NWN.Core.ICoreFunctionHandler.ObjectSelf => _objectSelf;


        void global::NWN.Core.ICoreFunctionHandler.ClosureAssignCommand(uint obj, Action func)
        {
            if (NWNXAPI.ClosureAssignCommand(obj, _nextEventId) != 0)
            {
                _closures.Add(_nextEventId++, func);
            }
        }

        void global::NWN.Core.ICoreFunctionHandler.ClosureDelayCommand(uint obj, float duration, Action func)
        {
            if (NWNXAPI.ClosureDelayCommand(obj, duration, _nextEventId) != 0)
            {
                _closures.Add(_nextEventId++, func);
            }
        }

        void global::NWN.Core.ICoreFunctionHandler.ClosureActionDoCommand(uint obj, Action func)
        {
            if (NWNXAPI.ClosureActionDoCommand(obj, _nextEventId) != 0)
            {
                _closures.Add(_nextEventId++, func);
            }
        }

        public void OnClosure(ulong eid, uint oidSelf)
        {
            uint old = _objectSelf;
            _objectSelf = oidSelf;

            try
            {
                _closures[eid].Invoke();
            }
            catch (Exception ex)
            {
                _logger.Write<ErrorLogGroup>(ex.ToMessageAndCompleteStacktrace());
            }

            _closures.Remove(eid);
            _objectSelf = old;
        }

        // Public methods for external access
        public uint ObjectSelf
        {
            get => _objectSelf;
            set => _objectSelf = value;
        }
    }
}
