using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Numerics;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Toolset.Domain.Render;

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

    internal HashSet<string> GetAbsentReservationPaths()
    {
        var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Changes.Count; i++) outputs.Add(Changes[i].Path);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in AbsentInputs)
            if (!outputs.Contains(path) && paths.Add(path) && paths.Count > AnimationInstall.MaximumAbsentReservations)
                throw new InvalidDataException($"Installation has too many missing model dependencies (maximum {AnimationInstall.MaximumAbsentReservations}). Select fewer targets or HAK layers.");
        return paths;
    }

    public void Apply()
    {
        // Bound and snapshot this list before staging files or opening any reservation handles.
        var absentReservations = GetAbsentReservationPaths();
        // The caller holds the module mutation lock. Every input is checked again after confirmation.
        VerifyInputs();
        foreach (var change in Changes) Verify(change);
        var staged = new Dictionary<string, string>();
        var applied = new List<AnimationFileChange>();
        var inputLeases = new List<FileStream>();
        var reservedAbsentInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var change in Changes)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(change.Path)!);
                var temporary = change.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                staged.Add(change.Path, temporary);
                File.WriteAllBytes(temporary, change.After);
            }
            // Outputs are captured and verified at their conditional commit. Keep every other
            // dependency stable through the entire commit/rollback sequence, including config.
            var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Changes.Count; i++) outputs.Add(Changes[i].Path);
            foreach (var input in Inputs)
            {
                if (outputs.Contains(input.Key)) continue;
                var lease = new FileStream(input.Key, FileMode.Open, FileAccess.Read, FileShare.Read);
                inputLeases.Add(lease);
                if (!AnimationSourceFile.Matches(lease, input.Value))
                    throw new IOException($"'{input.Key}' changed after the installation preview. Prepare a new preview.");
            }
            foreach (var path in absentReservations)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                // CreateNew fails if another writer won the race. The exclusive delete-on-close
                // handle reserves this missing resolution path without leaving a file after exit.
                try
                {
                    inputLeases.Add(new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite,
                        FileShare.None, 1, FileOptions.DeleteOnClose));
                }
                catch (IOException exception)
                {
                    throw new IOException($"'{path}' may have been created after the installation preview or cannot be reserved. Prepare a new preview.", exception);
                }
                reservedAbsentInputs.Add(path);
            }
            VerifyInputs(reservedAbsentInputs);
            foreach (var change in Changes)
            {
                AnimationProjectFile.CommitStaged(change.Path, staged[change.Path], change.Before, () => { });
                applied.Add(change);
            }
        }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };
            foreach (var change in applied.AsEnumerable().Reverse())
                try
                {
                    // Rollback uses the same capture/lease/create-only publication as installation.
                    // Never delete or overwrite another writer's replacement at the original path.
                    if (change.Before == null)
                        AnimationProjectFile.CommitStaged(change.Path, null, change.After, () => { });
                    else
                    {
                        File.WriteAllBytes(staged[change.Path], change.Before);
                        AnimationProjectFile.CommitStaged(change.Path, staged[change.Path], change.After, () => { });
                    }
                }
                catch (Exception rollback) { errors.Add(rollback); }
            if (errors.Count > 1) throw new AggregateException("Installation failed; some files need recovery from the preview's original contents.", errors);
            throw;
        }
        finally
        {
            foreach (var lease in inputLeases) lease.Dispose();
            foreach (var file in staged.Values) AnimationProjectFile.DeleteStaged(file);
        }
    }

    private void VerifyInputs(IReadOnlySet<string>? reservedAbsentInputs = null)
    {
        foreach (var path in AbsentInputs)
            if (reservedAbsentInputs?.Contains(path) != true && File.Exists(path))
                throw new IOException($"'{path}' was created after the installation preview. Prepare a new preview.");
        foreach (var input in Inputs)
            if (!File.Exists(input.Key) || !AnimationSourceFile.Matches(input.Key, input.Value))
                throw new IOException($"'{input.Key}' changed after the installation preview. Prepare a new preview.");
    }

    private static void Verify(AnimationFileChange change)
    {
        if (change.Before == null ? File.Exists(change.Path) :
            !File.Exists(change.Path) || !AnimationSourceFile.Matches(change.Path, change.Before))
            throw new IOException($"'{change.Path}' changed after the installation preview. Prepare a new preview.");
    }
}

public static class AnimationInstall
{
    public const int ClipsPerBank = 256;
    public const int MaximumModelChainDepth = 32;
    public const int MaximumInputBytes = 128 * 1024 * 1024;
    public const int MaximumOutputBytes = 128 * 1024 * 1024;
    public const int MaximumAbsentReservations = 4096;
    /// <summary>Resolves a mounted rig back to its winning loose repository source, never to its HAK archive.</summary>
    public static string? FindTargetSource(string repositoryRoot, string resref)
    {
        AnimationProject.ValidateToken(resref, 16);
        var root = Path.GetFullPath(repositoryRoot);
        var configPath = Path.Combine(root, "Build", "hakbuilder.json");
        if (!File.Exists(configPath)) return null;
        using var config = JsonDocument.Parse(AnimationSourceFile.ReadBytes(configPath, AnimationProject.MaximumFileBytes, "HAK configuration"));
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
        => Prepare(repositoryRoot, project, targetPaths, MaximumInputBytes);

    internal static AnimationInstallPlan Prepare(string repositoryRoot, AnimationProject project, IEnumerable<string> targetPaths, int inputBudget,
        int outputBudget = MaximumOutputBytes)
    {
        if (inputBudget < 1 || inputBudget > MaximumInputBytes) throw new ArgumentOutOfRangeException(nameof(inputBudget));
        if (outputBudget < 1 || outputBudget > MaximumOutputBytes) throw new ArgumentOutOfRangeException(nameof(outputBudget));
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
        var inputBytes = 0;
        byte[] Read(string path)
        {
            if (!inputs.TryGetValue(path, out var data))
            {
                data = AnimationSourceFile.ReadBytes(path, Math.Min(AnimationProject.MaximumFileBytes, inputBudget - inputBytes),
                    "Animation installation input (remaining aggregate budget)");
                inputBytes += data.Length;
                inputs[path] = data;
            }
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
        var usedNames = registrations.SelectMany(r => new[] { r.AnimationName, r.AnimationName + "_in", r.AnimationName + "_out" }).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var suffix = 1; registration == null && (usedNames.Contains(animationName) || usedNames.Contains(animationName + "_in") || usedNames.Contains(animationName + "_out")); suffix++)
        {
            var number = suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            animationName = stem[..Math.Min(stem.Length, AnimationClip.MaxNameLength - number.Length)] + number;
        }
        var targets = targetPaths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (targets.Length is < 1 or > 32) throw new InvalidDataException("Select 1–32 target model files.");
        var models = new Dictionary<string, MdlModel>(StringComparer.OrdinalIgnoreCase);
        var chains = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            if (!target.StartsWith(hakRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetExtension(target).Equals(".mdl", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Resolve(Path.GetFileNameWithoutExtension(target)), target, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Targets must be winning model files in the configured SWLOR HAK source directories.");
            if (Read(target).AsSpan().StartsWith("# SWLOR authored animations for "u8))
                throw new InvalidDataException("Generated animation banks cannot be installation targets. Select the original character model.");
            var currentPath = target;
            var chain = chains[target] = [];
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (currentPath != null)
            {
                if (!visited.Add(currentPath) || visited.Count > MaximumModelChainDepth) throw new InvalidDataException("Cyclic or excessively deep supermodel chain.");
                var model = new MdlReader().Parse(Read(currentPath)); models[currentPath] = model;
                chain.Add(currentPath);
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
        var plannedModels = new Dictionary<string, MdlModel>(StringComparer.OrdinalIgnoreCase);
        var outputBytes = 0;
        void EnsureOutputCapacity(int bytes)
        {
            if (bytes > outputBudget - outputBytes)
                throw new InvalidDataException("Generated animation outputs exceed the aggregate installation budget. Select fewer targets for a new registration.");
        }
        void Add(string path, byte[] bytes)
        {
            EnsureOutputCapacity(bytes.Length);
            outputBytes += bytes.Length;
            changes.Add(new(path, File.Exists(path) ? Read(path) : null, bytes));
        }
        foreach (var target in targets)
        {
            var model = models[target];
            var relativeTarget = Path.GetRelativePath(root, target).Replace('\\', '/');
            var registeredTarget = registrations.Any(r => r.Targets.Contains(relativeTarget, StringComparer.OrdinalIgnoreCase));
            var header = $"# SWLOR authored animations for {model.Name}";
            bool IsOwnedBank(string path)
            {
                var contents = Encoding.ASCII.GetString(Read(path));
                return contents.StartsWith(header + "\n", StringComparison.Ordinal) || contents.StartsWith(header + "\r\n", StringComparison.Ordinal);
            }
            var targetName = Path.GetFileNameWithoutExtension(target);
            AnimationProject.ValidateToken(targetName, 16);
            var overlayName = "an_" + targetName;
            if (overlayName.Length > 16)
            {
                if (registeredTarget)
                {
                    if (chains[target].Count < 2 || !IsOwnedBank(chains[target][1]))
                        throw new InvalidDataException("Registered target is missing its authored animation bank.");
                    overlayName = model.SuperModel;
                }
                else
                {
                    // Leave room for a readable collision suffix even for a full-length target resref.
                    var prefix = "an_" + targetName[..8] + "_";
                    var number = 1;
                    do
                    {
                        if (number > 9999) throw new InvalidDataException("No free animation bank names remain for this target.");
                        overlayName = prefix + (number++).ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
                    } while (Resolve(overlayName) != null || plannedModels.Values.Any(m => m.Name.Equals(overlayName, StringComparison.OrdinalIgnoreCase)));
                }
            }
            AnimationProject.ValidateToken(overlayName, 16);
            var overlayPath = Path.Combine(Path.GetDirectoryName(target)!, overlayName + ".mdl");
            var existingOverlay = Resolve(overlayName);
            if (existingOverlay != null && (!string.Equals(existingOverlay, overlayPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(model.SuperModel, overlayName, StringComparison.OrdinalIgnoreCase) || !registeredTarget))
                throw new InvalidDataException($"Overlay '{overlayName}' exists but is not owned by the animation registry for this target.");
            var linkPath = target;
            if (existingOverlay != null)
            {
                if (!IsOwnedBank(existingOverlay)) throw new InvalidDataException("Existing overlay is not an authored animation source.");
                var banks = chains[target].Skip(1).TakeWhile(IsOwnedBank).ToArray();
                var selected = registration != null
                    ? banks.SingleOrDefault(path => models[path].Animations.Any(a => a.Name.Equals(animationName, StringComparison.OrdinalIgnoreCase)))
                    : banks.FirstOrDefault(path => models[path].Animations.Count < ClipsPerBank * 3 &&
                        CountNodes(models[path]) + project.Joints.Count * 3 < 90_000 && Read(path).Length < 32 * 1024 * 1024);
                if (registration != null && selected == null) throw new InvalidDataException("Registered animation is missing from its target's banks.");
                if (selected == null)
                {
                    if (chains[target].Count >= MaximumModelChainDepth) throw new InvalidDataException("Animation banks would exceed the supported supermodel chain depth. Split the library by target rig.");
                    linkPath = existingOverlay;
                    // Extra banks retain a fixed-size resref and are inserted behind the first
                    // overlay. Native model payloads and the existing animation chain stay intact.
                    var prefix = "ab_" + model.Name[..Math.Min(model.Name.Length, 8)] + "_";
                    var number = 1;
                    do
                    {
                        if (number > 999) throw new InvalidDataException("No free animation bank names remain for this target.");
                        overlayName = prefix + (number++).ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
                    } while (Resolve(overlayName) != null || plannedModels.Values.Any(m => m.Name.Equals(overlayName, StringComparison.OrdinalIgnoreCase)));
                    overlayPath = Path.Combine(Path.GetDirectoryName(target)!, overlayName + ".mdl");
                    existingOverlay = null;
                }
                else
                {
                    existingOverlay = overlayPath = selected;
                    overlayName = models[selected].Name;
                }
            }
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
            var previousSuper = models[linkPath].SuperModel;
            var super = string.IsNullOrWhiteSpace(previousSuper) ? "NULL" : previousSuper;
            var blocks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [animationName] = AnimationMdl.Export(overlayProject, animationName, overlayName)
            };
            foreach (var (suffix, time) in new[] { ("_in", 0f), ("_out", project.Duration) })
            {
                var phase = overlayProject.Clone(); var pose = phase.Sample(time);
                phase.Keys.Clear(); phase.Events.Clear(); phase.Duration = .001f; phase.Transition = 0;
                phase.SetKey(0, pose);
                if (suffix == "_out")
                {
                    // Release a looping emote into this target's actual neutral pose. Repeating
                    // the final authored pose here can strand an idle creature in a combat stance.
                    var neutral = MdlAnimationPose.SampleIdle(model, name =>
                        chains[target].Select(path => models[path]).FirstOrDefault(parent =>
                            parent.Name.Equals(name, StringComparison.OrdinalIgnoreCase)), maxDepth: MaximumModelChainDepth);
                    phase.Duration = .2f;
                    phase.SetKey(phase.Duration, overlayProject.Joints.Select((joint, index) =>
                    {
                        var targetJoint = index == 0 ? targetRig.Joints[0] : targetRig.Joints.Single(j => j.Name.Equals(joint.Name, StringComparison.OrdinalIgnoreCase));
                        var rest = neutral.TryGetValue(targetJoint.Name, out var value) ? value : targetJoint.Rest;
                        return rest with { Position = rest.Position / model.Scale };
                    }).ToArray());
                }
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
            EnsureOutputCapacity(Encoding.ASCII.GetByteCount(overlay));
            var overlayBytes = Encoding.ASCII.GetBytes(overlay);
            Add(overlayPath, overlayBytes);
            plannedModels[overlayPath] = new MdlReader().Parse(overlayBytes);
            if (existingOverlay == null)
            {
                var patched = PatchSupermodel(Read(linkPath), models[linkPath].Name, overlayName);
                plannedModels[linkPath] = new MdlReader().Parse(patched);
                if (plannedModels[linkPath].SuperModel != overlayName) throw new InvalidDataException("Supermodel patch failed validation.");
                Add(linkPath, patched);
            }
        }
        // Multiple selected rigs can inherit one another. Validate their combined planned
        // chains, including every new bank, before applying any part of the transaction.
        foreach (var target in targets)
        {
            var path = target; var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                if (!visited.Add(path) || visited.Count > MaximumModelChainDepth) throw new InvalidDataException("Planned animation banks exceed the supported supermodel chain depth or form a cycle.");
                var model = plannedModels.TryGetValue(path, out var planned) ? planned : models[path];
                if (string.IsNullOrWhiteSpace(model.SuperModel) || model.SuperModel.Equals("NULL", StringComparison.OrdinalIgnoreCase)) break;
                path = plannedModels.Keys.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Equals(model.SuperModel, StringComparison.OrdinalIgnoreCase)) ??
                    Resolve(model.SuperModel) ?? throw new InvalidDataException("Planned animation bank is missing from the HAK sources.");
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
        var plan = new AnimationInstallPlan { AnimationName = animationName, ConstantName = project.Name, Changes = changes, Inputs = inputs, AbsentInputs = absentInputs };
        _ = plan.GetAbsentReservationPaths(); // Reject an oversized transaction before presenting its installation preview.
        return plan;
    }

    private static int CountNodes(MdlModel model)
    {
        static int Count(MdlNode? node) => node == null ? 0 : 1 + node.Children.Sum(Count);
        return Count(model.GeometryRoot) + model.Animations.Sum(a => Count(a.GeometryRoot));
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
