using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Serilog;
using Serilog.Events;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Game.Server.Service.AbilityService;

using var logger = new LoggerConfiguration().WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose).CreateLogger();
try
{
    if (args.Length >= 4 && args[0] == "install")
    {
        // Run against an isolated checkout with the toolset closed, just like other source generators.
        var root = Path.GetFullPath(args[1]);
        var project = AnimationProject.Deserialize(await ReadText(args[2]));
        var targets = args.Skip(3).Select(name => AnimationInstall.FindTargetSource(root, name)
            ?? throw new FileNotFoundException($"No configured HAK source for {name}.")).ToArray();
        var plan = AnimationInstall.Prepare(root, project, targets, Path.GetFullPath(args[2]));
        foreach (var change in plan.Changes) Console.WriteLine(Path.GetRelativePath(root, change.Path));
        plan.Apply();
        foreach (var backup in plan.RetainedBackups)
            logger.Warning("Backup cleanup failed; retained {BackupPath}. Review this file before removing it manually.",
                Path.GetRelativePath(root, backup));
        Console.WriteLine(plan.CodeExample);
        return 0;
    }

    if (args.Length is 3 or 4 && args[0] == "preview")
    {
        if (args.Length == 4 && args[3] != "--overwrite") throw new ArgumentException("Unknown preview option.");
        var previewRecipe = JsonSerializer.Deserialize<Recipe>(await ReadText(Path.Combine(args[1], "recipe.json")), Recipe.Json)
            ?? throw new InvalidDataException("Empty recipe.");
        var previews = new List<object>();
        foreach (var motion in previewRecipe.Motions)
        {
            AnimationProject.ValidateToken(motion.Id, 63);
            var project = AnimationProject.Deserialize(await ReadText(Path.Combine(args[1], motion.Id + ".swlanim")));
            previews.Add(PreviewWriter.Motion(project, motion));
        }
        var previewOutput = Path.GetFullPath(args[2]);
        var html = PreviewWriter.Html(previews);
        Directory.CreateDirectory(Path.GetDirectoryName(previewOutput)!);
        using var file = new FileStream(previewOutput, args.Contains("--overwrite") ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
        using var writer = new StreamWriter(file);
        await writer.WriteAsync(html);
        Console.WriteLine($"Wrote preview from {previews.Count} saved projects to {previewOutput}");
        return 0;
    }

    if (args.Length == 2 && args[0] == "inspect")
    {
        var source = new MdlReader().Parse(ReadBytes(args[1]));
        foreach (var mesh in MdlMeshBuilder.Build(source).Meshes)
        {
            var vertices = Enumerable.Range(0, mesh.Positions.Length / 3).Select(i => Vector3.Transform(
                new Vector3(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]), mesh.Transform)).ToArray();
            Console.WriteLine($"{mesh.NodeName}: min {vertices.Aggregate(Vector3.Min)}, max {vertices.Aggregate(Vector3.Max)}");
        }
        return 0;
    }

    if (args.Length >= 4 && args[0] == "render-data")
    {
        var source = new MdlReader().Parse(ReadBytes(args[1]));
        var sourceRig = AnimationProject.FromModel(source);
        MdlModel? overlay = null;
        AnimationRegistration[]? registry = null;
        var equipment = new List<(string Bone, MdlModel Model)>();
        for (var i = 4; i < args.Length; i++)
        {
            if (args[i] == "--frames") continue;
            if (args[i] is "--overlay" or "--registry")
            {
                var option = args[i];
                if (++i == args.Length) throw new ArgumentException(option + " requires a path.");
                if (option == "--overlay") overlay = new MdlReader().Parse(ReadBytes(args[i]));
                else registry = JsonSerializer.Deserialize<AnimationRegistration[]>(await ReadText(args[i]));
                continue;
            }
            var part = args[i] switch { "--shield" => "shield", "--sword" => "weaponr", _ => throw new ArgumentException("Unknown render option.") };
            var bone = MdlPartBoneMap.GetBoneName(part)!;
            if (++i == args.Length) throw new ArgumentException("Equipment option requires an MDL path.");
            equipment.Add((bone, new MdlReader().Parse(ReadBytes(args[i]))));
        }
        if ((overlay == null) != (registry == null)) throw new ArgumentException("Installed rendering requires both --overlay and --registry.");
        using var manifest = JsonDocument.Parse(await ReadText(Path.Combine(args[2], "manifest.json")));
        var poses = new List<object>();
        foreach (var entry in manifest.RootElement.GetProperty("Animations").EnumerateArray())
        {
            var id = entry.GetProperty("Id").GetString()!;
            var activity = entry.TryGetProperty("Activity", out var activityValue) ? activityValue.GetString() : null;
            AnimationProject.ValidateToken(id, 63);
            var project = AnimationProject.Deserialize(await ReadText(Path.Combine(args[2], id + ".swlanim")));
            var snapshots = new List<object>();
            var beats = entry.GetProperty("Beats").EnumerateArray().ToArray();
            var times = args.Contains("--frames")
                ? Enumerable.Range(0, (int)Math.Ceiling(project.Duration * 20) + 1).Select(i => Math.Min(i / 20f, project.Duration))
                : beats.Select(b => b.GetProperty("Time").GetSingle());
            foreach (var time in times)
            {
                var beat = beats.Last(b => b.GetProperty("Time").GetSingle() <= time);
                var sampleRig = overlay == null ? project : sourceRig;
                PosedNode[] pose;
                if (overlay == null) pose = project.Sample(time);
                else
                {
                    var registered = registry!.Single(r => r.Name == id);
                    var animation = overlay.Animations.Single(a => a.Name == registered.AnimationName);
                    var sampled = MdlAnimationPose.Sample(animation, time, MdlAnimationPose.BindPose(source));
                    // NWN scales inherited translations for the target model. Render the installed
                    // clip against that model, rather than reusing the authored project's transforms.
                    pose = sampleRig.Joints.Select(j => sampled.TryGetValue(j.Name, out var p)
                        ? p with { Position = p.Position * source.Scale } : j.Rest).ToArray();
                }
                var named = sampleRig.Joints.Select((j, i) => (j.Name, Pose: pose[i])).ToDictionary(j => j.Name, j => j.Pose);
                var world = AnimationRig.World(sampleRig.Joints, pose);
                var allMeshes = MdlMeshBuilder.Build(source, [named]).Meshes.Select(mesh => (Mesh: mesh, Transform: mesh.Transform)).ToList();
                foreach (var item in equipment)
                    foreach (var mesh in MdlMeshBuilder.Build(item.Model).Meshes)
                        allMeshes.Add((mesh, mesh.Transform * world[sampleRig.Joints.FindIndex(j => j.Name == item.Bone)]));
                var meshes = allMeshes.Select(entry => new
                {
                    entry.Mesh.NodeName, entry.Mesh.Indices,
                    Vertices = Enumerable.Range(0, entry.Mesh.Positions.Length / 3).Select(i =>
                    {
                        var v = entry.Mesh.Positions;
                        var p = Vector3.Transform(new Vector3(v[i * 3], v[i * 3 + 1], v[i * 3 + 2]), entry.Transform);
                        return new[] { p.X, p.Y, p.Z };
                    }).ToArray()
                });
                float[] Point(string joint, Vector3 local)
                {
                    var p = Vector3.Transform(local, world[sampleRig.Joints.FindIndex(j => j.Name == joint)]);
                    return [p.X, p.Y, p.Z];
                }
                snapshots.Add(new { Time = time, Label = beat.GetProperty("Label").GetString(), Meshes = meshes.ToArray(),
                    Hand = Point("rhand", Vector3.Zero), Tip = Point("rhand", PreviewWriter.WeaponTipOffset(activity)),
                    Shield = Enumerable.Range(0, 8).Select(i => Point("lforearm", new Vector3(-.09f,
                        .49f * MathF.Sin(i * MathF.PI / 4), .29f * MathF.Cos(i * MathF.PI / 4)))).ToArray(), EquipmentMeshes = equipment.Count > 0 });
            }
            poses.Add(new { Id = id, Name = entry.GetProperty("Name").GetString(), Activity = activity, project.Duration,
                PoseSource = overlay == null ? "Editable project" : "Installed MDL: " + overlay.Name, Snapshots = snapshots });
        }
        var renderOutput = Path.GetFullPath(args[3]);
        Directory.CreateDirectory(Path.GetDirectoryName(renderOutput)!);
        File.WriteAllText(renderOutput, JsonSerializer.Serialize(poses));
        return 0;
    }

    if (args.Length < 4 || args[0] != "generate" || args.Skip(4).Any(a => a != "--overwrite"))
    {
        Console.Error.WriteLine("Usage: SWLOR.AnimationDrafts generate <a_ba.mdl> <recipe.json> <output-folder> [--overwrite]");
        Console.Error.WriteLine("       SWLOR.AnimationDrafts preview <project-folder> <output.html> [--overwrite]");
        Console.Error.WriteLine("       SWLOR.AnimationDrafts install <repository-root> <project.swlanim> <target-model> [target-model ...]");
        Console.Error.WriteLine("       SWLOR.AnimationDrafts inspect <model.mdl>");
        Console.Error.WriteLine("       SWLOR.AnimationDrafts render-data <model.mdl> <project-folder> <output.json> [--frames] [--shield <model.mdl>] [--sword <model.mdl>] [--overlay <model.mdl> --registry <registry.json>]");
        return 1;
    }

    logger.Information("Generating animation drafts from {RecipePath} with model {ModelPath}", args[2], args[1]);
    var modelBytes = ReadBytes(args[1]);
    var model = new MdlReader().Parse(modelBytes);
    var rig = AnimationProject.FromModel(model);
    var idle = model.Animations.Single(a => a.Name == "pause1");
    var sampledIdle = MdlAnimationPose.Sample(idle, 0, MdlAnimationPose.BindPose(model));
    var neutral = rig.Joints.Select(j => sampledIdle.TryGetValue(j.Name, out var p) ? p : j.Rest).ToArray();
    var recipeText = (await ReadText(args[2])).Replace("\r\n", "\n");
    var recipe = JsonSerializer.Deserialize<Recipe>(recipeText, Recipe.Json) ?? throw new InvalidDataException("Empty recipe.");
    if (model.Name != recipe.Model) throw new InvalidDataException($"Recipe requires {recipe.Model}, received {model.Name}.");
    var output = Path.GetFullPath(args[3]);
    var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    files.Add("recipe.json", recipeText);
    var reports = new List<object>();
    foreach (var motion in recipe.Motions)
    {
        var definition = typeof(IAbilityListDefinition).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AbilityDefinition." + motion.AbilityDefinition);
        var activity = motion.Activity != null &&
            System.Enum.TryParse<SWLOR.Game.Server.Service.ActivityService.ActivityStatusType>(motion.Activity, out var activityType) &&
            System.Enum.IsDefined(activityType) && activityType != SWLOR.Game.Server.Service.ActivityService.ActivityStatusType.Invalid;
        if (motion.Activity != null && (!activity || !string.IsNullOrEmpty(motion.AbilityDefinition)))
            throw new InvalidDataException($"{motion.Id}: activity motions require a valid activity and no ability definition.");
        if (!activity && (definition == null || definition.IsAbstract || !typeof(IAbilityListDefinition).IsAssignableFrom(definition)))
            throw new InvalidDataException($"{motion.Id}: no current ability definition matches '{motion.AbilityDefinition}'. Remove outdated Bible entries from the recipe before generating.");
        AnimationProject.ValidateToken(motion.Id, 63);
        if (files.ContainsKey(motion.Id + ".swlanim")) throw new InvalidDataException("Duplicate motion ID.");
        var project = MotionAuthor.Bake(rig, neutral, recipe, motion);
        var serialized = project.Serialize().Replace("\r\n", "\n");
        AnimationProject.Deserialize(serialized);
        // Exercise the same ASCII exchange and native MDL reader used by the editor.
        var mdl = $"newmodel {rig.ModelName}\nsetsupermodel {rig.ModelName} NULL\n" +
                  AnimationMdl.ExportGeometry(rig) + AnimationMdl.Export(project) + $"donemodel {rig.ModelName}\n";
        var reloaded = new MdlReader().Parse(System.Text.Encoding.UTF8.GetBytes(mdl));
        var clip = reloaded.Animations.Single(a => a.Name == project.Name);
        float maximumError = 0;
        foreach (var key in project.Keys)
        {
            var sampled = MdlAnimationPose.Sample(clip, key.Time, MdlAnimationPose.BindPose(reloaded));
            var roundTrip = project.Joints.Select(j => sampled[j.Name]).ToArray();
            var expectedWorld = AnimationRig.World(project.Joints, key.Pose);
            var actualWorld = AnimationRig.World(project.Joints, roundTrip);
            for (var i = 0; i < roundTrip.Length; i++)
                maximumError = Math.Max(maximumError, Vector3.Distance(expectedWorld[i].Translation, actualWorld[i].Translation));
        }
        if (maximumError > .001f) throw new InvalidDataException($"{motion.Id}: MDL round trip moved a joint by {maximumError}m.");
        files.Add(motion.Id + ".swlanim", serialized + "\n");
        reports.Add(new
        {
            motion.Id, motion.Name, motion.BibleRow, motion.Reference, motion.Observation, motion.Interpretation, motion.Activity,
            Status = "Draft: visual review required", Project = motion.Id + ".swlanim", project.Duration,
            Keyframes = project.Keys.Count, Joints = project.Joints.Count, MdlRoundTripMaximumErrorMetres = maximumError,
            Beats = motion.Beats.Select(b => new { b.Time, b.Label }),
            ProjectSha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(serialized + "\n"))).ToLowerInvariant()
        });
        Console.WriteLine($"{motion.Name}: {project.Keys.Count} keys, {project.Duration:0.00}s, native MDL round trip {maximumError:0.000000}m.");
    }
    files.Add("manifest.json", JsonSerializer.Serialize(new
    {
        Version = 1, recipe.Workbook, Sheet = "Animations", Model = model.Name,
        ModelSha256 = Convert.ToHexString(SHA256.HashData(modelBytes)).ToLowerInvariant(),
        RecipeSha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(recipeText))).ToLowerInvariant(),
        Notes = "Image-informed poses authored through Codex and analytic IK. Timings are draft interpretations. No HAKs or gameplay bindings are changed.",
        Animations = reports
    }, Recipe.Json) + "\n");
    if (!args.Contains("--overwrite"))
        foreach (var file in files.Keys)
            if (File.Exists(Path.Combine(output, file))) throw new IOException($"Already exists: {file}. Choose another folder or use --overwrite.");
    Directory.CreateDirectory(output);
    foreach (var (file, contents) in files) File.WriteAllText(Path.Combine(output, file), contents);
    logger.Information("Generated {MotionCount} animation drafts in {OutputDirectory}", recipe.Motions.Length, output);
    Console.WriteLine($"Wrote {recipe.Motions.Length} editable projects and their manifest to {output}. Use the preview command to render saved projects separately.");
    return 0;
}
catch (Exception ex)
{
    logger.Error(ex, "Animation command {Command} failed", args.FirstOrDefault() ?? "(missing)");
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static byte[] ReadBytes(string path) =>
    AnimationSourceFile.ReadBytes(path, AnimationProject.MaximumFileBytes, "Animation source");

static Task<string> ReadText(string path) =>
    AnimationSourceFile.ReadTextAsync(path, AnimationProject.MaximumFileBytes, "Animation source");
