using System;
using System.Runtime.InteropServices;
using NWN;

namespace SWLOR.Game.Server.NWScript
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