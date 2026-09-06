using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Numerics;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Toolset.Domain.Animation;

public sealed record AnimationFileChange(string Path, byte[]? Before, byte[] After);
public sealed record AnimationRegistration(string Name, string AnimationName, float Duration, string[] Targets);

/// <summary>A reviewable installation transaction. Existing model payloads are retained byte for byte.</summary>
public sealed class AnimationInstallPlan
{
    public required string AnimationName { get; init; }
    public required string ConstantName { get; init; }
    public required IReadOnlyList<AnimationFileChange> Changes { get; init; }
    public required IReadOnlyDictionary<string, byte[]> Inputs { get; init; }
    public IReadOnlyCollection<string> AbsentInputs { get; init; } = [];
    public string CodeExample => $"NamedAnimation.Queue(creature, AuthoredAnimation.{ConstantName});";

    public void Apply()
    {
        // The caller holds the module mutation lock. Every input is checked again after confirmation.
        VerifyInputs();
        foreach (var change in Changes) Verify(change);
        var staged = new Dictionary<string, string>();
        var applied = new List<AnimationFileChange>();
        try
        {
            foreach (var change in Changes)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(change.Path)!);
                var temporary = change.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                staged.Add(change.Path, temporary);
                File.WriteAllBytes(temporary, change.After);
            }
            VerifyInputs();
            foreach (var change in Changes)
            {
                Verify(change);
                File.Move(staged[change.Path], change.Path, overwrite: true);
                applied.Add(change);
            }
        }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };
            foreach (var change in applied.AsEnumerable().Reverse())
                try
                {
                    // Preserve another writer's work if it changed a file during rollback.
                    if (!File.ReadAllBytes(change.Path).AsSpan().SequenceEqual(change.After))
                        throw new IOException($"Concurrent change prevented rollback of '{change.Path}'.");
                    if (change.Before == null) File.Delete(change.Path);
                    else File.WriteAllBytes(change.Path, change.Before);
                }
                catch (Exception rollback) { errors.Add(rollback); }
            if (errors.Count > 1) throw new AggregateException("Installation failed; some files need recovery from the preview's original contents.", errors);
            throw;
        }
        finally
        {
            foreach (var file in staged.Values) if (File.Exists(file)) File.Delete(file);
        }
    }

    private void VerifyInputs()
    {
        foreach (var path in AbsentInputs)
            if (File.Exists(path))
                throw new IOException($"'{path}' was created after the installation preview. Prepare a new preview.");
        foreach (var input in Inputs)
            if (!File.Exists(input.Key) || !File.ReadAllBytes(input.Key).AsSpan().SequenceEqual(input.Value))
                throw new IOException($"'{input.Key}' changed after the installation preview. Prepare a new preview.");
    }

    private static void Verify(AnimationFileChange change)
    {
        if (change.Before == null ? File.Exists(change.Path) :
            !File.Exists(change.Path) || !File.ReadAllBytes(change.Path).AsSpan().SequenceEqual(change.Before))
            throw new IOException($"'{change.Path}' changed after the installation preview. Prepare a new preview.");
    }
}

public static class AnimationInstall
{
    /// <summary>Resolves a mounted rig back to its winning loose repository source, never to its HAK archive.</summary>
    public static string? FindTargetSource(string repositoryRoot, string resref)
    {
        AnimationProject.ValidateToken(resref, 16);
        var root = Path.GetFullPath(repositoryRoot);
        var configPath = Path.Combine(root, "Build", "hakbuilder.json");
        if (!File.Exists(configPath)) return null;
        using var config = JsonDocument.Parse(File.ReadAllBytes(configPath));
        foreach (var layer in ReadLayers(configPath, config.RootElement))
        {
            var path = Path.Combine(layer, resref + ".mdl");
            if (File.Exists(path))
                return path.StartsWith(Path.Combine(root, "SWLOR_Haks") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? path : null;
        }
        return null;
    }

    private static string[] ReadLayers(string configPath, JsonElement config) => config.GetProperty("HakList").EnumerateArray().Select(layer =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(configPath)!, layer.GetProperty("Path").GetString()!))).ToArray();

    public static AnimationInstallPlan Prepare(string repositoryRoot, AnimationProject project, IEnumerable<string> targetPaths)
    {
        project.Validate();
        if (project.Name == "AuthoredAnimation" || !Regex.IsMatch(project.Name, @"\A[A-Z][A-Za-z0-9_]*\z"))
            throw new InvalidDataException("Use a C# constant name beginning with an uppercase letter, such as SaluteWithSaber.");
        var root = Path.GetFullPath(repositoryRoot);
        var hakRoot = Path.Combine(root, "SWLOR_Haks");
        var configPath = Path.Combine(root, "Build", "hakbuilder.json");
        var registryPath = Path.Combine(root, "design", "animations", "registry.json");
        var constantsPath = Path.Combine(root, "SWLOR.Game.Server", "Service", "AnimationService", "AuthoredAnimation.cs");
        var inputs = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var absentInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        byte[] Read(string path)
        {
            if (!inputs.TryGetValue(path, out var data)) inputs[path] = data = File.ReadAllBytes(path);
            return data;
        }
        using var config = JsonDocument.Parse(Read(configPath));
        var layers = ReadLayers(configPath, config.RootElement);
        // Resolve by the same first-HAK-wins order used by the resource index and pack pipeline.
        string? Resolve(string name)
        {
            foreach (var layer in layers)
            {
                var path = Path.Combine(layer, name + ".mdl");
                if (File.Exists(path)) return path;
                absentInputs.Add(path);
            }
            return null;
        }
        var registrations = File.Exists(registryPath)
            ? JsonSerializer.Deserialize<List<AnimationRegistration>>(Read(registryPath)) ?? throw new InvalidDataException("Invalid animation registry.")
            : [];
        if (registrations.Any(r => r == null || string.IsNullOrEmpty(r.Name) || r.Name == "AuthoredAnimation" || !Regex.IsMatch(r.Name, @"\A[A-Z][A-Za-z0-9_]*\z") ||
                r.AnimationName == null || r.AnimationName.Length > AnimationClip.MaxNameLength || !Regex.IsMatch(r.AnimationName, @"\Asw_[a-z0-9_]+\z") || r.Targets == null || r.Targets.Length == 0 ||
                !float.IsFinite(r.Duration) || r.Duration <= 0 || r.Duration > 600) ||
            registrations.Select(r => r.AnimationName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != registrations.Count ||
            registrations.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != registrations.Count)
            throw new InvalidDataException("Invalid or conflicting animation registry entries.");
        var registration = registrations.SingleOrDefault(r => r.Name.Equals(project.Name, StringComparison.OrdinalIgnoreCase));
        if (registration != null && registration.Name != project.Name)
            throw new InvalidDataException("That animation name already exists with different capitalization. Use its registered C# name.");
        var stem = "sw_" + project.Name.ToLowerInvariant(); stem = stem[..Math.Min(stem.Length, AnimationClip.MaxNameLength)];
        var animationName = registration?.AnimationName ?? stem;
        var usedNames = registrations.Select(r => r.AnimationName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var suffix = 1; registration == null && usedNames.Contains(animationName); suffix++)
        {
            var number = suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            animationName = stem[..Math.Min(stem.Length, AnimationClip.MaxNameLength - number.Length)] + number;
        }
        var targets = targetPaths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (targets.Length is < 1 or > 32) throw new InvalidDataException("Select 1–32 target model files.");
        var models = new Dictionary<string, MdlModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            if (!target.StartsWith(hakRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetExtension(target).Equals(".mdl", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Resolve(Path.GetFileNameWithoutExtension(target)), target, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Targets must be winning model files in the configured SWLOR HAK source directories.");
            var currentPath = target;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (currentPath != null)
            {
                if (!visited.Add(currentPath) || visited.Count > 32) throw new InvalidDataException("Cyclic or excessively deep supermodel chain.");
                var model = new MdlReader().Parse(Read(currentPath)); models[currentPath] = model;
                foreach (var animation in model.Animations)
                {
                    if ((animation.Name.Equals(animationName, StringComparison.OrdinalIgnoreCase) ||
                         animation.Name.Equals(animationName + "_in", StringComparison.OrdinalIgnoreCase) ||
                         animation.Name.Equals(animationName + "_out", StringComparison.OrdinalIgnoreCase)) && registration == null)
                        throw new InvalidDataException($"Animation name '{animation.Name}' already exists in the target's supermodel chain.");
                }
                if (string.IsNullOrWhiteSpace(model.SuperModel) || model.SuperModel.Equals("NULL", StringComparison.OrdinalIgnoreCase)) break;
                AnimationProject.ValidateToken(model.SuperModel, 16);
                currentPath = Resolve(model.SuperModel) ?? throw new InvalidDataException($"Supermodel '{model.SuperModel}' is not in the HAK sources. Include its source before installing.");
            }
            var rig = AnimationProject.FromModel(models[target]);
            var rigNames = rig.Joints.ToDictionary(j => j.Name, StringComparer.OrdinalIgnoreCase);
            // Model geometry roots are named after the model; those are remapped during overlay generation.
            if (project.Joints.Where(j => j.Parent >= 0).Any(j => !rigNames.ContainsKey(j.Name)))
                throw new InvalidDataException($"Target '{models[target].Name}' does not contain every joint in this animation.");
            foreach (var joint in project.Joints.Where(j => j.Parent >= 0))
            {
                var targetJoint = rigNames[joint.Name];
                var expectedParent = joint.Parent == 0 ? rig.Joints[0].Name : project.Joints[joint.Parent].Name;
                if (targetJoint.Parent < 0 || !rig.Joints[targetJoint.Parent].Name.Equals(expectedParent, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Target '{models[target].Name}' has a different hierarchy at '{joint.Name}'.");
            }
        }
        var targetNames = targets.Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).ToArray();
        if (registration != null && !registration.Targets.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(targetNames))
            throw new InvalidDataException("An installed animation must be updated for all of its original targets. Use the target paths in the registry.");
        var changes = new List<AnimationFileChange>();
        void Add(string path, byte[] bytes) => changes.Add(new(path, File.Exists(path) ? Read(path) : null, bytes));
        foreach (var target in targets)
        {
            var model = models[target];
            var overlayName = "an_" + Path.GetFileNameWithoutExtension(target);
            AnimationProject.ValidateToken(overlayName, 16);
            var overlayPath = Path.Combine(Path.GetDirectoryName(target)!, overlayName + ".mdl");
            var existingOverlay = Resolve(overlayName);
            var relativeTarget = Path.GetRelativePath(root, target).Replace('\\', '/');
            if (existingOverlay != null && (!string.Equals(existingOverlay, overlayPath, StringComparison.OrdinalIgnoreCase) ||
                model.SuperModel != overlayName || !registrations.Any(r => r.Targets.Contains(relativeTarget, StringComparer.OrdinalIgnoreCase))))
                throw new InvalidDataException($"Overlay '{overlayName}' exists but is not owned by the animation registry for this target.");
            var overlayProject = project.Clone(); overlayProject.ModelName = overlayName;
            var targetRig = AnimationProject.FromModel(model);
            if (!float.IsFinite(model.Scale) || model.Scale <= 0) throw new InvalidDataException("Target model has an invalid animation scale.");
            // Match target proportions and compensate for NWN applying the target's animation scale
            // to inherited translations. An unkeyed rest pose must stay at the target's own bind pose.
            if (overlayProject.Keys.Count == 0) overlayProject.SetKey(0, project.Sample(0));
            for (var jointIndex = 0; jointIndex < overlayProject.Joints.Count; jointIndex++)
            {
                var sourceJoint = project.Joints[jointIndex];
                var targetJoint = sourceJoint.Parent < 0 ? targetRig.Joints[0] : targetRig.Joints.Single(j => j.Name.Equals(sourceJoint.Name, StringComparison.OrdinalIgnoreCase));
                foreach (var key in overlayProject.Keys)
                {
                    var value = key.Pose[jointIndex];
                    key.Pose[jointIndex] = value with
                    {
                        Position = (targetJoint.Rest.Position + value.Position - sourceJoint.Rest.Position) / model.Scale,
                        Orientation = Quaternion.Normalize(value.Orientation * Quaternion.Inverse(sourceJoint.Rest.Orientation) * targetJoint.Rest.Orientation),
                        Scale = targetJoint.Rest.Scale * value.Scale / sourceJoint.Rest.Scale
                    };
                }
            }
            for (var i = 0; i < overlayProject.Joints.Count; i++)
                if (overlayProject.Joints[i].Parent < 0)
                {
                    if (overlayProject.AnimationRoot.Equals(overlayProject.Joints[i].Name, StringComparison.OrdinalIgnoreCase)) overlayProject.AnimationRoot = overlayName;
                    overlayProject.Joints[i] = overlayProject.Joints[i] with { Name = overlayName };
                }
            var super = string.IsNullOrWhiteSpace(model.SuperModel) ? "NULL" : model.SuperModel;
            var blocks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [animationName] = AnimationMdl.Export(overlayProject, animationName, overlayName)
            };
            foreach (var (suffix, time) in new[] { ("_in", 0f), ("_out", project.Duration) })
            {
                var phase = overlayProject.Clone(); var pose = phase.Sample(time);
                phase.Keys.Clear(); phase.Events.Clear(); phase.Duration = .001f; phase.Transition = 0;
                phase.SetKey(0, pose);
                blocks[animationName + suffix] = AnimationMdl.Export(phase, animationName + suffix, overlayName);
            }
            var block = string.Concat(blocks.Values);
            targetRig.ModelName = overlayName;
            if (targetRig.AnimationRoot.Equals(targetRig.Joints[0].Name, StringComparison.OrdinalIgnoreCase)) targetRig.AnimationRoot = overlayName;
            targetRig.Joints = targetRig.Joints.Select((joint, i) => joint with
            {
                Name = i == 0 ? overlayName : joint.Name,
                Rest = joint.Rest with { Position = joint.Rest.Position / model.Scale }
            }).ToList();
            var overlay = $"# SWLOR authored animations for {model.Name}\nnewmodel {overlayName}\nsetsupermodel {overlayName} {super}\nclassification character\nsetanimationscale 1\n" +
                AnimationMdl.ExportGeometry(targetRig) +
                block + $"donemodel {overlayName}\n";
            if (existingOverlay != null)
            {
                overlay = Encoding.ASCII.GetString(Read(existingOverlay));
                var header = $"# SWLOR authored animations for {model.Name}";
                if (!overlay.StartsWith(header + "\n", StringComparison.Ordinal) && !overlay.StartsWith(header + "\r\n", StringComparison.Ordinal))
                    throw new InvalidDataException("Existing overlay is not an authored animation source.");
                if (registration != null)
                {
                    foreach (var phase in blocks)
                    {
                        var escaped = Regex.Escape(phase.Key);
                        var blockPattern = new Regex($@"(?ms)^newanim {escaped} [^\r\n]+\r?\n.*?^doneanim {escaped} [^\r\n]+\r?\n");
                        if (blockPattern.Matches(overlay).Count != 1) throw new InvalidDataException("Installed animation block is missing or duplicated.");
                        overlay = blockPattern.Replace(overlay, _ => phase.Value);
                    }
                }
                else
                {
                    var terminator = new Regex(@"(?m)^donemodel\s+[^\r\n]+\r?$");
                    if (terminator.Matches(overlay).Count != 1) throw new InvalidDataException("Invalid overlay terminator.");
                    overlay = terminator.Replace(overlay, match => block + match.Value);
                }
            }
            var overlayBytes = Encoding.ASCII.GetBytes(overlay);
            _ = new MdlReader().Parse(overlayBytes);
            Add(overlayPath, overlayBytes);
            if (existingOverlay == null)
            {
                var patched = PatchSupermodel(Read(target), model.Name, overlayName);
                if (new MdlReader().Parse(patched).SuperModel != overlayName) throw new InvalidDataException("Supermodel patch failed validation.");
                Add(target, patched);
            }
        }
        if (registration != null) registrations.Remove(registration);
        registrations.Add(new(project.Name, animationName, project.Duration, targetNames));
        Add(registryPath, JsonSerializer.SerializeToUtf8Bytes(registrations, new JsonSerializerOptions { WriteIndented = true }));
        var generated = "// Generated by SWLOR's Animation Editor from design/animations/registry.json.\n" +
            "namespace SWLOR.Game.Server.Service.AnimationService;\n\npublic static class AuthoredAnimation\n{\n" +
            string.Join("\n", registrations.OrderBy(r => r.Name, StringComparer.Ordinal).Select(r =>
                $"    public static readonly global::SWLOR.Game.Server.Service.AnimationService.AnimationClip {r.Name} = new(\"{r.AnimationName}\", {AnimationMdl.F(r.Duration)}f);")) + "\n}\n";
        if (File.Exists(constantsPath) && !Encoding.UTF8.GetString(Read(constantsPath)).StartsWith("// Generated by SWLOR's Animation Editor", StringComparison.Ordinal))
            throw new InvalidDataException("The animation constants file is not owned by this editor.");
        Add(constantsPath, Encoding.UTF8.GetBytes(generated));
        Add(Path.Combine(root, "design", "animations", project.Name + ".swlanim"), Encoding.UTF8.GetBytes(project.Serialize()));
        return new() { AnimationName = animationName, ConstantName = project.Name, Changes = changes, Inputs = inputs, AbsentInputs = absentInputs };
    }

    public static byte[] PatchSupermodel(byte[] data, string modelName, string supermodel)
    {
        AnimationProject.ValidateToken(supermodel, 16);
        if (data.Length >= 244 && BitConverter.ToUInt32(data) == 0)
        {
            var patched = (byte[])data.Clone();
            Array.Clear(patched, 180, 64); // 12-byte file header + 168-byte offset in the model header.
            Encoding.ASCII.GetBytes(supermodel).CopyTo(patched, 180);
            return patched;
        }
        // Latin-1 is a byte-preserving bridge. Only the ASCII supermodel token is replaced.
        var text = Encoding.Latin1.GetString(data);
        var pattern = new Regex(@"(?im)^(\s*setsupermodel\s+" + Regex.Escape(modelName) + @"\s+)([^\s#]+)");
        if (pattern.Matches(text).Count != 1) throw new InvalidDataException("ASCII target must declare exactly one setsupermodel line.");
        return Encoding.Latin1.GetBytes(pattern.Replace(text, match => match.Groups[1].Value + supermodel));
    }
}
