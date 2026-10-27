namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Decides, member by member, where the original body comes from. A wrapper that still holds its original body is the
/// source of truth (a first run, or a sync that took the upstream file). A wrapper that already forwards takes its body
/// from the previous pass-through output, so a second run changes nothing.
/// </summary>
internal static class Planner
{
    public static GenerationPlan Plan(
        IReadOnlyList<WrapperSourceFile> sources,
        IReadOnlyDictionary<string, GeneratedMember> existingPassThrough,
        SeamDefinition definition,
        List<string> errors)
    {
        var bodies = new Dictionary<string, string>(StringComparer.Ordinal);
        var moved = new HashSet<string>(StringComparer.Ordinal);
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            foreach (var member in source.Members)
            {
                if (owners.TryGetValue(member.Key, out var firstPath))
                {
                    errors.Add($"{source.Path}: {member.Key} is declared twice (first in {firstPath}).");
                    continue;
                }

                owners[member.Key] = source.Path;

                if (ForwardingBody.IsForwarder(member, definition))
                {
                    if (!existingPassThrough.TryGetValue(member.Key, out var existing) || existing.BodyText == null)
                    {
                        errors.Add($"{source.Path}: {member.Key} already forwards to {definition.RuntimeName}, but the previous " +
                                   $"{definition.PassThroughName} output has no body for it. Restore the wrapper's original body " +
                                   "(take the file from the base branch) and run the generator again.");
                        continue;
                    }

                    bodies[member.Key] = existing.BodyText;
                    continue;
                }

                if (member.BodyText.Contains(definition.RuntimeName, StringComparison.Ordinal))
                {
                    errors.Add($"{source.Path}: {member.Key} mixes original code with {definition.RuntimeName}. Moving it would make " +
                               $"{definition.PassThroughName} call itself. Restore the wrapper's original body or the exact forwarding body.");
                    continue;
                }

                bodies[member.Key] = member.BodyText;
                moved.Add(member.Key);
            }
        }

        var dropped = existingPassThrough.Keys.Where(k => !owners.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();
        return new GenerationPlan
        {
            Sources = sources,
            OriginalBodies = bodies,
            MovedThisRun = moved,
            Dropped = dropped
        };
    }
}
