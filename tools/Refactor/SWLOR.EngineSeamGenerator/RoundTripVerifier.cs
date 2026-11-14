namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Reads the generated text back with Roslyn and proves it matches the plan before anything is written: every moved body
/// equals the original body, every signature equals the wrapper's, every rewritten wrapper differs from its input only
/// inside bodies and holds exactly the forwarding body. Any mismatch is an error and nothing is written.
/// </summary>
internal static class RoundTripVerifier
{
    public static void Verify(
        GenerationPlan plan,
        IReadOnlyDictionary<string, string> files,
        SeamDefinition definition,
        List<string> errors)
    {
        foreach (var source in plan.Sources)
        {
            VerifyRewrittenWrapper(source, plan, files[source.Path], definition, errors);

            if (source.Members.Count == 0) continue;

            var interfacePath = definition.PartPath(definition.InterfaceRoot, source.Path);
            var passThroughPath = definition.PartPath(definition.PassThroughRoot, source.Path);
            var fakePath = definition.PartPath(definition.FakeRoot, source.Path);

            VerifySignatures(source, interfacePath, files, definition.InterfaceName, withBodies: false, withDefaults: true, plan, errors);
            VerifySignatures(source, passThroughPath, files, definition.PassThroughName, withBodies: true, withDefaults: true, plan, errors);
            VerifySignatures(source, fakePath, files, definition.FakeName, withBodies: false, withDefaults: false, plan, errors);
        }
    }

    private static void VerifySignatures(
        WrapperSourceFile source,
        string path,
        IReadOnlyDictionary<string, string> files,
        string typeName,
        bool withBodies,
        bool withDefaults,
        GenerationPlan plan,
        List<string> errors)
    {
        if (!files.TryGetValue(path, out var text))
        {
            errors.Add($"{path}: expected output was not produced for {source.Path}.");
            return;
        }

        var read = GeneratedTypeReader.Read(path, text, typeName, errors);
        if (read.Count != source.Members.Count)
        {
            errors.Add($"{path}: {typeName} has {read.Count} members but {source.Path} has {source.Members.Count}.");
            return;
        }

        for (var i = 0; i < read.Count; i++)
        {
            var expected = source.Members[i];
            var actual = read[i];

            if (actual.Key != expected.Key || actual.Kind != expected.Kind)
            {
                errors.Add($"{path}: member {i} is {actual.Key} but the wrapper's is {expected.Key}.");
                continue;
            }

            var expectedSignature = withDefaults ? expected.Signature : expected.SignatureWithoutDefaults;
            if (actual.Signature != expectedSignature)
            {
                errors.Add($"{path}: {expected.Key} has signature `{actual.Signature}` but the wrapper's is `{expectedSignature}`.");
            }

            if (withBodies && actual.BodyText != plan.OriginalBodies[expected.Key])
            {
                errors.Add($"{path}: the moved body of {expected.Key} is not textually identical to the original.");
            }
        }
    }

    private static void VerifyRewrittenWrapper(
        WrapperSourceFile source,
        GenerationPlan plan,
        string rewrittenText,
        SeamDefinition definition,
        List<string> errors)
    {
        var problems = new List<string>();
        var rewritten = WrapperParser.Parse(source.Path, rewrittenText, definition, problems);
        if (problems.Count > 0)
        {
            errors.AddRange(problems.Select(p => "rewritten wrapper: " + p));
            return;
        }

        if (!rewritten.Members.Select(m => m.Key).SequenceEqual(source.Members.Select(m => m.Key)))
        {
            errors.Add($"{source.Path}: the rewritten wrapper has different members than the input.");
            return;
        }

        if (!rewritten.Constants.SequenceEqual(source.Constants) || !rewritten.Usings.SequenceEqual(source.Usings) ||
            !rewritten.NestedTypes.SequenceEqual(source.NestedTypes))
        {
            errors.Add($"{source.Path}: the rewrite changed the constants, nested types or using directives.");
        }

        foreach (var (before, after) in source.Members.Zip(rewritten.Members))
        {
            if (before.Signature != after.Signature || before.Indent != after.Indent || !before.DocLines.SequenceEqual(after.DocLines))
            {
                errors.Add($"{source.Path}: the rewrite changed the signature or documentation of {before.Key}.");
            }

            if (!ForwardingBody.IsForwarder(after, definition))
            {
                errors.Add($"{source.Path}: {after.Key} does not hold the forwarding body after the rewrite.");
            }
        }

        // Nothing outside the bodies may change: blank the bodies in both texts and compare what is left.
        if (MaskBodies(source.Text, source.Members) != MaskBodies(rewrittenText, rewritten.Members))
        {
            errors.Add($"{source.Path}: the rewrite changed text outside the method bodies.");
        }

        // Putting the original bodies back must give the input again (for an input that held original bodies).
        var restored = RestoreBodies(rewrittenText, rewritten.Members, plan);
        var expectedRestored = RestoreBodies(source.Text, source.Members, plan);
        if (WrapperParser.Normalize(restored) != WrapperParser.Normalize(expectedRestored))
        {
            errors.Add($"{source.Path}: restoring the original bodies into the rewritten wrapper does not give back the input.");
        }
    }

    private static string MaskBodies(string text, IReadOnlyList<WrapperMember> members)
    {
        foreach (var member in members.OrderByDescending(m => m.BodyStart))
        {
            text = string.Concat(text.AsSpan(0, member.BodyStart), "<body>", text.AsSpan(member.BodyStart + member.BodyLength));
        }

        return WrapperParser.Normalize(text);
    }

    private static string RestoreBodies(string text, IReadOnlyList<WrapperMember> members, GenerationPlan plan)
    {
        foreach (var member in members.OrderByDescending(m => m.BodyStart))
        {
            text = string.Concat(text.AsSpan(0, member.BodyStart), plan.OriginalBodies[member.Key], text.AsSpan(member.BodyStart + member.BodyLength));
        }

        return text;
    }
}
