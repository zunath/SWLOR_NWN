namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Emits the part of the recording fake that mirrors one wrapper file. Every member records its name and arguments and
/// returns the configured value, or a default that ends typical engine loops (zero, empty string, an invalid object).
/// </summary>
internal static class FakeEmitter
{
    public static string Emit(WrapperSourceFile source, SeamDefinition definition)
    {
        var text = PartWriter.Begin(source, definition.FakeNamespace, new[]
        {
            $"using {definition.Namespace};",
        });
        text.Append('\n');
        text.Append(PartWriter.Indent).Append("/// <summary>Engine calls declared in ").Append(InterfaceEmitter.PartName(source)).Append(", recorded instead of sent to the engine.</summary>\n");
        text.Append(PartWriter.Indent).Append("public partial class ").Append(definition.FakeName).Append(" : ").Append(definition.FakeBaseName).Append(", ").Append(definition.InterfaceName).Append('\n');
        text.Append(PartWriter.Indent).Append("{\n");

        var memberIndent = PartWriter.Indent + PartWriter.Indent;
        var first = true;
        foreach (var member in source.Members)
        {
            if (!first) text.Append('\n');
            first = false;

            var arguments = member.Parameters.Count == 0
                ? "Array.Empty<object?>()"
                : $"new object?[] {{ {member.ArgumentList} }}";

            if (member.Kind == MemberKind.Property)
            {
                text.Append(memberIndent).Append("public ").Append(member.ReturnType).Append(' ').Append(member.Name)
                    .Append(" => InvokeCall<").Append(member.ReturnType).Append(">(\"").Append(member.Name).Append("\", ").Append(arguments).Append(");\n");
                continue;
            }

            text.Append(memberIndent).Append("public ").Append(member.SignatureWithoutDefaults).Append('\n');
            text.Append(memberIndent).Append("{\n");
            text.Append(memberIndent).Append(PartWriter.Indent);
            if (member.ReturnsValue)
            {
                text.Append("return InvokeCall<").Append(member.ReturnType).Append(">(\"").Append(member.Name).Append("\", ").Append(arguments).Append(");\n");
            }
            else
            {
                text.Append("RecordCall(\"").Append(member.Name).Append("\", ").Append(arguments).Append(");\n");
            }

            text.Append(memberIndent).Append("}\n");
        }

        text.Append(PartWriter.Indent).Append("}\n");
        return PartWriter.End(text);
    }
}
