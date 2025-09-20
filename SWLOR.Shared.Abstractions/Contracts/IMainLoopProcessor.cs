using System;

namespace SWLOR.Shared.Core.Server;

public interface IMainLoopProcessor
{
    event Action OnScriptContextBegin;
    event Action OnScriptContextEnd;
    void ProcessMainLoop(ulong frame);
}