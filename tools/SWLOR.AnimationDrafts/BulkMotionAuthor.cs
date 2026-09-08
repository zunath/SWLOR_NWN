using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.AnimationDrafts;

internal sealed record ActiveMotion(string Id, string InternalName, string Category, string Type, string Description,
    string Reference = "", string? SourceAnimation = null, string? Profile = null, float? Duration = null,
    string[]? Feats = null, string? DisplayName = null);
internal sealed record MotionProfile(string Name, string Source, float Duration, bool Procedural = false, int Repeats = 1);

/// <summary>Deterministic, source-backed drafts. A motion family is shared deliberately by related abilities;
/// these are editable starting points, not a claim that reference images were motion-captured.</summary>
internal static class BulkMotionAuthor
{
    internal static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    internal static MotionProfile Select(ActiveMotion motion)
    {
        var words = (motion.Id + " " + motion.Description).ToLowerInvariant();
        bool Has(string word) => words.Contains(word, StringComparison.Ordinal);
        var category = motion.Category;
        if (motion.SourceAnimation != null) return new(motion.Profile ?? "Native adaptation", motion.SourceAnimation, motion.Duration ?? 1.2f,
            motion.Profile?.Contains("recoil", StringComparison.OrdinalIgnoreCase) == true || motion.Profile == "Force leap");
        if (motion.Type.Contains("Stance", StringComparison.OrdinalIgnoreCase))
            return new("Guard activation", category is "Spear" or "Staff" or "Saberstaff" ? "plreadyr" : category == "Heavy Vibroblade" ? "2hreadyr" : "1hreadyr", 1f);
        if (category is "Pistol" or "Rifle" && (Has("reload") || Has("overclock") || Has("calibrat") || Has("aim") && !Has("damage")))
            return new("Weapon preparation", "getmid", 1.1f);
        if (category == "Pistol") return new("Pistol aim and recoil", "1hreadyr", .95f, true, Has("barrage") || Has("rapid") ? 3 : 1);
        if (category == "Rifle") return new("Rifle shoulder and recoil", "xbowrdy", 1.1f, true, Has("barrage") || Has("burst") ? 3 : 1);
        if (category == "Throwing" || Has("grenade")) return new("Overarm throw", "throwr", 1.1f);
        if (category == "Devices")
            return Has("trap") || Has("mine") || Has("field") ? new("Place device", "getlow", 1.3f) :
                Has("repair") || Has("analy") || Has("scan") ? new("Operate device", "getmid", 1.2f) : new("Activate device", "castpoint", 1.15f);
        if (category == "First Aid" || category == "Beast Mastery")
            return Has("revive") || Has("resusc") ? new("Kneel and assist", "getlow", 1.5f) : new("Reach and assist", "getmid", 1.2f);
        if (category == "Leadership") return new("Command gesture", Has("rally") || Has("inspir") ? "victoryfr" : "taunt", 1.2f);
        if (category == "Espionage") return new("Concealed action", Has("stealth") ? "steal" : "getmid", 1f);
        if (category == "Force" || category == "Mimicry")
        {
            if (Has("breath") || Has("cone") || Has("spray")) return new("Directed projection", "castout", 1.25f);
            if (Has("earth") || Has("quake") || Has("stomp")) return new("Ground impact", "nwkicks", 1.15f);
            if (Has("kick")) return new("Kick", "nwkickr", .9f);
            if (Has("claw") || Has("bite") || Has("rend")) return new("Unarmed rake", "nwslashr", .95f);
            if (Has("heal") || Has("ward") || Has("barrier") || Has("armor")) return new("Protective invocation", "castself", 1.2f);
            if (Has("nova") || Has("burst") || Has("storm") || Has("within")) return new("Area invocation", "castarea", 1.35f);
            return new("Directed invocation", "castpoint", 1.1f);
        }
        if (motion.Type.Contains("Stance", StringComparison.OrdinalIgnoreCase) || Has("stance"))
            return new("Guard activation", category is "Spear" or "Staff" or "Saberstaff" ? "plreadyr" : category == "Heavy Vibroblade" ? "2hreadyr" : "1hreadyr", 1f);
        var prefix = category switch { "Heavy Vibroblade" => "2h", "Spear" or "Staff" or "Saberstaff" => "pl", "Twin Blade" => "2w", "Katar" => "nw", _ => "1h" };
        var attack = Has("thrust") || Has("pierc") || Has("stab") || Has("impal") ? "stab" :
            Has("cleave") || Has("overhead") || Has("crush") || Has("rend") ? "slasho" :
            Has("sweep") || Has("spin") || Has("cyclone") ? "slashl" : "slashr";
        return new(prefix + " " + attack, prefix + attack, attack == "slasho" ? 1.1f : .95f);
    }

    internal static AnimationProject Bake(MdlModel model, ActiveMotion motion, MotionProfile profile)
    {
        var rig = AnimationProject.FromModel(model);
        var source = model.Animations.SingleOrDefault(a => a.Name.Equals(profile.Source, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"{motion.Id}: required base animation '{profile.Source}' is unavailable. Supply an explicit source; a silent unrelated fallback is not permitted.");
        var idle = model.Animations.Single(a => a.Name == "pause1");
        var bind = MdlAnimationPose.BindPose(model);
        PosedNode[] Sample(MdlAnimation clip, float time)
        {
            var pose = MdlAnimationPose.Sample(clip, time, bind);
            return rig.Joints.Select(j => pose.TryGetValue(j.Name, out var p) ? p : j.Rest).ToArray();
        }
        var neutral = Sample(idle, 0);
        var feet = new[] { "lfoot_g", "rfoot_g" }.Select(name => rig.Joints.FindIndex(j => j.Name == name)).ToArray();
        if (feet.Any(i => i < 0)) throw new InvalidDataException("The source rig must have humanoid feet.");
        var root = rig.Joints.FindIndex(j => j.Name == "rootdummy");
        if (root < 0) throw new InvalidDataException("The source rig must have rootdummy.");
        var neutralWorld = AnimationRig.World(rig.Joints, neutral);
        var floor = feet.Min(i => neutralWorld[i].Translation.Z);
        var result = rig.Clone(); result.Name = motion.Id; result.Keys.Clear(); result.Events.Clear();
        result.Duration = motion.Duration ?? profile.Duration; result.Transition = .12f;
        if (!float.IsFinite(result.Duration) || result.Duration < .4f || result.Duration > 10)
            throw new InvalidDataException(motion.Id + ": duration must be between 0.4 and 10 seconds.");
        int count = (int)Math.Ceiling(result.Duration * 20);
        for (int frame = 0; frame <= count; frame++)
        {
            var u = (float)frame / count;
            var time = u * result.Duration;
            var active = Math.Clamp((u - .15f) / .65f, 0, 1);
            var pose = Sample(source, profile.Procedural ? 0 : active * source.Length);
            if (profile.Name == "Force leap")
            {
                var flight = MathF.Sin(active * MathF.PI);
                foreach (var (name, angle) in new[] { ("lthigh_g", .48f), ("rthigh_g", .48f), ("lshin_g", -.75f), ("rshin_g", -.75f), ("torso_g", -.12f) })
                {
                    var index = rig.Joints.FindIndex(j => j.Name == name);
                    if (index < 0) throw new InvalidDataException("Missing leap joint: " + name);
                    pose[index] = pose[index] with { Orientation = Quaternion.Normalize(pose[index].Orientation * Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle * flight)) };
                }
            }
            if (profile.Procedural && profile.Name != "Force leap")
            {
                // Keep the native grip and elbow relationship; move the upper arm as a unit.
                // The pistol uses a single raised arm; the rifle keeps the two-handed native shoulder grip.
                float recoil = 0;
                for (int shot = 0; shot < profile.Repeats; shot++)
                {
                    var shotTime = .35f + shot * .16f;
                    var phase = (u - shotTime) / .13f;
                    if (phase is > 0 and < 1) recoil += MathF.Sin(phase * MathF.PI) * .10f;
                }
                foreach (var name in profile.Source == "xbowrdy" ? new[] { "rbicep_g", "lbicep_g" } : new[] { "rbicep_g" })
                {
                    var index = rig.Joints.FindIndex(j => j.Name == name);
                    if (index < 0) throw new InvalidDataException("Missing recoil joint: " + name);
                    pose[index] = pose[index] with { Orientation = Quaternion.Normalize(pose[index].Orientation * Quaternion.CreateFromAxisAngle(Vector3.UnitX, recoil)) };
                }
            }
            var weight = Math.Min(Math.Clamp(u / .15f, 0, 1), Math.Clamp((1 - u) / .2f, 0, 1));
            weight = weight * weight * (3 - 2 * weight);
            for (int joint = 0; joint < pose.Length; joint++)
                pose[joint] = new(Vector3.Lerp(neutral[joint].Position, pose[joint].Position, weight),
                    Quaternion.Slerp(neutral[joint].Orientation, pose[joint].Orientation, weight), float.Lerp(neutral[joint].Scale, pose[joint].Scale, weight));
            // Blending skeletal rotations can briefly arc a planted foot below the ground.
            // Translate the complete body up by that penetration; never twist ankles or grips.
            var world = AnimationRig.World(rig.Joints, pose);
            var lift = Math.Max(0, floor - feet.Min(i => world[i].Translation.Z));
            if (profile.Name == "Force leap") lift += .32f * MathF.Sin(active * MathF.PI) * weight;
            if (frame > 0 && frame < count)
                pose[root] = pose[root] with { Position = pose[root].Position + Vector3.UnitZ * lift };
            result.SetKey(time, pose);
        }
        // Add only the intermediate keys needed to correct the nonlinear foot arc between
        // baked rotations. Keep the sampler's actual interpolation, rather than assuming
        // the endpoints prove that the complete motion clears the floor.
        for (int pass = 0; pass < 3; pass++)
        for (int frame = 1; frame < Math.Ceiling(result.Duration * 120); frame++)
        {
            float time = frame / 120f;
            var pose = result.Sample(time);
            var world = AnimationRig.World(rig.Joints, pose);
            var penetration = floor - feet.Min(i => world[i].Translation.Z);
            if (penetration <= .002f) continue;
            pose[root] = pose[root] with { Position = pose[root].Position + Vector3.UnitZ * penetration };
            result.SetKey(time, pose);
        }
        result.Validate();
        ValidateMotion(result, neutral, floor);
        return result;
    }

    internal static void ValidateMotion(AnimationProject project, PosedNode[] neutral, float floor)
    {
        foreach (var time in new[] { 0f, project.Duration })
        {
            var pose = project.Sample(time);
            for (int i = 0; i < pose.Length; i++)
                if (Vector3.Distance(pose[i].Position, neutral[i].Position) > .0001f || Math.Abs(Quaternion.Dot(pose[i].Orientation, neutral[i].Orientation)) < .99999f)
                    throw new InvalidDataException(project.Name + ": entry and recovery must match the native idle pose.");
        }
        var feet = new[] { "lfoot_g", "rfoot_g" }.Select(name => project.Joints.FindIndex(j => j.Name == name)).ToArray();
        for (int frame = 0; frame <= Math.Ceiling(project.Duration * 120); frame++)
        {
            var world = AnimationRig.World(project.Joints, project.Sample(Math.Min(frame / 120f, project.Duration)));
            if (feet.Min(i => world[i].Translation.Z) < floor - .025f)
                throw new InvalidDataException(project.Name + ": interpolated foot penetrates the ground by more than 2.5 cm.");
        }
    }

    internal static void Generate(string modelPath, string inputPath, string output, bool overwrite, IReadOnlySet<string>? replaceIds = null)
    {
        var entries = JsonSerializer.Deserialize<ActiveMotion[]>(File.ReadAllText(inputPath), Json) ?? throw new InvalidDataException("Empty inventory.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Id) || string.IsNullOrEmpty(entry.InternalName) ||
                !Regex.IsMatch(entry.Id, "^[A-Z][A-Za-z0-9_]*$") || !Regex.IsMatch(entry.InternalName, "^sw_[a-z0-9_]{1,9}$") ||
                !names.Add(entry.InternalName) || !ids.Add(entry.Id))
                throw new InvalidDataException("Invalid or duplicate animation identity: " + entry?.Id);
            foreach (var feat in entry.Feats ?? [])
                if (!Enum.IsDefined(typeof(SWLOR.NWN.API.NWScript.Enum.FeatType), feat))
                    throw new InvalidDataException("Unknown feat: " + feat);
        }
        var resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (replaceIds != null && replaceIds.Any(id => !ids.Contains(id))) throw new InvalidDataException("A requested replacement ID is absent from the inventory.");
        foreach (var entry in entries)
            foreach (var resource in new[] { entry.InternalName, entry.InternalName + "_in", entry.InternalName + "_out" })
                if (!resources.Add(resource)) throw new InvalidDataException("Animation identity collides with a transition: " + resource);
        var manifestPath = Path.Combine(output, "active-manifest.json");
        using var previousManifest = File.Exists(manifestPath) ? JsonDocument.Parse(File.ReadAllText(manifestPath)) : null;
        var previous = previousManifest?.RootElement.GetProperty("Animations").EnumerateArray()
            .ToDictionary(a => a.GetProperty("Id").GetString()!) ?? new Dictionary<string, JsonElement>();
        var modelBytes = AnimationSourceFile.ReadBytes(modelPath, AnimationProject.MaximumFileBytes, "Base model");
        var model = new MdlReader().Parse(modelBytes);
        var reports = new List<object>();
        var pending = new Dictionary<string, string>();
        var baseProfiles = new Dictionary<string, MotionProfile>();
        foreach (var entry in entries)
        {
            var category = Regex.Replace(entry.Category.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            if (category.Length == 0) throw new InvalidDataException("Missing category for " + entry.Id);
            var relative = category + "/" + entry.Id + ".swlanim";
            var path = Path.Combine(output, relative);
            var profile = Select(entry);
            AnimationProject project;
            string contents;
            bool preserved = File.Exists(path) && !(overwrite && (replaceIds == null || replaceIds.Contains(entry.Id)));
            if (preserved)
            {
                contents = File.ReadAllText(path); project = AnimationProject.Deserialize(contents);
                if (project.Name != entry.Id) throw new InvalidDataException("Project identity mismatch: " + path);
            }
            else { project = Bake(model, entry, profile); contents = project.Serialize() + "\n"; pending.Add(path, contents); }
            if (profile.Procedural && !preserved) baseProfiles.TryAdd(profile.Name, profile);
            var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(contents))).ToLowerInvariant();
            var hasProvenance = preserved && previous.TryGetValue(entry.Id, out var prior) && prior.GetProperty("ProjectSha256").GetString() == hash;
            string? Prior(string property) => hasProvenance ? previous[entry.Id].GetProperty(property).GetString() : null;
            reports.Add(new { entry.Id, entry.InternalName, entry.Category, project.Duration, Project = relative,
                SourceModel = preserved ? Prior("SourceModel") ?? "Existing authored project" : model.Name,
                SourceAnimation = preserved ? Prior("SourceAnimation") : profile.Source, Profile = preserved ? Prior("Profile") ?? "Preserved authored motion" : profile.Name,
                Procedural = hasProvenance ? previous[entry.Id].GetProperty("Procedural").GetBoolean() : !preserved && profile.Procedural,
                Status = "Draft: in-game visual review required", entry.Reference, ProjectSha256 = hash });
        }
        foreach (var profile in baseProfiles.Values)
        {
            var id = Regex.Replace(profile.Name, "[^A-Za-z0-9]", "");
            var baseMotion = new ActiveMotion(id, "sw_base", "Bases", "Base", "Reusable procedural base: " + profile.Name);
            var basePath = Path.Combine(output, "bases", id + ".swlanim");
            if (overwrite || !File.Exists(basePath))
                pending.Add(basePath, Bake(model, baseMotion, profile).Serialize() + "\n");
        }
        foreach (var (path, contents) in pending) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, contents); }
        Directory.CreateDirectory(output);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(new { Version = 1,
            SourceModelSha256 = Convert.ToHexString(SHA256.HashData(modelBytes)).ToLowerInvariant(), Animations = reports }, Json) + "\n");
        var root = Directory.GetParent(Path.GetFullPath(output))?.Parent?.FullName;
        if (root != null && Directory.Exists(Path.Combine(root, "SWLOR.Game.Server"))) WriteCatalog(root, entries);
        Console.WriteLine($"Wrote {pending.Count} projects including {baseProfiles.Count} reusable procedural bases. Inventory: {entries.Length} animations.");
    }

    private static void WriteCatalog(string root, ActiveMotion[] entries)
    {
        static string Q(string value) => JsonSerializer.Serialize(value);
        var lines = new List<string> { "// Generated from design/animations/active-abilities.json and saved animation projects.",
            "using System.Collections.Generic;", "using SWLOR.NWN.API.NWScript.Enum;", "", "namespace SWLOR.Game.Server.Service.AnimationService;", "",
            "public static class ActiveAbilityAnimationCatalog", "{", "    public static IReadOnlyList<AbilityAnimationEntry> Entries { get; } = new AbilityAnimationEntry[]", "    {" };
        foreach (var entry in entries)
        {
            var feats = entry.Feats ?? [];
            foreach (var feat in feats)
                if (!Enum.IsDefined(typeof(SWLOR.NWN.API.NWScript.Enum.FeatType), feat))
                    throw new InvalidDataException("Unknown feat: " + feat);
            lines.Add($"        new({Q(entry.Id)}, {Q(entry.DisplayName ?? entry.Id)}, {Q(entry.Category)}, AuthoredAnimation.{entry.Id}, new FeatType[] {{ {string.Join(", ", feats.Select(f => "FeatType." + f))} }}),");
        }
        lines.AddRange(["    };", "}"]);
        File.WriteAllText(Path.Combine(root, "SWLOR.Game.Server/Service/AnimationService/ActiveAbilityAnimationCatalog.cs"), string.Join("\n", lines) + "\n");
    }
}

