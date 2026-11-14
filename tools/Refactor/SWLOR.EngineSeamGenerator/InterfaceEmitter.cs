namespace SWLOR.EngineSeamGenerator;

/// <summary>Emits the part of the engine interface that mirrors one wrapper file.</summary>
internal static class InterfaceEmitter
{
    public static string Emit(WrapperSourceFile source, SeamDefinition definition)
    {
        var text = PartWriter.Begin(source, definition.Namespace,
            new[] { $"using static {definition.Namespace}.{definition.StaticClassName};" });
        text.Append('\n');
        text.Append(PartWriter.Indent).Append("/// <summary>Engine calls declared in ").Append(PartName(source)).Append(".</summary>\n");
        text.Append(PartWriter.Indent).Append("public partial interface ").Append(definition.InterfaceName).Append('\n');
        text.Append(PartWriter.Indent).Append("{\n");

        var first = true;
        foreach (var member in source.Members)
        {
            if (!first) text.Append('\n');
            first = false;

            PartWriter.AppendDocs(text, member);
            text.Append(PartWriter.Indent).Append(PartWriter.Indent);
            text.Append(member.Kind == MemberKind.Property ? $"{member.ReturnType} {member.Name} {{ get; }}" : $"{member.Signature};");
            text.Append('\n');
        }

        text.Append(PartWriter.Indent).Append("}\n");
        return PartWriter.End(text);
    }

    /// <summary>The wrapper file's path below its project folder, e.g. `NWScript/ActionFunctions.cs`.</summary>
    public static string PartName(WrapperSourceFile source) => source.Path[(source.Path.IndexOf('/') + 1)..];
}
