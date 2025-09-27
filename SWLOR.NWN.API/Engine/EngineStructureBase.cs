using NWNX.NET;

namespace SWLOR.NWN.API.Engine
{
    public abstract class EngineStructureBase
    {
        public abstract int StructureId { get; }

        public nint Handle;

        protected EngineStructureBase(nint handle)
        {
            Handle = handle;
        }

        ~EngineStructureBase()
        {
            NWNXAPI.FreeGameDefinedStructure(StructureId, Handle);
        }

        public static implicit operator nint(EngineStructureBase engineStructure) => engineStructure.Handle;
    }
}
