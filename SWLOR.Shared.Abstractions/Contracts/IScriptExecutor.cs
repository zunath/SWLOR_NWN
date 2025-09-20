namespace SWLOR.Shared.Abstractions.Contracts;

public interface IScriptExecutor
{
    void Initialize();
    int ProcessRunScript(string scriptName, uint objectSelf);
    void ExecuteInScriptContext(Action action, uint objectId = OBJECT_INVALID, int scriptEventId = 0);
    T ExecuteInScriptContext<T>(Func<T> action, uint objectId = OBJECT_INVALID, int scriptEventId = 0);
}