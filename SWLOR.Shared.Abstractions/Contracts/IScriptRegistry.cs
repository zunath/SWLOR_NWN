using SWLOR.Shared.Abstractions.Delegates;

namespace SWLOR.Shared.Abstractions.Contracts;

public interface IScriptRegistry
{
    void LoadHandlersFromAssembly();
    bool HasScript(string scriptName);
    bool HasConditionalScript(string scriptName);
    IEnumerable<(Action Action, string Name)> GetActionScripts(string scriptName);
    IEnumerable<(ConditionalScriptDelegate Action, string Name)> GetConditionalScripts(string scriptName);
}