using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Core.Engine
{
    public class Event : EngineStructureBase
    {
        public Event(nint handle) : base(handle) { }
        public override int StructureId => (int)EngineStructureType.Event;
        public static implicit operator Event(nint intPtr) => new(intPtr);
    }
}
