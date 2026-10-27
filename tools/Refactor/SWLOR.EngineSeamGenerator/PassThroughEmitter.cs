namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Emits the part of the pass-through implementation that mirrors one wrapper file: every original body, moved
/// verbatim into an instance method with the wrapper's signature.
/// </summary>
internal static class PassThroughEmitter
{
    public static string Emit(WrapperSourceFile source, GenerationPlan plan, SeamDefinition definition)
    {
        var text = PartWriter.Begin(source, definition.Namespace,
            new[] { $"using static {definition.Namespace}.{definition.StaticClassName};" });
        text.Append('\n');
        text.Append(PartWriter.Indent).Append("/// <summary>Engine calls declared in ").Append(InterfaceEmitter.PartName(source)).Append(", forwarded to the engine.</summary>\n");
        text.Append(PartWriter.Indent).Append("public sealed partial class ").Append(definition.PassThroughName).Append(" : ").Append(definition.InterfaceName).Append('\n');
        text.Append(PartWriter.Indent).Append("{\n");

        var memberIndent = PartWriter.Indent + PartWriter.Indent;
        var first = true;
        foreach (var member in source.Members)
        {
            if (!first) text.Append('\n');
            first = false;

            var body = plan.OriginalBodies[member.Key];
            if (member.Kind == MemberKind.Property)
            {
                text.Append(memberIndent).Append("public ").Append(member.ReturnType).Append(' ').Append(member.Name).Append(" => ").Append(body).Append(";\n");
            }
            else
            {
                text.Append(memberIndent).Append("public ").Append(member.Signature).Append('\n');
                text.Append(memberIndent).Append(body).Append('\n');
            }
        }

        text.Append(PartWriter.Indent).Append("}\n");
        return PartWriter.End(text);
    }
}
