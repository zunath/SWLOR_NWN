using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Engine
{
    public class Json : EngineStructureBase
    {
        public Json(nint handle) : base(handle) { }
        public override int StructureId => (int)EngineStructureType.Json;
        public static implicit operator Json(nint intPtr) => new(intPtr);
    }
}
