namespace SWLOR.EngineSeamGenerator;

/// <summary>One parameter of a wrapper, kept as source text so defaults and `params` are reproduced exactly.</summary>
/// <param name="Name">The parameter identifier.</param>
/// <param name="Type">The parameter type as written.</param>
/// <param name="Text">The whole parameter as written: modifiers, type, identifier and default value.</param>
/// <param name="TextWithoutDefault">The parameter without its default value, e.g. `params string[] tags`.</param>
internal sealed record WrapperParameter(string Name, string Type, string Text, string TextWithoutDefault);
