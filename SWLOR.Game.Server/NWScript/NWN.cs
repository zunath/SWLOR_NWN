using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
namespace NWN
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector
    {
        public float m_X;
        public float m_Y;
        public float m_Z;

        public Vector(float x = 0.0f, float y = 0.0f, float z = 0.0f)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Z = z;
        }
    }

    public partial class NWGameObject
    {
        public const uint OBJECT_INVALID = 0x7F000000;

        public static NWGameObject OBJECT_SELF { get { return Internal.OBJECT_SELF; } }

        public uint Self = OBJECT_INVALID;

        public static implicit operator NWGameObject(uint objId)
        {
            return new NWGameObject { Self = objId };
        }

        public static bool operator ==(NWGameObject lhs, NWGameObject rhs)
        {
            bool lhsNull = object.ReferenceEquals(lhs, null);
            bool rhsNull = object.ReferenceEquals(rhs, null);
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Self == rhs.Self);
        }

        public static bool operator !=(NWGameObject lhs, NWGameObject rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            NWGameObject other = o as NWGameObject;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return (int)Self;
        }
    }

    public partial class Effect
    {
        public IntPtr Handle;
        public Effect(IntPtr handle) { Handle = handle; }
        ~Effect() { Internal.NativeFunctions.FreeEffect(Handle); }
    }

    public partial class Event
    {
        public IntPtr Handle;
        public Event(IntPtr handle) { Handle = handle; }
        ~Event() { Internal.NativeFunctions.FreeEvent(Handle); }
    }

    public partial class Location
    {
        public IntPtr Handle;
        public Location(IntPtr handle) { Handle = handle; }
        ~Location() { Internal.NativeFunctions.FreeLocation(Handle); }
    }

    public partial class Talent
    {
        public IntPtr Handle;
        public Talent(IntPtr handle) { Handle = handle; }
        ~Talent() { Internal.NativeFunctions.FreeTalent(Handle); }
    }

    public partial class ItemProperty
    {
        public IntPtr Handle;
        public ItemProperty(IntPtr handle) { Handle = handle; }
        ~ItemProperty() { Internal.NativeFunctions.FreeItemProperty(Handle); }
    }

    public delegate void ActionDelegate();


}
