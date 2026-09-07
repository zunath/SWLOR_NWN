using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;

namespace SWLOR.Toolset.Domain.Animation;

public sealed record SourceJoint(string Name, int Parent, Vector3 Position, Quaternion Rotation, Vector3 Scale);
public sealed record SourceAnimation(string Name, float Duration, IReadOnlyList<SourceTrack> Tracks, float StartTime = 0);
public sealed record SourceTrack(int Joint, string Path, string Interpolation, float[] Times, Vector4[] Values);

/// <summary>glTF 2.0 skeleton/animation reader, based on the Khronos specification. No scene engine is required.</summary>
public sealed class GltfAnimationSource
{
    private const int MaxBytes = 128 * 1024 * 1024;
    public IReadOnlyList<SourceJoint> Joints { get; private init; } = [];
    public IReadOnlyList<SourceAnimation> Animations { get; private init; } = [];
    private int[] Order { get; init; } = [];

    /// <summary>A single-key glTF pose has zero source duration; hold it for one editable second.</summary>
    public float GetPlaybackDuration(int animation) => Animations[animation].Duration > 0 ? Animations[animation].Duration : 1f;

    public static GltfAnimationSource Load(string path)
    {
        var bytes = ReadBounded(path);
        ReadOnlyMemory<byte>? bin = null;
        ReadOnlyMemory<byte> json = bytes;
        if (bytes.Length >= 12 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x46546c67)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) != 2 ||
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)) != bytes.Length)
                throw new InvalidDataException("Invalid GLB 2.0 header.");
            json = ReadOnlyMemory<byte>.Empty;
            for (var offset = 12; offset < bytes.Length;)
            {
                if (offset + 8 > bytes.Length) throw new InvalidDataException("Truncated GLB chunk.");
                var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
                var kind = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4));
                if (length % 4 != 0 || length > bytes.Length - offset - 8) throw new InvalidDataException("Invalid GLB chunk length.");
                var chunk = bytes.AsMemory(offset + 8, (int)length);
                if (kind == 0x4e4f534a)
                {
                    if (offset != 12 || json.Length != 0) throw new InvalidDataException("GLB JSON must be the first chunk.");
                    json = chunk;
                }
                else if (kind == 0x004e4942)
                {
                    if (bin != null) throw new InvalidDataException("Multiple GLB binary chunks.");
                    bin = chunk;
                }
                offset += checked(8 + (int)length);
            }
        }
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("asset").GetProperty("version").GetString() != "2.0")
            throw new InvalidDataException("Only glTF 2.0 is supported.");
        if (root.TryGetProperty("extensionsRequired", out var required) && required.GetArrayLength() > 0)
            throw new InvalidDataException("This source requires glTF extensions. Export an uncompressed glTF 2.0 animation.");
        var buffers = new List<ReadOnlyMemory<byte>>();
        var total = 0;
        foreach (var buffer in root.GetProperty("buffers").EnumerateArray())
        {
            var remaining = MaxBytes - total;
            var declared = buffer.GetProperty("byteLength").GetInt32();
            if (declared < 0 || declared > remaining)
                throw new InvalidDataException("Invalid or oversized glTF buffers.");
            ReadOnlyMemory<byte> data;
            if (!buffer.TryGetProperty("uri", out var uri))
            {
                if (buffers.Count != 0) throw new InvalidDataException("Only the first GLB buffer may omit its URI.");
                data = bin ?? throw new InvalidDataException("Missing GLB buffer.");
            }
            else
            {
                var value = uri.GetString()!;
                if (value.StartsWith("data:", StringComparison.Ordinal))
                {
                    var split = value.IndexOf(',');
                    if (split < 0 || !value[..split].EndsWith(";base64", StringComparison.Ordinal)) throw new InvalidDataException("Unsupported data URI.");
                    data = DecodeBuffer(value.AsSpan(split + 1), remaining);
                }
                else
                {
                    var folder = Path.GetFullPath(Path.GetDirectoryName(path)!);
                    var decoded = Uri.UnescapeDataString(value);
                    var file = Path.GetFullPath(Path.Combine(folder, decoded));
                    if (Path.IsPathRooted(decoded) || decoded.Contains(':') ||
                        !file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("glTF buffers must be local files alongside the source.");
                    data = ReadBounded(file, remaining);
                }
            }
            if (declared > data.Length || data.Length > remaining)
                throw new InvalidDataException("Invalid or oversized glTF buffers.");
            total += data.Length;
            buffers.Add(data[..declared]);
        }

        var nodes = root.GetProperty("nodes").EnumerateArray().ToArray();
        if (nodes.Length is < 1 or > 4096) throw new InvalidDataException("Source must contain 1–4096 joints.");
        var parents = Enumerable.Repeat(-1, nodes.Length).ToArray();
        for (var i = 0; i < nodes.Length; i++)
            if (nodes[i].TryGetProperty("children", out var children))
                foreach (var child in children.EnumerateArray())
                {
                    var index = child.GetInt32();
                    if ((uint)index >= nodes.Length || parents[index] >= 0 || index == i)
                        throw new InvalidDataException("Invalid source hierarchy.");
                    parents[index] = i;
                }
        var order = new List<int>(); var state = new byte[nodes.Length];
        void Visit(int i)
        {
            if (state[i] == 1) throw new InvalidDataException("Source hierarchy has a cycle.");
            if (state[i] == 2) return;
            state[i] = 1;
            if (parents[i] >= 0) Visit(parents[i]);
            state[i] = 2; order.Add(i);
        }
        for (var i = 0; i < nodes.Length; i++) Visit(i);
        var joints = nodes.Select((node, i) =>
        {
            var position = Vec(node, "translation", Vector4.Zero, 3);
            var rotation = Vec(node, "rotation", new(0, 0, 0, 1), 4);
            var scale = Vec(node, "scale", Vector4.One, 3);
            var p = new Vector3(position.X, position.Y, position.Z);
            var q = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
            var s = new Vector3(scale.X, scale.Y, scale.Z);
            if (node.TryGetProperty("matrix", out var matrix))
            {
                var a = matrix.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                if (a.Length != 16 || a.Any(v => !float.IsFinite(v)) || !Matrix4x4.Decompose(new(
                    a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8], a[9], a[10], a[11], a[12], a[13], a[14], a[15]), out s, out q, out p))
                    throw new InvalidDataException("Invalid source matrix.");
            }
            if (!AnimationProject.Finite(p) || !AnimationProject.Finite(s) || s.X <= 0 || s.Y <= 0 || s.Z <= 0 ||
                !float.IsFinite(q.LengthSquared()) || Math.Abs(q.LengthSquared() - 1) > 0.002f)
                throw new InvalidDataException("Source transforms require positive scale and unit rotations.");
            return new SourceJoint(node.TryGetProperty("name", out var name) ? name.GetString() ?? $"Node {i}" : $"Node {i}", parents[i], p, q, s);
        }).ToArray();

        var accessors = root.GetProperty("accessors"); var views = root.GetProperty("bufferViews");
        long componentsRead = 0;
        var decodedAccessors = new Dictionary<int, Vector4[]>();
        var decodedTimes = new Dictionary<int, float[]>();
        Vector4[] Accessor(int index, string type)
        {
            var a = accessors[index];
            if (a.GetProperty("componentType").GetInt32() != 5126 || a.GetProperty("type").GetString() != type ||
                a.TryGetProperty("sparse", out _) || a.TryGetProperty("normalized", out var normalized) && normalized.GetBoolean())
                throw new InvalidDataException("Animation tracks require non-sparse float accessors.");
            if (decodedAccessors.TryGetValue(index, out var decoded)) return decoded;
            var count = a.GetProperty("count").GetInt32();
            var width = type == "SCALAR" ? 1 : type == "VEC3" ? 3 : 4;
            if (count < 1 || count > 1_000_000 || (componentsRead += (long)count * width) > 8_000_000)
                throw new InvalidDataException("Source animation has too many keys.");
            var view = views[a.GetProperty("bufferView").GetInt32()];
            var data = buffers[view.GetProperty("buffer").GetInt32()];
            var start = Int(view, "byteOffset"); var offset = Int(a, "byteOffset");
            var length = view.GetProperty("byteLength").GetInt32();
            var stride = Int(view, "byteStride", width * 4);
            if (start < 0 || offset < 0 || length < 0 || stride < width * 4 || stride % 4 != 0 ||
                (long)start + length > data.Length || (long)offset + (long)(count - 1) * stride + width * 4 > length)
                throw new InvalidDataException("Animation accessor exceeds its buffer view.");
            var values = new Vector4[count];
            for (var row = 0; row < count; row++)
                for (var column = 0; column < width; column++)
                {
                    var value = BinaryPrimitives.ReadSingleLittleEndian(data.Span[(start + offset + row * stride + column * 4)..]);
                    if (!float.IsFinite(value)) throw new InvalidDataException("Source contains a non-finite key.");
                    values[row][column] = value;
                }
            decodedAccessors.Add(index, values);
            return values;
        }
        var animations = new List<SourceAnimation>();
        if (!root.TryGetProperty("animations", out var clips))
            throw new InvalidDataException("No skeletal animations in this source.");
        foreach (var clip in clips.EnumerateArray())
        {
            var tracks = new List<SourceTrack>(); var unique = new HashSet<(int, string)>();
            foreach (var channel in clip.GetProperty("channels").EnumerateArray())
            {
                var target = channel.GetProperty("target"); var joint = target.GetProperty("node").GetInt32();
                var channelPath = target.GetProperty("path").GetString()!;
                if (channelPath == "weights") continue; // Morph weights do not change the skeleton overlay.
                if ((uint)joint >= joints.Length || !unique.Add((joint, channelPath)) ||
                    channelPath is not ("translation" or "rotation" or "scale") || nodes[joint].TryGetProperty("matrix", out _))
                    throw new InvalidDataException("Invalid animated joint channel.");
                var sampler = clip.GetProperty("samplers")[channel.GetProperty("sampler").GetInt32()];
                var interpolation = sampler.TryGetProperty("interpolation", out var mode) ? mode.GetString()! : "LINEAR";
                if (interpolation is not ("LINEAR" or "STEP" or "CUBICSPLINE")) throw new InvalidDataException("Unsupported interpolation.");
                var input = sampler.GetProperty("input").GetInt32();
                if (!decodedTimes.TryGetValue(input, out var times))
                    decodedTimes.Add(input, times = Accessor(input, "SCALAR").Select(v => v.X).ToArray());
                var values = Accessor(sampler.GetProperty("output").GetInt32(), channelPath == "rotation" ? "VEC4" : "VEC3");
                if (times[0] < 0 || times.Zip(times.Skip(1)).Any(p => p.First >= p.Second) ||
                    values.Length != times.Length * (interpolation == "CUBICSPLINE" ? 3 : 1))
                    throw new InvalidDataException("Invalid animation key count or times.");
                var keyedValues = interpolation == "CUBICSPLINE" ? values.Where((_, i) => i % 3 == 1) : values;
                if (channelPath == "rotation" && keyedValues.Any(v => Math.Abs(v.LengthSquared() - 1) > .002f) ||
                    channelPath == "scale" && keyedValues.Any(v => v.X <= 0 || v.Y <= 0 || v.Z <= 0))
                    throw new InvalidDataException("Animation keys require unit rotations and positive scales.");
                tracks.Add(new(joint, channelPath, interpolation, times, values));
            }
            if (tracks.Count > 0)
            {
                var startTime = tracks.Min(t => t.Times[0]);
                var duration = tracks.Max(t => t.Times[^1]) - startTime;
                if (duration > 600) throw new InvalidDataException("Source animation duration exceeds 600 seconds.");
                animations.Add(new(clip.TryGetProperty("name", out var name) ? name.GetString() ?? "Animation" : $"Animation {animations.Count + 1}",
                    duration, tracks, startTime));
            }
        }
        if (animations.Count == 0) throw new InvalidDataException("No skeletal animations in this source.");
        return new() { Joints = joints, Animations = animations, Order = order.ToArray() };
    }

    public Matrix4x4[] Sample(int animation, float time)
    {
        if (!float.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
        var clip = Animations[animation];
        // Keep shared accessor timestamps intact; expose a zero-based timeline per clip.
        var sourceTime = Math.Clamp(time, 0, clip.Duration) + clip.StartTime;
        var positions = Joints.Select(j => j.Position).ToArray();
        var rotations = Joints.Select(j => j.Rotation).ToArray();
        var scales = Joints.Select(j => j.Scale).ToArray();
        foreach (var track in clip.Tracks)
        {
            var value = SampleTrack(track, sourceTime);
            switch (track.Path)
            {
                case "translation": positions[track.Joint] = new(value.X, value.Y, value.Z); break;
                case "scale": scales[track.Joint] = new(value.X, value.Y, value.Z); break;
                case "rotation":
                    var q = new Quaternion(value.X, value.Y, value.Z, value.W);
                    if (!float.IsFinite(q.LengthSquared()) || q.LengthSquared() < 1e-10f) throw new InvalidDataException("Invalid quaternion in source animation.");
                    rotations[track.Joint] = Quaternion.Normalize(q); break;
            }
        }
        var world = new Matrix4x4[Joints.Count];
        foreach (var i in Order)
        {
            if (!AnimationProject.Finite(positions[i]) || !AnimationProject.Finite(scales[i]) || scales[i].X <= 0 || scales[i].Y <= 0 || scales[i].Z <= 0)
                throw new InvalidDataException("Animated transforms must stay finite with positive scale.");
            world[i] = Matrix4x4.CreateScale(scales[i]) * Matrix4x4.CreateFromQuaternion(rotations[i]) * Matrix4x4.CreateTranslation(positions[i]);
            if (Joints[i].Parent >= 0) world[i] *= world[Joints[i].Parent];
        }
        // glTF is Y up; Aurora is Z up. Express both joint axes and translations in
        // Aurora coordinates after hierarchy evaluation (System.Numerics uses row vectors).
        var basis = Matrix4x4.CreateRotationX(MathF.PI / 2);
        var inverseBasis = Matrix4x4.Transpose(basis);
        return world.Select(matrix => inverseBasis * matrix * basis).ToArray();
    }

    private static Vector4 SampleTrack(SourceTrack track, float time)
    {
        var cubic = track.Interpolation == "CUBICSPLINE";
        Vector4 Value(int i) => track.Values[cubic ? i * 3 + 1 : i];
        if (time <= track.Times[0]) return Value(0);
        var right = Array.BinarySearch(track.Times, time);
        if (right >= 0) return Value(right);
        right = ~right;
        if (right == track.Times.Length) return Value(track.Times.Length - 1);
        var left = right - 1; var dt = track.Times[right] - track.Times[left];
        var t = (time - track.Times[left]) / dt;
        if (track.Interpolation == "STEP") return Value(left);
        if (cubic)
        {
            var t2 = t * t; var t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * Value(left) + (t3 - 2 * t2 + t) * dt * track.Values[left * 3 + 2] +
                (-2 * t3 + 3 * t2) * Value(right) + (t3 - t2) * dt * track.Values[right * 3];
        }
        if (track.Path != "rotation") return Vector4.Lerp(Value(left), Value(right), t);
        var a = Value(left); var b = Value(right);
        var q = Quaternion.Slerp(Quaternion.Normalize(new(a.X, a.Y, a.Z, a.W)), Quaternion.Normalize(new(b.X, b.Y, b.Z, b.W)), t);
        return new(q.X, q.Y, q.Z, q.W);
    }

    private static int Int(JsonElement node, string key, int fallback = 0) => node.TryGetProperty(key, out var value) ? value.GetInt32() : fallback;
    private static Vector4 Vec(JsonElement node, string key, Vector4 fallback, int length)
    {
        if (!node.TryGetProperty(key, out var value)) return fallback;
        if (value.GetArrayLength() != length) throw new InvalidDataException("Invalid transform component count.");
        for (var i = 0; i < length; i++) fallback[i] = value[i].GetSingle();
        return fallback;
    }
    private static byte[] ReadBounded(string path, int remaining = MaxBytes) =>
        AnimationSourceFile.ReadBytes(path, remaining, "glTF source");

    private static byte[] DecodeBuffer(ReadOnlySpan<char> encoded, int remaining)
    {
        var count = 0; var padding = 0;
        foreach (var value in encoded)
        {
            if (char.IsWhiteSpace(value)) continue;
            count++;
            padding = value == '=' ? padding + 1 : 0;
        }
        if (count % 4 != 0 || padding > 2)
            throw new InvalidDataException("Invalid base64 glTF buffer.");
        var length = count / 4 * 3 - padding;
        if (length > remaining) throw new InvalidDataException("Invalid or oversized glTF buffers.");
        var data = new byte[length];
        if (!Convert.TryFromBase64Chars(encoded, data, out var written) || written != length)
            throw new InvalidDataException("Invalid base64 glTF buffer.");
        return data;
    }
}
