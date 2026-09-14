using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Domain.Animation;

public sealed record AnimationJoint(string Name, int Parent, PosedNode Rest);
public sealed record AnimationKey(float Time, PosedNode[] Pose);
public sealed record AnimationCue(float Time, string Name);

/// <summary>Portable authoring data. Coordinates are native NWN coordinates (Z up, metres).</summary>
public sealed class AnimationProject
{
    public const int MaxKeyframes = 18001;
    public const int MaximumFileBytes = 64 * 1024 * 1024;
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "NewAnimation";
    public string ModelName { get; set; } = "pmh0";
    public string AnimationRoot { get; set; } = "rootdummy";
    public float Duration { get; set; } = 1;
    public float Transition { get; set; } = 0.25f;
    public List<AnimationJoint> Joints { get; set; } = [];
    public List<AnimationKey> Keys { get; set; } = [];
    public List<AnimationCue> Events { get; set; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true, WriteIndented = true };
    private const int MaximumSerializedCharacters = MaximumFileBytes;
    // A rest pose uses the shortest valid numbers and Boolean. Nested poses add indentation, so
    // this lower bound rejects only outputs that are already certain to exceed the file limit.
    private static readonly int MinimumPoseCharacters = JsonSerializer.Serialize(new PosedNode(Vector3.Zero, Quaternion.Identity, 1), JsonOptions).Length;

    internal static void ValidateSizeBudget(int keyCount, int jointCount)
    {
        if ((long)keyCount * jointCount * MinimumPoseCharacters > MaximumSerializedCharacters)
            throw new InvalidDataException("Animation project exceeds 64 MB. Reduce the bake rate or duration.");
    }

    public string Serialize()
    {
        Validate();
        var text = JsonSerializer.Serialize(this, JsonOptions);
        if (text.Length > MaximumSerializedCharacters) throw new InvalidDataException("Animation project exceeds 64 MB. Reduce the bake rate or duration.");
        return text;
    }
    public static AnimationProject Deserialize(string text)
    {
        if (text.Length > MaximumSerializedCharacters) throw new InvalidDataException("Animation project exceeds 64 MB.");
        // Byte-backed document opens and external reloads retain a UTF-8 BOM as U+FEFF.
        if (text.Length > 0 && text[0] == '\uFEFF') text = text[1..];
        var result = JsonSerializer.Deserialize<AnimationProject>(text, JsonOptions)
            ?? throw new InvalidDataException("Empty animation project.");
        result.Validate();
        return result;
    }

    /// <summary>Reads a project through a bounded file handle before allocating its text representation.</summary>
    public static Task<byte[]> ReadFileBytesAsync(string path) =>
        AnimationSourceFile.ReadBytesAsync(path, MaximumFileBytes, "Animation project");

    public AnimationProject Clone() => Deserialize(Serialize());

    public void Validate()
    {
        if (Version != 1) throw new InvalidDataException("Unsupported animation project version.");
        ValidateToken(Name, 63);
        ValidateToken(ModelName, 16);
        ValidateToken(AnimationRoot, 31);
        if (!float.IsFinite(Duration) || Duration <= 0 || Duration > 600 ||
            !float.IsFinite(Transition) || Transition < 0 || Transition > 600)
            throw new InvalidDataException("Duration must be between 0 and 600 seconds; transition cannot be negative.");
        if (Joints == null || Joints.Count is < 1 or > 512 || Keys == null || Keys.Count > MaxKeyframes ||
            Events == null || Events.Count > 4096 || (long)Keys.Count * Joints.Count > 2_000_000)
            throw new InvalidDataException("Animation exceeds the supported joint/keyframe limits.");
        ValidateSizeBudget(Keys.Count, Joints.Count);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Joints.Count; i++)
        {
            var joint = Joints[i] ?? throw new InvalidDataException("Null joint.");
            ValidateToken(joint.Name, 31);
            if (!names.Add(joint.Name) || joint.Parent < (i == 0 ? -1 : 0) || joint.Parent >= i)
                throw new InvalidDataException("Joint names must be unique, with one root and parents preceding children.");
            ValidatePose(joint.Rest);
        }
        if (!names.Contains(AnimationRoot)) throw new InvalidDataException("Animation root is absent from the rig.");
        float previous = -1;
        foreach (var key in Keys)
        {
            if (key == null || !float.IsFinite(key.Time) || key.Time < 0 || key.Time > Duration ||
                key.Time <= previous || key.Pose == null || key.Pose.Length != Joints.Count)
                throw new InvalidDataException("Keyframes must be ordered, unique, within duration, and cover the rig.");
            foreach (var pose in key.Pose) ValidatePose(pose);
            previous = key.Time;
        }
        foreach (var cue in Events)
        {
            if (cue == null || !float.IsFinite(cue.Time) || cue.Time < 0 || cue.Time > Duration)
                throw new InvalidDataException("Animation event is outside the duration.");
            ValidateToken(cue.Name, 31);
        }
    }

    public static void ValidateToken(string token, int maximum)
    {
        if (string.IsNullOrEmpty(token) || token.Length > maximum || !Regex.IsMatch(token, @"\A[A-Za-z0-9_]+\z"))
            throw new InvalidDataException($"'{token}' must contain 1–{maximum} letters, digits or underscores.");
    }

    public static void ValidatePose(PosedNode pose)
    {
        if (!Finite(pose.Position) || !float.IsFinite(pose.Scale) || pose.Scale <= 0 ||
            !float.IsFinite(pose.Orientation.LengthSquared()) || Math.Abs(pose.Orientation.LengthSquared() - 1) > 0.002f)
            throw new InvalidDataException("Transforms require finite positions, positive scale and unit rotations.");
    }

    public static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    public PosedNode[] Sample(float time)
    {
        if (!float.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
        if (Keys.Count == 0) return Joints.Select(j => j.Rest).ToArray();
        if (time <= Keys[0].Time) return (PosedNode[])Keys[0].Pose.Clone();
        var low = 0; var high = Keys.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (Keys[middle].Time < time) low = middle + 1; else high = middle;
        }
        var right = low;
        if (right == Keys.Count) return (PosedNode[])Keys[^1].Pose.Clone();
        var a = Keys[right - 1]; var b = Keys[right];
        var t = (time - a.Time) / (b.Time - a.Time);
        return a.Pose.Select((pose, i) => new PosedNode(
            Vector3.Lerp(pose.Position, b.Pose[i].Position, t),
            Quaternion.Normalize(Quaternion.Slerp(pose.Orientation, b.Pose[i].Orientation, t)),
            float.Lerp(pose.Scale, b.Pose[i].Scale, t))).ToArray();
    }

    public void SetKey(float time, PosedNode[] pose)
    {
        var index = Keys.FindIndex(key => Math.Abs(key.Time - time) < 0.00001f);
        var key = new AnimationKey(time, (PosedNode[])pose.Clone());
        if (index >= 0) Keys[index] = key; else Keys.Add(key);
        Keys.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    public static AnimationProject FromModel(MdlModel model)
    {
        var project = new AnimationProject { ModelName = model.Name };
        void Add(MdlNode node, int parent)
        {
            if (project.Joints.Count >= 512) throw new InvalidDataException("Model exceeds 512 joints.");
            var index = project.Joints.Count;
            project.Joints.Add(new(node.Name, parent, new(node.Position, node.Orientation, node.Scale)));
            foreach (var child in node.Children) Add(child, index);
        }
        if (model.GeometryRoot == null) throw new InvalidDataException("Model has no rig.");
        Add(model.GeometryRoot, -1);
        project.AnimationRoot = project.Joints.FirstOrDefault(j => j.Name.Equals("rootdummy", StringComparison.OrdinalIgnoreCase))?.Name
            ?? project.Joints[0].Name;
        project.Validate();
        return project;
    }
}
