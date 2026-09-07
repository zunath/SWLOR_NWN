using System.Globalization;
using System.Numerics;
using System.Text;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Transform animation exchange; never rewrites the geometry of an imported MDL.</summary>
public static class AnimationMdl
{
    public const int MaximumFileBytes = 64 * 1024 * 1024;

    /// <summary>Bounds the source file before allocating or decoding its animation text.</summary>
    public static async Task<AnimationProject> ImportFileAsync(string path, AnimationProject rig)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > MaximumFileBytes)
            throw new InvalidDataException("Animation MDL exceeds 64 MB.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes);
        if (stream.ReadByte() != -1)
            throw new IOException("The animation MDL changed while reading.");
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Import(await reader.ReadToEndAsync(), rig);
    }

    public static string ExportGeometry(AnimationProject rig)
    {
        rig.Validate();
        var text = new StringBuilder();
        text.AppendLine($"beginmodelgeom {rig.ModelName}");
        foreach (var joint in rig.Joints)
        {
            text.AppendLine($"node dummy {joint.Name}");
            text.AppendLine($"  parent {(joint.Parent < 0 ? "NULL" : rig.Joints[joint.Parent].Name)}");
            text.AppendLine($"  position {V(joint.Rest.Position)}");
            text.AppendLine($"  orientation {AxisAngle(joint.Rest.Orientation)}");
            text.AppendLine($"  scale {F(joint.Rest.Scale)}");
            text.AppendLine("endnode");
        }
        text.AppendLine($"endmodelgeom {rig.ModelName}");
        return text.ToString();
    }

    private static string AxisAngle(Quaternion rotation)
    {
        var q = Quaternion.Normalize(rotation);
        if (q.W < 0) q = -q;
        var sine = new Vector3(q.X, q.Y, q.Z).Length();
        var axis = sine < 1e-6f ? Vector3.UnitZ : new Vector3(q.X, q.Y, q.Z) / sine;
        return $"{V(axis)} {F(sine < 1e-6f ? 0 : 2 * MathF.Atan2(sine, q.W))}";
    }

    public static string Export(AnimationProject project, string? animationName = null, string? modelName = null)
    {
        project.Validate();
        var name = animationName ?? project.Name;
        var model = modelName ?? project.ModelName;
        AnimationProject.ValidateToken(name, 63); AnimationProject.ValidateToken(model, 16);
        var text = new StringBuilder();
        text.AppendLine($"newanim {name} {model}");
        text.AppendLine($"  length {F(project.Duration)}");
        text.AppendLine($"  transtime {F(project.Transition)}");
        text.AppendLine($"  animroot {project.AnimationRoot}");
        foreach (var cue in project.Events.OrderBy(e => e.Time)) text.AppendLine($"  event {F(cue.Time)} {cue.Name}");
        var keys = project.Keys.Count == 0 ? [new AnimationKey(0, project.Sample(0))] : project.Keys.ToArray();
        for (var joint = 0; joint < project.Joints.Count; joint++)
        {
            var bone = project.Joints[joint];
            text.AppendLine($"  node dummy {bone.Name}");
            text.AppendLine($"    parent {(bone.Parent < 0 ? "NULL" : project.Joints[bone.Parent].Name)}");
            // Constant channels need one key, regardless of the clip's frame count. Large
            // libraries otherwise repeat thousands of identical bone offsets and scales.
            var positions = keys.All(k => k.Pose[joint].Position == keys[0].Pose[joint].Position) ? keys[..1] : keys;
            var orientations = keys.All(k => k.Pose[joint].Orientation == keys[0].Pose[joint].Orientation) ? keys[..1] : keys;
            var scales = keys.All(k => k.Pose[joint].Scale == keys[0].Pose[joint].Scale) ? keys[..1] : keys;
            text.AppendLine($"    positionkey {positions.Length}");
            foreach (var key in positions)
                text.AppendLine($"      {F(key.Time)} {V(key.Pose[joint].Position)}");
            text.AppendLine($"    orientationkey {orientations.Length}");
            foreach (var key in orientations)
            {
                // Axis-angle is in radians in Aurora, not Euler angles or degrees.
                text.AppendLine($"      {F(key.Time)} {AxisAngle(key.Pose[joint].Orientation)}");
            }
            text.AppendLine($"    scalekey {scales.Length}");
            foreach (var key in scales) text.AppendLine($"      {F(key.Time)} {F(key.Pose[joint].Scale)}");
            text.AppendLine("  endnode");
        }
        text.AppendLine($"doneanim {name} {model}");
        return text.ToString();
    }

    public static AnimationProject Import(string text, AnimationProject rig)
    {
        if (text.Length > MaximumFileBytes) throw new InvalidDataException("Animation text exceeds 64 MB.");
        rig.Validate();
        var project = rig.Clone();
        project.Keys.Clear(); project.Events.Clear();
        // The shared reader treats static node values as geometry defaults. Convert animation
        // constants into one-key tracks so they override a nonzero rig bind transform as intended.
        var lines = text.Replace("\r", "").Split('\n');
        var start = -1; var end = -1; var node = false; var count = 0; var width = 0; var list = false; var rows = 0;
        float prior = -1;
        var seenNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AnimationJoint? currentJoint = null;
        string? declaredModel = null;
        var times = new SortedSet<float> { 0 };
        foreach (var (line, index) in lines.Select((value, index) => (value, index)))
        {
            var parts = line.Split('#')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            var op = parts[0].ToLowerInvariant();
            if (op == "newanim")
            {
                if (start >= 0) throw new InvalidDataException("Import one newanim/doneanim block at a time.");
                if (parts.Length != 3) throw new InvalidDataException("Invalid animation declaration.");
                start = index; project.Name = parts[1]; declaredModel = parts[2];
                continue;
            }
            if (start < 0 || end >= 0) continue;
            if (count > 0 || list)
            {
                if (list && op == "endlist") { Need(parts, 1); list = false; continue; }
                if (parts.Length != width) throw new InvalidDataException("Invalid transform key width.");
                var values = parts.Select(Number).ToArray();
                if (values[0] < 0 || values[0] <= prior) throw new InvalidDataException("Track times must increase.");
                if (++rows > AnimationProject.MaxKeyframes) throw new InvalidDataException("Invalid transform track: too many keys.");
                prior = values[0]; times.Add(prior); if (!list) count--;
                continue;
            }
            var uniqueDirective = op.EndsWith("key", StringComparison.Ordinal) ? op[..^3] : op;
            if (op is not ("node" or "endnode" or "event" or "doneanim") && !directives.Add(uniqueDirective))
                throw new InvalidDataException($"Duplicate animation directive '{op}'.");
            switch (op)
            {
                case "length": Need(parts, 2); project.Duration = Number(parts[1]); break;
                case "transtime": Need(parts, 2); project.Transition = Number(parts[1]); break;
                case "animroot": Need(parts, 2); project.AnimationRoot = parts[1]; break;
                case "event": Need(parts, 3); project.Events.Add(new(Number(parts[1]), parts[2])); break;
                case "node":
                    Need(parts, 3);
                    if (node || !parts[1].Equals("dummy", StringComparison.OrdinalIgnoreCase) || !seenNodes.Add(parts[2]))
                        throw new InvalidDataException("Only unique dummy transform tracks are supported.");
                    if (!project.Joints.Any(j => j.Name.Equals(parts[2], StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException($"The loaded rig has no node '{parts[2]}'. Load the matching rig first.");
                    currentJoint = project.Joints.Single(j => j.Name.Equals(parts[2], StringComparison.OrdinalIgnoreCase));
                    directives.Clear(); node = true; break;
                case "parent":
                    Need(parts, 2); if (!node) throw new InvalidDataException("Parent outside node.");
                    var expectedParent = currentJoint!.Parent < 0 ? "NULL" : project.Joints[currentJoint.Parent].Name;
                    if (!parts[1].Equals(expectedParent, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Animation joint hierarchy does not match the loaded rig.");
                    break;
                case "position": case "orientation": case "scale":
                    Need(parts, op == "position" ? 4 : op == "orientation" ? 5 : 2);
                    if (!node) throw new InvalidDataException("Transform outside animation node.");
                    foreach (var value in parts.Skip(1)) _ = Number(value);
                    lines[index] = $"{op}key 1\n0 " + string.Join(' ', parts.Skip(1));
                    break;
                case "positionkey": case "orientationkey": case "scalekey":
                    list = parts.Length == 1; count = 0; rows = 0;
                    if (!node || !list && (parts.Length != 2 || !int.TryParse(parts[1], out count) || count < 0 || count > AnimationProject.MaxKeyframes))
                        throw new InvalidDataException("Invalid transform track.");
                    prior = -1; width = op == "positionkey" ? 4 : op == "orientationkey" ? 5 : 2; break;
                case "endnode":
                    Need(parts, 1);
                    if (!node || !directives.Contains("parent")) throw new InvalidDataException("Missing animation node or parent.");
                    node = false; directives.Clear(); break;
                case "doneanim":
                    Need(parts, 3);
                    if (node || !parts[1].Equals(project.Name, StringComparison.OrdinalIgnoreCase) ||
                        !parts[2].Equals(declaredModel, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Mismatched animation terminator.");
                    end = index; break;
                default: throw new InvalidDataException($"Unsupported animation directive '{op}'. Import cancelled to preserve its data.");
            }
        }
        if (start < 0 || end < 0 || count > 0 || list) throw new InvalidDataException("Incomplete animation block.");
        if (project.Duration == 0) project.Duration = 1; // A static pose becomes an editable one-second clip.
        var wrapper = $"newmodel {rig.ModelName}\nbeginmodelgeom {rig.ModelName}\nendmodelgeom {rig.ModelName}\n" +
            string.Join('\n', lines[start..(end + 1)]);
        var animation = new MdlReader().Parse(Encoding.UTF8.GetBytes(wrapper)).Animations.Single();
        var bind = rig.Joints.ToDictionary(j => j.Name, j => new MdlNode
            { Name = j.Name, Position = j.Rest.Position, Orientation = j.Rest.Orientation, Scale = j.Rest.Scale }, StringComparer.OrdinalIgnoreCase);
        times.Add(project.Duration);
        if (times.Count > AnimationProject.MaxKeyframes || (long)times.Count * rig.Joints.Count > 2_000_000)
            throw new InvalidDataException("Animation contains too many sampled transforms.");
        foreach (var time in times)
        {
            var sampled = MdlAnimationPose.Sample(animation, time, bind);
            project.Keys.Add(new(time, rig.Joints.Select(j => sampled.TryGetValue(j.Name, out var pose) ? pose : j.Rest).ToArray()));
        }
        project.Validate();
        return project;
    }

    private static void Need(string[] parts, int count)
    { if (parts.Length != count) throw new InvalidDataException($"Invalid {parts[0]} declaration."); }
    private static float Number(string value) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && float.IsFinite(number)
        ? number : throw new InvalidDataException($"Invalid number '{value}'.");
    internal static string F(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
    internal static string V(Vector3 value) => $"{F(value.X)} {F(value.Y)} {F(value.Z)}";
}
