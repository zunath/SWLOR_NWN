using System;
using System.Collections.Generic;
using SWLOR.Game.Server.NWScript;

namespace NWN
{
    partial class Internal
    {
        public const uint OBJECT_INVALID = 0x7F000000;
        public static NWGameObject OBJECT_SELF { get; private set; } = OBJECT_INVALID;
        public static event EventHandler<MainLoopEventArgs> OnMainLoopEvent;


        public static void OnMainLoop(ulong frame)
        {
            try
            {
                NWN.Entrypoints.OnMainLoop(frame);
                OnMainLoopEvent?.Invoke(null, new MainLoopEventArgs(frame));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private struct ScriptContext
        {
            public NWGameObject OwnerObject;
            public string ScriptName;
        }
        private static Stack<ScriptContext> ScriptContexts = new Stack<ScriptContext>();
        public static int OnRunScript(string script, uint oidSelf)
        {
            int ret = 0;
            OBJECT_SELF = oidSelf;
            ScriptContexts.Push(new ScriptContext { OwnerObject = oidSelf, ScriptName = script });
            try
            {
                ret = NWN.Entrypoints.OnRunScript(script, oidSelf);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            ScriptContexts.Pop();
            OBJECT_SELF = ScriptContexts.Count == 0 ? OBJECT_INVALID : ScriptContexts.Peek().OwnerObject;
            return ret;
        }

        private struct Closure
        {
            public NWGameObject OwnerObject;
            public ActionDelegate Run;
        }
        private static ulong NextEventId = 0;
        private static Dictionary<ulong, Closure> Closures = new Dictionary<ulong, Closure>();

        public static void OnClosure(ulong eid, uint oidSelf)
        {
            try
            {
                Closures[eid].Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Closures.Remove(eid);
        }

        public static void ClosureAssignCommand(NWGameObject obj, ActionDelegate func)
        {
            if (NativeFunctions.ClosureAssignCommand(obj.Self, NextEventId) != 0)
            {
                Closures.Add(NextEventId++, new Closure { OwnerObject = obj, Run = func });
            }
        }

        public static void ClosureDelayCommand(NWGameObject obj, float duration, ActionDelegate func)
        {
            if (NativeFunctions.ClosureDelayCommand(obj.Self, duration, NextEventId) != 0)
            {
                Closures.Add(NextEventId++, new Closure { OwnerObject = obj, Run = func });
            }
        }

        public static void ClosureActionDoCommand(NWGameObject obj, ActionDelegate func)
        {
            if (NativeFunctions.ClosureActionDoCommand(obj.Self, NextEventId) != 0)
            {
                Closures.Add(NextEventId++, new Closure { OwnerObject = obj, Run = func });
            }
        }
    }
}
