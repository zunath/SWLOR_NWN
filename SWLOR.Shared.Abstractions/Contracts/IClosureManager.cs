namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface IClosureManager
    {
        uint ObjectSelf { get; set; }
        void OnClosure(ulong eid, uint oidSelf);
    }
}