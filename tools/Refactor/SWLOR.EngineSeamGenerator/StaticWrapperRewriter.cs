namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Rewrites the body of every static wrapper to the forwarding body and touches nothing else, so each signature,
/// default value, comment and blank line stays byte for byte as it was.
/// </summary>
internal static class StaticWrapperRewriter
{
    public static string Rewrite(WrapperSourceFile source, SeamDefinition definition)
    {
        var text = source.Text;
        foreach (var member in source.Members.OrderByDescending(m => m.BodyStart))
        {
            if (ForwardingBody.IsForwarder(member, definition)) continue;

            var replacement = ForwardingBody.Text(member, definition).Replace("\n", source.Eol);
            text = string.Concat(text.AsSpan(0, member.BodyStart), replacement, text.AsSpan(member.BodyStart + member.BodyLength));
        }

        return text;
    }
}
