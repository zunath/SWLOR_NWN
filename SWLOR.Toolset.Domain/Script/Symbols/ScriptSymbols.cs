namespace SWLOR.Toolset.Domain.Script.Symbols
{
    /// <summary>One parameter of an engine function.</summary>
    /// <param name="Type">Declared type, e.g. <c>int</c> or <c>object</c>.</param>
    /// <param name="Name">Parameter name, e.g. <c>nFirstCriteriaType</c>.</param>
    /// <param name="DefaultValue">The literal after <c>=</c>, or null when the parameter is required.</param>
    /// <param name="Documentation">The <c>// - name:</c> lines from the declaration's comment block.</param>
    /// <param name="ConstantFamily">
    /// The <c>FOO_*</c> family this parameter accepts, lifted out of its documentation — the thing
    /// that lets completion offer 12 constants in an argument position instead of all 6,201.
    /// Null when the docs name no family.
    /// </param>
    public sealed record ScriptParameter(
        string Type,
        string Name,
        string? DefaultValue,
        string? Documentation,
        string? ConstantFamily)
    {
        public bool IsOptional => DefaultValue != null;

        public string Display => DefaultValue == null ? $"{Type} {Name}" : $"{Type} {Name}={DefaultValue}";
    }

    /// <summary>An engine function declared in nwscript.nss.</summary>
    public sealed record ScriptFunction(
        string Name,
        string ReturnType,
        IReadOnlyList<ScriptParameter> Parameters,
        string? Summary,
        string? ReturnsOnError,
        string Category)
    {
        /// <summary>The full signature, as shown in signature help and the reference browser.</summary>
        public string Signature =>
            $"{ReturnType} {Name}({string.Join(", ", Parameters.Select(p => p.Display))})";

        /// <summary>A call skeleton for insert-at-cursor: required parameters only, as placeholders.</summary>
        public string CallSkeleton
        {
            get
            {
                var required = Parameters.Where(p => !p.IsOptional).Select(p => p.Name);
                return $"{Name}({string.Join(", ", required)})";
            }
        }
    }

    /// <summary>A constant declared in nwscript.nss, e.g. <c>int CREATURE_TYPE_PLAYER_CHAR = 1;</c>.</summary>
    public sealed record ScriptConstant(string Name, string Type, string Value)
    {
        /// <summary>
        /// The <c>FOO_*</c> family this constant belongs to: everything up to and including the last
        /// underscore. <c>CREATURE_TYPE_PLAYER_CHAR</c> belongs to families <c>CREATURE_TYPE_*</c> and
        /// - by prefix - to <c>CREATURE_*</c>, so membership is tested by prefix rather than equality.
        /// </summary>
        public bool IsInFamily(string family)
        {
            var prefix = family.EndsWith('*') ? family[..^1] : family;
            return Name.StartsWith(prefix, StringComparison.Ordinal);
        }
    }
}
