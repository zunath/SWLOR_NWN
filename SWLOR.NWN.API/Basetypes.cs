using SWLOR.NWN.API.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API
{
    public partial class Effect
    {
        public IntPtr Handle;
        public Effect(IntPtr handle) => Handle = handle;
        ~Effect() { VM.FreeGameDefinedStructure((int)EngineStructure.Effect, Handle); }

        public static implicit operator IntPtr(Effect effect) => effect.Handle;
        public static implicit operator Effect(IntPtr intPtr) => new Effect(intPtr);
    }

    public partial class Event
    {
        public IntPtr Handle;
        public Event(IntPtr handle) => Handle = handle;
        ~Event() { VM.FreeGameDefinedStructure((int)EngineStructure.Event, Handle); }

        public static implicit operator IntPtr(Event effect) => effect.Handle;
        public static implicit operator Event(IntPtr intPtr) => new Event(intPtr);
    }

    public partial class Location
    {
        public IntPtr Handle;
        public Location(IntPtr handle) => Handle = handle;
        ~Location() { VM.FreeGameDefinedStructure((int)EngineStructure.Location, Handle); }

        public static implicit operator IntPtr(Location effect) => effect.Handle;
        public static implicit operator Location(IntPtr intPtr) => new Location(intPtr);
    }

    public partial class Talent
    {
        public IntPtr Handle;
        public Talent(IntPtr handle) => Handle = handle;
        ~Talent() { VM.FreeGameDefinedStructure((int)EngineStructure.Talent, Handle); }

        public static implicit operator IntPtr(Talent effect) => effect.Handle;
        public static implicit operator Talent(IntPtr intPtr) => new Talent(intPtr);
    }

    public partial class ItemProperty
    {
        public IntPtr Handle;
        public ItemProperty(IntPtr handle) => Handle = handle;
        ~ItemProperty() { VM.FreeGameDefinedStructure((int)EngineStructure.ItemProperty, Handle); }

        public static implicit operator IntPtr(ItemProperty effect) => effect.Handle;
        public static implicit operator ItemProperty(IntPtr intPtr) => new ItemProperty(intPtr);
    }

    public partial class SQLQuery
    {
        public IntPtr Handle;
        public SQLQuery(IntPtr handle) => Handle = handle;
        ~SQLQuery() { VM.FreeGameDefinedStructure((int)EngineStructure.SQLQuery, Handle); }

        public static implicit operator IntPtr(SQLQuery sqlQuery) => sqlQuery.Handle;
        public static implicit operator SQLQuery(IntPtr intPtr) => new SQLQuery(intPtr);
    }

    public partial class Cassowary
    {
        public IntPtr Handle;
        public Cassowary(IntPtr handle) => Handle = handle;
        ~Cassowary() { VM.FreeGameDefinedStructure((int)EngineStructure.Cassowary, Handle); }

        public static implicit operator IntPtr(Cassowary cassowary) => cassowary.Handle;
        public static implicit operator Cassowary(IntPtr intPtr) => new Cassowary(intPtr);
    }

    public partial class Json
    {
        public IntPtr Handle;
        public Json(IntPtr handle) => Handle = handle;
        ~Json() { VM.FreeGameDefinedStructure((int)EngineStructure.Json, Handle); }

        public static implicit operator IntPtr(Json json) => json.Handle;
        public static implicit operator Json(IntPtr intPtr) => new Json(intPtr);
    }

    public delegate void ActionDelegate();
}