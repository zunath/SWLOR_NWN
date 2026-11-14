namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// The one canonical body a static wrapper gets after the rewrite. A wrapper whose body equals it is already moved; any
/// other body is original code that still has to be moved behind the interface.
/// </summary>
internal static class ForwardingBody
{
    public static string Text(WrapperMember member, SeamDefinition definition)
    {
        if (member.Kind == MemberKind.Property)
        {
            return $"{definition.RuntimeName}.Current.{member.Name}";
        }

        var prefix = member.ReturnsValue ? "return " : string.Empty;
        return $"{{\n{member.Indent}    {prefix}{definition.RuntimeName}.Current.{member.Name}({member.ArgumentList});\n{member.Indent}}}";
    }

    public static bool IsForwarder(WrapperMember member, SeamDefinition definition) => member.BodyText == Text(member, definition);
}
