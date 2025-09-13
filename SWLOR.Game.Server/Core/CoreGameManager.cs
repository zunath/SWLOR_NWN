using System;
using System.Collections.Generic;
using SWLOR.NWN.API.Core;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Simple GameManager implementation. Used by default if no manager is specified during bootstrap.
    /// </summary>
    public class CoreGameManager : global::NWN.Core.ICoreFunctionHandler, ICoreEventHandler
    {
        // Hook-able Events
        public delegate void ServerLoopEvent(ulong frame);

        /// <inheritdoc cref="ICoreEventHandler.OnMainLoop"/>
        public event ServerLoopEvent? OnServerLoop;

        public delegate void SignalEvent(string signal);

        /// <inheritdoc cref="ICoreEventHandler.OnSignal"/>
        public event SignalEvent? OnSignal;

        public delegate void RunScriptEvent(string scriptName, uint objectSelf, out int scriptHandleResult);

        /// <inheritdoc cref="ICoreEventHandler.OnRunScript"/>
        public event RunScriptEvent? OnRunScript;

        // Native Management
        private readonly Stack<uint> _scriptContexts = new Stack<uint>();
        private readonly Dictionary<ulong, Action> _closures = new Dictionary<ulong, Action>();
        private ulong _nextEventId;

        private const uint ObjectInvalid = 0x7F000000;
        private uint _objectSelf = ObjectInvalid;

        // NWN.Core interface implementation
        uint global::NWN.Core.ICoreFunctionHandler.ObjectSelf => _objectSelf;

        void ICoreEventHandler.OnMainLoop(ulong frame)
            => OnServerLoop?.Invoke(frame);

        void ICoreEventHandler.OnSignal(string signal)
            => OnSignal?.Invoke(signal);

        int ICoreEventHandler.OnRunScript(string script, uint oidSelf)
        {
            var retVal = -1;
            _objectSelf = oidSelf;
            _scriptContexts.Push(oidSelf);

            try
            {
                OnRunScript?.Invoke(script, oidSelf, out retVal);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _scriptContexts.Pop();
            _objectSelf = _scriptContexts.Count == 0 ? ObjectInvalid : _scriptContexts.Peek();
            return retVal;
        }

        void ICoreEventHandler.OnClosure(ulong eid, uint oidSelf)
        {
            var old = _objectSelf;
            _objectSelf = oidSelf;

            try
            {
                _closures[eid].Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _closures.Remove(eid);
            _objectSelf = old;
        }

        void global::NWN.Core.ICoreFunctionHandler.ClosureAssignCommand(uint obj, Action func)
        {
            if (VM.ClosureAssignCommand(obj, _nextEventId) != 0)
            {
                _closures.Add(_nextEventId++, func);
            }
        }

        void global::NWN.Core.ICoreFunctionHandler.ClosureDelayCommand(uint obj, float duration, Action func)
        {
            if (VM.ClosureDelayCommand(obj, duration, _nextEventId) != 0)
            {
                _closures.Add(_nextEventId++, func);
            }
        }

        void global::NWN.Core.ICoreFunctionHandler.ClosureActionDoCommand(uint obj, Action func)
        {
            if (VM.ClosureActionDoCommand(obj, _nextEventId) != 0)
            {
                _closures.Add(_nextEventId++, func);
            }
        }

        // Public methods for external access
        public uint ObjectSelf
        {
            get => _objectSelf;
            set => _objectSelf = value;
        }

        public void ClosureAssignCommand(uint obj, Action func)
        {
            ((global::NWN.Core.ICoreFunctionHandler)this).ClosureAssignCommand(obj, func);
        }

        public void ClosureDelayCommand(uint obj, float duration, Action func)
        {
            ((global::NWN.Core.ICoreFunctionHandler)this).ClosureDelayCommand(obj, duration, func);
        }

        public void ClosureActionDoCommand(uint obj, Action func)
        {
            ((global::NWN.Core.ICoreFunctionHandler)this).ClosureActionDoCommand(obj, func);
        }
    }
}
