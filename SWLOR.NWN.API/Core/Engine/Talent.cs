using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Core.Engine
{
    public class Talent : EngineStructureBase
    {
        public Talent(nint handle) : base(handle) { }
        public override int StructureId => (int)EngineStructureType.Talent;
        public static implicit operator Talent(nint intPtr) => new(intPtr);
    }
}
