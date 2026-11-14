using System.Text.Json;

namespace SWLOR.FacadeRemoval;

/// <summary>
/// Reads <c>tools/Refactor/RemovedFacades.json</c>: the facades the branch deleted, sorted by name, each with the
/// constants it repeated and the classes that hold them now.
/// <code>
/// { "facades": [
///     { "facade": "Taxi" },
///     { "facade": "Combat", "explicit": true, "constants": [ { "name": "MaximumHitRate", "owner": "SWLOR.Game.Server.Domain.Combat.CombatRules" } ],
///       "members": [ { "owner": "CombatQueries", "names": [ "GetMinimumHitRate" ] }, { "owner": "SWLOR.Game.Server.Domain.Combat.CombatRules", "names": [ "CalculateHitRate" ] } ] }
/// ] }
/// </code>
/// </summary>
internal static class RemovedFacadeList
{
    public static IReadOnlyList<RemovedFacade> Load(string path)
    {
        if (!File.Exists(path))
            throw new FacadeRemovalException($"The data file {path} does not exist.");

        return Parse(File.ReadAllText(path), path);
    }

    public static IReadOnlyList<RemovedFacade> Parse(string json, string source)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException exception)
        {
            throw new FacadeRemovalException($"{source} is not valid JSON: {exception.Message}");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("facades", out var facades) ||
                facades.ValueKind != JsonValueKind.Array)
            {
                throw new FacadeRemovalException($"{source} must be an object with a \"facades\" array.");
            }

            var problems = new List<string>();
            var result = new List<RemovedFacade>();
            foreach (var element in facades.EnumerateArray())
            {
                var facade = StringProperty(element, "facade");
                if (facade == null || !IsDottedName(facade))
                {
                    problems.Add($"Every entry needs a \"facade\": a type name relative to {RemovedFacade.ServiceNamespace}, such as Taxi or QuestService.QuestEncounter.");
                    continue;
                }

                var isExplicit = element.TryGetProperty("explicit", out var explicitElement) && explicitElement.ValueKind == JsonValueKind.True;
                if (element.TryGetProperty("explicit", out explicitElement) && explicitElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    problems.Add($"{facade}: \"explicit\" must be true or false.");

                var constants = new List<RemovedConstant>();
                if (element.TryGetProperty("constants", out var constantsElement))
                {
                    if (constantsElement.ValueKind != JsonValueKind.Array)
                    {
                        problems.Add($"{facade}: \"constants\" must be an array.");
                    }
                    else
                    {
                        foreach (var entry in constantsElement.EnumerateArray())
                        {
                            var constantName = StringProperty(entry, "name");
                            var owner = StringProperty(entry, "owner");
                            if (constantName == null || !IsDottedName(constantName) || constantName.Contains('.') || owner == null || !IsDottedName(owner) || !owner.Contains('.'))
                            {
                                problems.Add($"{facade}: every constant needs a plain \"name\" and the full type name of its \"owner\" (SWLOR.Game.Server.Domain.Combat.CombatRules).");
                                continue;
                            }

                            if (constants.Any(known => known.Name == constantName))
                                problems.Add($"{facade}: the constant {constantName} is listed twice.");
                            else
                                constants.Add(new RemovedConstant(constantName, owner));
                        }
                    }
                }

                var members = ReadMembers(facade, element, constants, problems);

                if (result.Any(known => known.Facade == facade))
                {
                    problems.Add($"{facade} is listed twice.");
                    continue;
                }

                if (result.Count > 0 && string.CompareOrdinal(result[^1].Facade, facade) > 0)
                    problems.Add($"{facade} is out of order: keep the list sorted by facade so pull requests that add facades do not conflict.");

                if (result.Any(known => known.Name == facade[(facade.LastIndexOf('.') + 1)..]))
                    problems.Add($"{facade}: another removed facade has the same simple name, so GameServices cannot have a property for each.");

                result.Add(new RemovedFacade(facade, isExplicit, constants, members));
            }

            // A reversal maps Owner.Name back to one facade, so two facades cannot hand the same constant to one owner.
            foreach (var shared in result
                         .SelectMany(facade => facade.Constants.Select(constant => (facade.Facade, constant.Owner, constant.Name)))
                         .GroupBy(entry => (entry.Owner, entry.Name))
                         .Where(group => group.Count() > 1))
            {
                problems.Add($"{shared.Key.Owner}.{shared.Key.Name} is the owner of a constant of more than one facade ({string.Join(", ", shared.Select(entry => entry.Facade))}).");
            }

            if (problems.Count > 0)
                throw new FacadeRemovalException($"{source} has problems:{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", problems));

            return result;
        }
    }

    // The optional "members" of a facade split over several classes: one object per owner, with the owner (a GameServices
    // property or a full class name) and the names of its members. A name under two owners is an overload on each.
    private static IReadOnlyList<RemovedMember>? ReadMembers(string facade, JsonElement element, IReadOnlyList<RemovedConstant> constants, List<string> problems)
    {
        if (!element.TryGetProperty("members", out var membersElement))
            return null;

        if (membersElement.ValueKind != JsonValueKind.Array)
        {
            problems.Add($"{facade}: \"members\" must be an array.");
            return null;
        }

        var members = new List<RemovedMember>();
        foreach (var entry in membersElement.EnumerateArray())
        {
            var owner = StringProperty(entry, "owner");
            if (owner == null || !IsDottedName(owner) || entry.ValueKind != JsonValueKind.Object ||
                !entry.TryGetProperty("names", out var names) || names.ValueKind != JsonValueKind.Array)
            {
                problems.Add($"{facade}: every \"members\" entry needs an \"owner\" (a GameServices property, or the full name of a class) and an array of \"names\".");
                continue;
            }

            foreach (var name in names.EnumerateArray())
            {
                var memberName = name.ValueKind == JsonValueKind.String ? name.GetString() : null;
                if (memberName == null || !IsDottedName(memberName) || memberName.Contains('.'))
                    problems.Add($"{facade}: the members of {owner} must be plain names.");
                else if (constants.Any(constant => constant.Name == memberName))
                    problems.Add($"{facade}: {memberName} is listed as a constant and as a member.");
                else if (members.Any(known => known.Name == memberName && known.Owner == owner))
                    problems.Add($"{facade}: the member {memberName} of {owner} is listed twice.");
                else
                    members.Add(new RemovedMember(memberName, owner));
            }
        }

        return members;
    }

    // A string property of an object, or null when the element is not an object or the property is missing or not a string
    // (GetString would throw instead of letting the problem be reported).
    private static string? StringProperty(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool IsDottedName(string name) =>
        name.Split('.').All(part => part.Length > 0 && (char.IsLetter(part[0]) || part[0] == '_') && part.All(c => char.IsLetterOrDigit(c) || c == '_'));
}
