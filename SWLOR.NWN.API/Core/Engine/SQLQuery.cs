using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Core.Engine
{
    public class SQLQuery : EngineStructureBase
    {
        public SQLQuery(nint handle) : base(handle) { }
        public override int StructureId => (int)EngineStructureType.SQLQuery;
        public static implicit operator SQLQuery(nint intPtr) => new(intPtr);
    }
}
