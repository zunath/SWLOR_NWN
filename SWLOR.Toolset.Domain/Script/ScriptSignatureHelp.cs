using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>The signature popup's contents for a caret inside a call.</summary>
    /// <param name="Function">The function being called.</param>
    /// <param name="ActiveParameter">0-based index of the argument the caret is in.</param>
    public sealed record SignatureHelp(ScriptFunction Function, int ActiveParameter)
    {
        /// <summary>The active parameter, or null when the caret is past the last one.</summary>
        public ScriptParameter? Active =>
            ActiveParameter >= 0 && ActiveParameter < Function.Parameters.Count
                ? Function.Parameters[ActiveParameter]
                : null;

        /// <summary>"argument 2 of 8", for the popup header.</summary>
        public string PositionLabel => $"argument {ActiveParameter + 1} of {Function.Parameters.Count}";
    }

    /// <summary>Resolves the signature popup for a caret position.</summary>
    public sealed class ScriptSignatureHelpEngine
    {
        private readonly EngineSymbolDatabase _engine;

        public ScriptSignatureHelpEngine(EngineSymbolDatabase engine) => _engine = engine;

        /// <summary>Returns the help for the call the caret sits in, or null when it is not in one.</summary>
        public SignatureHelp? GetSignatureHelp(string source, int caret)
        {
            caret = Math.Clamp(caret, 0, source.Length);

            var tokens = ScriptLexer.TokenizeCode(source);
            var call = ScriptCompletionEngine.FindEnclosingCall(tokens, source, caret);
            if (call == null)
                return null;

            var fn = _engine.FindFunction(call.Value.Name);
            return fn == null ? null : new SignatureHelp(fn, call.Value.Index);
        }
    }
}
