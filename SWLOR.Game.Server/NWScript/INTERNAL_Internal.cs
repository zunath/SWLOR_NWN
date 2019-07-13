using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace NWN
{
    public class Internal
    {
        public static NWGameObject OBJECT_SELF { get; private set; } = NWGameObject.OBJECT_INVALID;

        private static Stack<NWGameObject> s_ScriptContexts = new Stack<NWGameObject>();

        private static void PushScriptContext(uint oid)
        {
            s_ScriptContexts.Push(oid);
            OBJECT_SELF = oid;
        }

        private static void PopScriptContext()
        {
            s_ScriptContexts.Pop();
            OBJECT_SELF = s_ScriptContexts.Count == 0 ? NWGameObject.OBJECT_INVALID : s_ScriptContexts.Peek();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void CallBuiltIn(int id);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushInteger(int value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushFloat(float value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushString(string value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushObject_Native(uint value);

        public static void StackPushObject(NWGameObject value, bool defAsObjSelf)
        {
            if (value == null)
            {
                value = defAsObjSelf ? OBJECT_SELF : NWGameObject.OBJECT_INVALID;
            }

            StackPushObject_Native(value.m_ObjId);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushVector_Native(Vector value);

        public static void StackPushVector(Vector? value)
        {
            if (!value.HasValue)
            {
                value = new Vector(0.0f, 0.0f, 0.0f);
            }

            StackPushVector_Native(value.Value);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushEffect_Native(IntPtr value);

        public static void StackPushEffect(Effect value)
        {
            StackPushEffect_Native(value.m_Handle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushEvent_Native(IntPtr value);

        public static void StackPushEvent(Event value)
        {
            StackPushEvent_Native(value.m_Handle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushLocation_Native(IntPtr value);

        public static void StackPushLocation(Location value)
        {
            StackPushLocation_Native(value.m_Handle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushTalent_Native(IntPtr value);

        public static void StackPushTalent(Talent value)
        {
            StackPushTalent_Native(value.m_Handle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void StackPushItemProperty_Native(IntPtr value);

        public static void StackPushItemProperty(ItemProperty value)
        {
            StackPushItemProperty_Native(value.m_Handle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static int StackPopInteger();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static float StackPopFloat();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static string StackPopString();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static uint StackPopObject_Native();

        public static NWGameObject StackPopObject()
        {
            return StackPopObject_Native();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static Vector StackPopVector();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static IntPtr StackPopEffect_Native();

        public static Effect StackPopEffect()
        {
            return new Effect { m_Handle = StackPopEffect_Native() };
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static IntPtr StackPopEvent_Native();

        public static Event StackPopEvent()
        {
            return new Event { m_Handle = StackPopEvent_Native() };
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static IntPtr StackPopLocation_Native();

        public static Location StackPopLocation()
        {
            return new Location { m_Handle = StackPopLocation_Native() };
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static IntPtr StackPopTalent_Native();

        public static Talent StackPopTalent()
        {
            return new Talent { m_Handle = StackPopTalent_Native() };
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static IntPtr StackPopItemProperty_Native();

        public static ItemProperty StackPopItemProperty()
        {
            return new ItemProperty { m_Handle = StackPopItemProperty_Native() };
        }

        private struct Closure
        {
            public NWGameObject m_Object;
            public ActionDelegate m_Func;
        }

        private static ulong m_NextEventId = 0;
        private static Dictionary<ulong, Closure> m_Closures = new Dictionary<ulong, Closure>();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void BeginClosure(uint oid);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static int ClosureAssignCommand_Native(uint oid, ulong eventId);

        public static void ClosureAssignCommand(NWGameObject obj, ActionDelegate func)
        {
            if (ClosureAssignCommand_Native(obj.m_ObjId, m_NextEventId) != 0)
            {
                m_Closures.Add(m_NextEventId++, new Closure { m_Object = obj, m_Func = func });
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static int ClosureDelayCommand_Native(uint oid, float duration, ulong eventId);

        public static void ClosureDelayCommand(NWGameObject obj, float duration, ActionDelegate func)
        {
            if (ClosureDelayCommand_Native(obj.m_ObjId, duration, m_NextEventId) != 0)
            {
                m_Closures.Add(m_NextEventId++, new Closure { m_Object = obj, m_Func = func });
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static int ClosureActionDoCommand_Native(uint oid, ulong eventId);

        public static void ClosureActionDoCommand(NWGameObject obj, ActionDelegate func)
        {
            if (ClosureActionDoCommand_Native(obj.m_ObjId, m_NextEventId) != 0)
            {
                m_Closures.Add(m_NextEventId++, new Closure { m_Object = obj, m_Func = func });
            }
        }

        private static void ExecuteClosure(ulong eventId)
        {
            Closure closure = m_Closures[eventId];
            OBJECT_SELF = closure.m_Object;
            BeginClosure(closure.m_Object.m_ObjId);
            closure.m_Func();
            m_Closures.Remove(eventId);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void FreeEffect(IntPtr ptr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void FreeEvent(IntPtr ptr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void FreeLocation(IntPtr ptr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void FreeTalent(IntPtr ptr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void FreeItemProperty(IntPtr ptr);
    }
}
