using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Splits one rig's owned banks without changing animation keys, registration, or native models.</summary>
public static class AnimationBankRebalance
{
    private static readonly Regex Blocks = new(@"(?ms)^newanim (\S+) (\S+)\r?\n.*?^doneanim \1 \2\r?\n");

    public static AnimationInstallPlan Prepare(string repositoryRoot, string targetName, int clipsPerBank = AnimationInstall.ClipsPerBank)
        => Prepare(repositoryRoot, targetName, AnimationInstall.MaximumBankBytes, clipsPerBank);

    internal static AnimationInstallPlan Prepare(string repositoryRoot, string targetName, int bankBudget, int clipsPerBank)
    {
        if (bankBudget < 1 || bankBudget > AnimationInstall.MaximumBankBytes)
            throw new ArgumentOutOfRangeException(nameof(bankBudget));
        if (clipsPerBank < 1 || clipsPerBank > AnimationInstall.ClipsPerBank)
            throw new ArgumentOutOfRangeException(nameof(clipsPerBank));
        AnimationProject.ValidateToken(targetName, 16);
        var root = Path.GetFullPath(repositoryRoot);
        var hakRoot = Path.Combine(root, "SWLOR_Haks");
        var configPath = Path.Combine(root, "Build", "hakbuilder.json");
        var inputs = new Dictionary<string, byte[]>(AnimationInstall.PathComparer);
        var absent = new HashSet<string>(AnimationInstall.PathComparer);
        var totalInput = 0;
        byte[] Read(string path)
        {
            if (inputs.TryGetValue(path, out var cached)) return cached;
            var bytes = AnimationSourceFile.ReadBytes(path,
                Math.Min(AnimationProject.MaximumFileBytes, AnimationInstall.MaximumInputBytes - totalInput), "Bank rebalance input");
            totalInput += bytes.Length;
            inputs.Add(path, bytes);
            return bytes;
        }
        using var config = JsonDocument.Parse(AnimationSourceFile.Utf8Content(Read(configPath)));
        var layers = config.RootElement.GetProperty("HakList").EnumerateArray()
            .Select(layer => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(configPath)!, layer.GetProperty("Path").GetString()!)))
            .Distinct(AnimationInstall.PathComparer).ToArray();
        var indexes = layers.ToDictionary(layer => layer, AnimationInstall.IndexModels, AnimationInstall.PathComparer);
        string? Resolve(string name, bool reserve = true)
        {
            foreach (var layer in layers)
            {
                if (indexes[layer].TryGetValue(name, out var found)) return found;
                if (reserve) absent.Add(Path.Combine(layer, name.ToLowerInvariant() + ".mdl"));
            }
            return null;
        }
        var target = Resolve(targetName) ?? throw new FileNotFoundException("Target rig was not found: " + targetName);
        if (!Path.GetFullPath(target).StartsWith(hakRoot + Path.DirectorySeparatorChar,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException("The rig must be a loose model within SWLOR_Haks.");
        var relativeTarget = Path.GetRelativePath(root, target).Replace('\\', '/');
        var registry = JsonSerializer.Deserialize<AnimationRegistration[]>(AnimationSourceFile.Utf8Content(Read(Path.Combine(root, "design", "animations", "registry.json"))).Span)
            ?? throw new InvalidDataException("Missing animation registry.");
        if (registry.Any(entry => entry == null || string.IsNullOrEmpty(entry.Name) || entry.Targets == null ||
                string.IsNullOrEmpty(entry.AnimationName) || !Regex.IsMatch(entry.AnimationName, @"\Asw_[a-z0-9_]{1,9}\z")) ||
            registry.Select(entry => entry.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != registry.Length ||
            registry.Select(entry => entry.AnimationName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != registry.Length)
            throw new InvalidDataException("Invalid or duplicate animation registrations.");
        var names = registry.Where(entry => entry.Targets.Contains(relativeTarget, AnimationInstall.PathComparer))
            .Select(entry => entry.AnimationName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0) throw new InvalidDataException("The rig has no registered animations.");
        var chain = new List<(string Path, MdlModel Model)>();
        var visited = new HashSet<string>(AnimationInstall.PathComparer);
        var path = target;
        while (true)
        {
            if (!visited.Add(path) || visited.Count > AnimationInstall.MaximumModelChainDepth)
                throw new InvalidDataException("Invalid or excessively deep model chain.");
            var model = new MdlReader().Parse(Read(path));
            chain.Add((path, model));
            if (string.IsNullOrEmpty(model.SuperModel) || model.SuperModel.Equals("NULL", StringComparison.OrdinalIgnoreCase)) break;
            path = Resolve(model.SuperModel) ?? throw new InvalidDataException("Missing supermodel: " + model.SuperModel);
        }
        var changes = new List<AnimationFileChange>();
        var outputBytes = 0;
        var newBankNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenClips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (bankPath, model) in chain.Skip(1))
        {
            // A female rig can inherit male banks. Discovery already has their compiled
            // metadata; do not retain their large editable companions for this rig's transaction.
            var shortTarget = targetName[..Math.Min(8, targetName.Length)];
            if (!model.Name.Equals("an_" + targetName, StringComparison.OrdinalIgnoreCase) &&
                !model.Name.StartsWith("an_" + shortTarget + "_", StringComparison.OrdinalIgnoreCase) &&
                !model.Name.StartsWith("ab_" + shortTarget + "_", StringComparison.OrdinalIgnoreCase)) continue;
            var compiled = Read(bankPath);
            var sourcePath = AnimationBankSource.PathFor(hakRoot, bankPath);
            if (AnimationBankSource.IsBinary(compiled) && !File.Exists(sourcePath))
            {
                absent.Add(sourcePath);
                continue;
            }
            var source = AnimationBankSource.IsBinary(compiled) ? AnimationBankSource.Decode(Read(sourcePath), compiled) : compiled;
            var text = Encoding.UTF8.GetString(AnimationSourceFile.Utf8Content(source).Span);
            if (!text.StartsWith($"# SWLOR authored animations for {targetName}\n", StringComparison.Ordinal) &&
                !text.StartsWith($"# SWLOR authored animations for {targetName}\r\n", StringComparison.Ordinal)) continue;
            if (!Path.GetFullPath(bankPath).StartsWith(hakRoot + Path.DirectorySeparatorChar,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new InvalidDataException("Owned bank lies outside SWLOR_Haks.");
            var matches = Blocks.Matches(text).Cast<Match>().ToArray();
            if (matches.Length == 0 || matches.Length != model.Animations.Count ||
                matches.Any(match => !match.Groups[2].Value.Equals(model.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Malformed animation blocks in " + bankPath);
            var blocks = matches.ToDictionary(match => match.Groups[1].Value, match => match.Value, StringComparer.OrdinalIgnoreCase);
            var triples = new List<string>();
            foreach (var match in matches)
            {
                var name = match.Groups[1].Value;
                if (!names.Contains(name)) continue;
                if (!seenClips.Add(name) || !blocks.TryGetValue(name + "_in", out var start) || !blocks.TryGetValue(name + "_out", out var end))
                    throw new InvalidDataException("Missing or duplicate registered animation phases: " + name);
                triples.Add(blocks[name] + start + end);
            }
            if (triples.Count * 3 != blocks.Count) throw new InvalidDataException("Owned bank contains unregistered or orphaned animation phases.");
            if (Encoding.UTF8.GetByteCount(text) <= bankBudget && triples.Count <= clipsPerBank) continue;
            var prefix = text[..matches[0].Index];
            var groups = new List<List<string>> { new() };
            var size = Encoding.UTF8.GetByteCount(prefix) + 128;
            foreach (var triple in triples)
            {
                var bytes = Encoding.UTF8.GetByteCount(triple);
                // New resource names can be longer. Reserve room for every model-root token expansion.
                var overhead = Regex.Matches(triple, $@"(?<![A-Za-z0-9_]){Regex.Escape(model.Name)}(?![A-Za-z0-9_])").Count * 16;
                if (bytes + overhead + Encoding.UTF8.GetByteCount(prefix) + 128 > bankBudget)
                    throw new InvalidDataException("A complete animation triplet exceeds the bank limit.");
                if (groups[^1].Count > 0 && (groups[^1].Count >= clipsPerBank || size + bytes + overhead > bankBudget))
                {
                    groups.Add(new());
                    size = Encoding.UTF8.GetByteCount(prefix) + 128;
                }
                groups[^1].Add(triple);
                size += bytes + overhead;
            }
            var bankNames = new List<string> { model.Name };
            for (var i = 1; i < groups.Count; i++)
            {
                if (chain.Count + newBankNames.Count >= AnimationInstall.MaximumModelChainDepth)
                    throw new InvalidDataException("Rebalancing would exceed the supported chain depth.");
                string candidate;
                var number = 1;
                do
                {
                    if (number > 999) throw new InvalidDataException("No free bank names remain.");
                    candidate = "ab_" + targetName[..Math.Min(8, targetName.Length)] + "_" + (number++).ToString("D3");
                } while (Resolve(candidate, false) != null || newBankNames.Contains(candidate));
                newBankNames.Add(candidate);
                Resolve(candidate); // Reserve every mounted layer against a shadowing concurrent creation.
                bankNames.Add(candidate);
            }
            for (var i = 0; i < groups.Count; i++)
            {
                var name = bankNames[i];
                var super = i + 1 < groups.Count ? bankNames[i + 1] : string.IsNullOrEmpty(model.SuperModel) ? "NULL" : model.SuperModel;
                var output = prefix + string.Concat(groups[i]) + "donemodel " + model.Name + "\n";
                // Rename the model root only in MDL declarations/references, never inside
                // animation names, comments, numeric key payloads, or unrelated resource names.
                output = Regex.Replace(output,
                    $@"(?m)^([ \t]*(?:(?:newmodel|beginmodelgeom|endmodelgeom|donemodel|parent|animroot)[ \t]+|(?:node|newanim|doneanim)[ \t]+\S+[ \t]+)){Regex.Escape(model.Name)}(?=[ \t]*\r?$)",
                    match => match.Groups[1].Value + name);
                output = Regex.Replace(output, @"(?m)^setsupermodel\s+\S+\s+\S+\r?$", "setsupermodel " + name + " " + super);
                output = AnimationInstall.SortAnimationBlocks(output);
                var bytes = Encoding.UTF8.GetBytes(output);
                if (bytes.Length > bankBudget || bytes.Length > AnimationInstall.MaximumOutputBytes - outputBytes)
                    throw new InvalidDataException("Rebalanced output exceeds its bounded bank or transaction budget.");
                outputBytes += bytes.Length;
                var parsed = new MdlReader().Parse(bytes);
                if ((string.IsNullOrEmpty(parsed.SuperModel) ? "NULL" : parsed.SuperModel) != super || parsed.Animations.Count != groups[i].Count * 3)
                    throw new InvalidDataException("Rebalanced bank failed validation.");
                changes.Add(new(i == 0 ? bankPath : Path.Combine(Path.GetDirectoryName(bankPath)!, name + ".mdl"), i == 0 ? compiled : null, bytes));
            }
        }
        if (!seenClips.SetEquals(names)) throw new InvalidDataException("Registered animations are missing from the owned bank chain.");
        return new AnimationInstallPlan
        {
            AnimationName = "rebalance", ConstantName = "Rebalance", Inputs = inputs,
            AbsentInputs = absent.ToArray(), Changes = changes
        };
    }
}
