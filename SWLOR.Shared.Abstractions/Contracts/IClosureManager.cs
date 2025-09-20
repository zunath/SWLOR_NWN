namespace SWLOR.Shared.Core.Server;

public interface IClosureManager
{
    uint ObjectSelf { get; set; }
    void OnClosure(ulong eid, uint oidSelf);
}