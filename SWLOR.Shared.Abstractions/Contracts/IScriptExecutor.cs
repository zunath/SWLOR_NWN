using System;

namespace SWLOR.Shared.Core.Server;

public interface IScriptExecutor
{
    int ProcessRunScript(string scriptName, uint objectSelf);
    void ExecuteInScriptContext(Action action, uint objectId = OBJECT_INVALID, int scriptEventId = 0);
    T ExecuteInScriptContext<T>(Func<T> action, uint objectId = OBJECT_INVALID, int scriptEventId = 0);
}