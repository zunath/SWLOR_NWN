using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Core.Engine
{
    public class Location : EngineStructureBase
    {
        public Location(nint handle) : base(handle) { }
        public override int StructureId => (int)EngineStructureType.Location;
        public static implicit operator Location(nint intPtr) => new(intPtr);
    }
}
