using System;
using System.Runtime.InteropServices;

namespace SWLOR.Game.Server.NWN
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector
    {
        public float X;
        public float Y;
        public float Z;

        public Vector(float x = 0.0f, float y = 0.0f, float z = 0.0f)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    public class Effect
    {
        public IntPtr Handle;

        public Effect(IntPtr handle)
        {
            Handle = handle;
        }

        ~Effect()
        {
            Internal.NativeFunctions.FreeEffect(Handle);
        }
    }

    public class Event
    {
        public IntPtr Handle;

        public Event(IntPtr handle)
        {
            Handle = handle;
        }

        ~Event()
        {
            Internal.NativeFunctions.FreeEvent(Handle);
        }
    }

    public class Location
    {
        public IntPtr Handle;

        public Location(IntPtr handle)
        {
            Handle = handle;
        }

        ~Location()
        {
            Internal.NativeFunctions.FreeLocation(Handle);
        }
    }

    public class Talent
    {
        public IntPtr Handle;

        public Talent(IntPtr handle)
        {
            Handle = handle;
        }

        ~Talent()
        {
            Internal.NativeFunctions.FreeTalent(Handle);
        }
    }

    public class ItemProperty
    {
        public IntPtr Handle;

        public ItemProperty(IntPtr handle)
        {
            Handle = handle;
        }

        ~ItemProperty()
        {
            // todo: We're getting a double free on this for some reason. This is an awful hack but it's causing crashes.
            // todo: I'll take a temporary memory leak over a crash at this point.
            //Internal.NativeFunctions.FreeItemProperty(Handle);
        }
    }

    public delegate void ActionDelegate();
}