using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Core.Engine
{
    public class ItemProperty : EngineStructureBase
    {
        public ItemProperty(nint handle) : base(handle) { }
        public override int StructureId => (int)EngineStructureType.ItemProperty;
        public static implicit operator ItemProperty(nint intPtr) => new(intPtr);
    }
}
