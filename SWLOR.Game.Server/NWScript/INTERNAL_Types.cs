using System;

// ReSharper disable once CheckNamespace
namespace NWN
{
    public partial class NWGameObject
    {
        public const uint OBJECT_INVALID = 0x7F000000;
        public static NWGameObject OBJECT_SELF { get { return Internal.OBJECT_SELF; } }

        public uint m_ObjId = OBJECT_INVALID;

        public static implicit operator NWGameObject(uint objId)
        {
            return new NWGameObject { m_ObjId = objId };
        }

        public static bool operator ==(NWGameObject lhs, NWGameObject rhs)
        {
            bool lhsNull = object.ReferenceEquals(lhs, null);
            bool rhsNull = object.ReferenceEquals(rhs, null);
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.m_ObjId == rhs.m_ObjId);
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
            return (int)m_ObjId;
        }
    }

    public struct Vector
    {
        public float m_X;
        public float m_Y;
        public float m_Z;

        public Vector(float x, float y, float z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }
    }

    public class Effect
    {
        public IntPtr m_Handle;
        ~Effect() { Internal.FreeEffect(m_Handle); }
    }

    public class Event
    {
        public IntPtr m_Handle;
        ~Event() { Internal.FreeEvent(m_Handle); }
    }

    public class Location
    {
        public IntPtr m_Handle;
        ~Location() { Internal.FreeLocation(m_Handle); }
    }

    public class Talent
    {
        public IntPtr m_Handle;
        ~Talent() { Internal.FreeTalent(m_Handle); }
    }

    public class ItemProperty
    {
        public IntPtr m_Handle;
        ~ItemProperty() { Internal.FreeItemProperty(m_Handle); }
    }

    public delegate void ActionDelegate();
}
