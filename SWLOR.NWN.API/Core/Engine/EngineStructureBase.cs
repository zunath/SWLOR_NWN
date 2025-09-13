namespace SWLOR.NWN.API.Core.Engine
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
            VM.FreeGameDefinedStructure(StructureId, Handle);
        }

        public static implicit operator nint(EngineStructureBase engineStructure) => engineStructure.Handle;
    }
}
