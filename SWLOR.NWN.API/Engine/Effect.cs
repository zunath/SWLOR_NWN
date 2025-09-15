using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Engine
{
    public class Effect : EngineStructureBase
    {
        public Effect(nint handle) : base(handle) { }
        public override int StructureId => (int)EngineStructureType.Effect;
        public static implicit operator Effect(nint intPtr) => new(intPtr);
    }
}
