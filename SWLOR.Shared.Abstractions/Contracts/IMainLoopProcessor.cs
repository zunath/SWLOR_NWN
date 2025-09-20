namespace SWLOR.Shared.Abstractions.Contracts;

public interface IMainLoopProcessor
{
    event Action OnScriptContextBegin;
    event Action OnScriptContextEnd;
    void ProcessMainLoop(ulong frame);
}