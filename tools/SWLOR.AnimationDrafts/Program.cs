using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

if (args.Length >= 4 && args[0] == "install")
{
    // Run against an isolated checkout with the toolset closed, just like other source generators.
    var root = Path.GetFullPath(args[1]);
    var project = AnimationProject.Deserialize(File.ReadAllText(args[2]));
    var targets = args.Skip(3).Select(name => AnimationInstall.FindTargetSource(root, name)
        ?? throw new FileNotFoundException($"No configured HAK source for {name}.")).ToArray();
    var plan = AnimationInstall.Prepare(root, project, targets);
    foreach (var change in plan.Changes) Console.WriteLine(Path.GetRelativePath(root, change.Path));
    plan.Apply();
    Console.WriteLine(plan.CodeExample);
    return 0;
}

if (args.Length == 2 && args[0] == "inspect")
{
    var source = new MdlReader().Parse(File.ReadAllBytes(args[1]));
    foreach (var mesh in MdlMeshBuilder.Build(source).Meshes)
    {
        var vertices = Enumerable.Range(0, mesh.Positions.Length / 3).Select(i => Vector3.Transform(
            new Vector3(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]), mesh.Transform)).ToArray();
        Console.WriteLine($"{mesh.NodeName}: min {vertices.Aggregate(Vector3.Min)}, max {vertices.Aggregate(Vector3.Max)}");
    }
    return 0;
}

if (args.Length is 4 or 5 && args[0] == "render-data" && (args.Length == 4 || args[4] == "--frames"))
{
    var source = new MdlReader().Parse(File.ReadAllBytes(args[1]));
    using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(args[2], "manifest.json")));
    var poses = new List<object>();
    foreach (var entry in manifest.RootElement.GetProperty("Animations").EnumerateArray())
    {
        var id = entry.GetProperty("Id").GetString()!;
        AnimationProject.ValidateToken(id, 63);
        var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(args[2], id + ".swlanim")));
        var snapshots = new List<object>();
        var beats = entry.GetProperty("Beats").EnumerateArray().ToArray();
        var times = args.Length == 5
            ? Enumerable.Range(0, (int)Math.Ceiling(project.Duration * 20) + 1).Select(i => Math.Min(i / 20f, project.Duration))
            : beats.Select(b => b.GetProperty("Time").GetSingle());
        foreach (var time in times)
        {
            var beat = beats.Last(b => b.GetProperty("Time").GetSingle() <= time);
            var pose = project.Sample(time);
            var named = project.Joints.Select((j, i) => (j.Name, Pose: pose[i])).ToDictionary(j => j.Name, j => j.Pose);
            var meshes = MdlMeshBuilder.Build(source, [named]).Meshes.Select(mesh => new
            {
                mesh.NodeName, mesh.Indices,
                Vertices = Enumerable.Range(0, mesh.Positions.Length / 3).Select(i =>
                {
                    var p = Vector3.Transform(new Vector3(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]), mesh.Transform);
                    return new[] { p.X, p.Y, p.Z };
                }).ToArray()
            });
            var world = AnimationRig.World(project.Joints, pose);
            float[] Point(string joint, Vector3 local)
            {
                var p = Vector3.Transform(local, world[project.Joints.FindIndex(j => j.Name == joint)]);
                return [p.X, p.Y, p.Z];
            }
            snapshots.Add(new { Time = time, Label = beat.GetProperty("Label").GetString(), Meshes = meshes.ToArray(),
                Hand = Point("rhand_g", Vector3.Zero), Tip = Point("rhand_g", new Vector3(0, .8f, 0)),
                Shield = Enumerable.Range(0, 8).Select(i => Point("lhand_g", new Vector3(-.09f,
                    .29f * MathF.Cos(i * MathF.PI / 4), .49f * MathF.Sin(i * MathF.PI / 4)))).ToArray() });
        }
        poses.Add(new { Id = id, Name = entry.GetProperty("Name").GetString(), project.Duration, Snapshots = snapshots });
    }
    File.WriteAllText(args[3], JsonSerializer.Serialize(poses));
    return 0;
}

if (args.Length < 4 || args[0] != "generate" || args.Skip(4).Any(a => a != "--overwrite"))
{
    Console.Error.WriteLine("Usage: SWLOR.AnimationDrafts generate <a_ba.mdl> <recipe.json> <output-folder> [--overwrite]");
    return 1;
}

try
{
    var modelBytes = File.ReadAllBytes(args[1]);
    var model = new MdlReader().Parse(modelBytes);
    var rig = AnimationProject.FromModel(model);
    var recipeText = File.ReadAllText(args[2]).Replace("\r\n", "\n");
    var recipe = JsonSerializer.Deserialize<Recipe>(recipeText, Recipe.Json) ?? throw new InvalidDataException("Empty recipe.");
    if (model.Name != recipe.Model) throw new InvalidDataException($"Recipe requires {recipe.Model}, received {model.Name}.");
    var output = Path.GetFullPath(args[3]);
    var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var previews = new List<object>();
    var reports = new List<object>();
    foreach (var motion in recipe.Motions)
    {
        AnimationProject.ValidateToken(motion.Id, 63);
        if (files.ContainsKey(motion.Id + ".swlanim")) throw new InvalidDataException("Duplicate motion ID.");
        var project = MotionAuthor.Bake(rig, recipe, motion);
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
        previews.Add(PreviewWriter.Motion(project, motion));
        reports.Add(new
        {
            motion.Id, motion.Name, motion.BibleRow, motion.Reference, motion.Observation, motion.Interpretation,
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
    files.Add("preview.html", PreviewWriter.Html(previews));
    if (!args.Contains("--overwrite"))
        foreach (var file in files.Keys)
            if (File.Exists(Path.Combine(output, file))) throw new IOException($"Already exists: {file}. Choose another folder or use --overwrite.");
    Directory.CreateDirectory(output);
    foreach (var (file, contents) in files) File.WriteAllText(Path.Combine(output, file), contents);
    Console.WriteLine($"Wrote {recipe.Motions.Length} editable drafts and preview to {output}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
