using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Tests.Feature;

public class WeaponGripMeasurementInputsTests
{
    [Test]
    public void MeasuredHandsHandlesAndAnimationSocketsHaveNotChanged()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        var haks = Path.Combine(root!.FullName, "SWLOR_Haks");
        var measurements = typeof(WeaponFit).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.PistolGripMeasurements")!;
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var hands = (IDictionary)measurements.GetField("Hands", flags)!.GetValue(null)!;
        var handles = (IDictionary)measurements.GetField("Handles", flags)!.GetValue(null)!;
        var rifles = typeof(WeaponFit).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.RifleGripMeasurements")!;
        var rifleHandles = (IDictionary)rifles.GetField("Handles", flags)!.GetValue(null)!;
        var paths = hands.Keys.Cast<string>().Select(hand =>
                (hand[9] == 'r' ? "sw_pt_rhand/" : "sw_pt_lhand/") + hand + ".mdl")
            .Concat(handles.Keys.Cast<int>().Select(model =>
                $"sw_weapon/wbwsh_m_{(model % 100) * 10 + model / 100:D3}.mdl"))
            .Concat(rifleHandles.Keys.Cast<int>().Select(model =>
                $"sw_weapon/wbwxl_m_{(model % 100) * 10 + model / 100:D3}.mdl"))
            .Concat(SocketInputs).Concat(RifleBankInputs(haks))
            .Select(path => path.ToLowerInvariant()).Distinct().Order(StringComparer.Ordinal).ToArray();
        // Read the finished resources, including uncommitted hand repairs. A sparse
        // checkout may omit unchanged weapons/skeletons; resolve those in one Git batch.
        var files = paths.Select(path => path.Split('/')[0]).Distinct()
            .Where(folder => Directory.Exists(Path.Combine(haks, folder)))
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(haks, folder), "*.mdl"))
            .ToDictionary(path => Path.GetRelativePath(haks, path).Replace('\\', '/'),
                StringComparer.OrdinalIgnoreCase);
        var missing = paths.Where(path => !files.ContainsKey(path)).ToArray();
        var committed = ReadCommitted(haks, missing);
        var text = new StringBuilder();
        foreach (var path in paths)
        {
            var bytes = files.TryGetValue(path, out var file) ? File.ReadAllBytes(file) : committed[path];
            text.Append(path).Append(' ').Append(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()).Append('\n');
        }
        var actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToLowerInvariant();
        var expected = (string)measurements.GetField("InputFingerprint", flags)!.GetRawConstantValue()!;
        actual.Should().Be(expected,
            "changed hand, handle or socket geometry needs new fit measurements and client review");
    }

    [Test]
    public void SparseFallbackReadsALargeHandBatchWithoutBlocking()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        var haks = Path.Combine(root!.FullName, "SWLOR_Haks");
        var measurements = typeof(WeaponFit).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.PistolGripMeasurements")!;
        var hands = (IDictionary)measurements.GetField("Hands", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var paths = hands.Keys.Cast<string>().Select(hand =>
            (hand[9] == 'r' ? "sw_pt_rhand/" : "sw_pt_lhand/") + hand + ".mdl").ToArray();
        paths.Sum(path => Encoding.UTF8.GetByteCount("HEAD:" + path + "\n")).Should().BeGreaterThan(4096,
            "the sparse fallback must drain model output before filling the request pipe");

        var resources = ReadCommitted(haks, paths);

        resources.Keys.Should().BeEquivalentTo(paths);
        resources.Values.Should().OnlyContain(bytes => bytes.Length > 0);
        foreach (var path in paths.Where(path => File.Exists(Path.Combine(haks, path))).Take(3))
            resources[path].Should().Equal(File.ReadAllBytes(Path.Combine(haks, path)));
    }

    private static string[] RifleBankInputs(string haks)
    {
        const string recipe = "tools/RifleGripProfileMotions.json";
        var path = Path.Combine(haks, recipe);
        var bytes = File.Exists(path) ? File.ReadAllBytes(path) : ReadCommitted(haks, new[] { recipe })[recipe];
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.GetProperty("models").EnumerateArray()
            .SelectMany(model => model.TryGetProperty("bankParts", out var parts)
                ? parts.EnumerateArray().Select(part => part.GetProperty("output").GetString()!)
                : new[] { model.GetProperty("output").GetString()! })
            .ToArray();
    }

    private static Dictionary<string, byte[]> ReadCommitted(string haks, string[] paths)
    {
        var result = new Dictionary<string, byte[]>();
        if (paths.Length == 0) return result;
        var tracked = TrackedPaths(haks, paths);
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = haks, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("cat-file");
        start.ArgumentList.Add("--batch");
        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        var stream = process.StandardOutput.BaseStream;
        foreach (var path in paths)
        {
            process.StandardInput.WriteLine("HEAD:" + tracked.GetValueOrDefault(path, path));
            process.StandardInput.Flush();
            var header = new StringBuilder();
            for (int value; (value = stream.ReadByte()) != '\n';)
            {
                if (value < 0) throw new EndOfStreamException("Missing measured resource: " + path);
                header.Append((char)value);
            }
            var fields = header.ToString().Split(' ');
            if (fields.Length != 3 || fields[1] != "blob")
                throw new FileNotFoundException("Missing measured resource: " + path);
            var data = new byte[int.Parse(fields[2])];
            stream.ReadExactly(data);
            if (stream.ReadByte() != '\n') throw new InvalidDataException("Invalid Git resource boundary");
            result.Add(path, data);
        }
        process.StandardInput.Close();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, errors.GetAwaiter().GetResult());
        return result;
    }

    private static Dictionary<string, string> TrackedPaths(string haks, string[] paths)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = haks, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var argument in new[] { "ls-tree", "-r", "--name-only", "-z", "HEAD", "--" })
            start.ArgumentList.Add(argument);
        foreach (var folder in paths.Select(path => path.Split('/')[0]).Distinct())
            start.ArgumentList.Add(folder);
        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, errors.GetAwaiter().GetResult());
        var requested = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(requested.Contains)
            .ToDictionary(path => path, path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static readonly string[] SocketInputs =
    {
        "sw_cr_creature/a_ba_med_weap.mdl",
        "sw_cr_creature/a_ba_non_combat.mdl",
        "sw_cr_creature/an_a_ba.mdl",
        "sw_cr_creature/an_a_fa.mdl",
        "sw_pt_root/pfd0.mdl",
        "sw_pt_root/pfe0.mdl",
        "sw_pt_root/pfh0.mdl",
        "sw_pt_root/pfo0.mdl",
        "sw_pt_root/pfz0.mdl",
        "sw_pt_root/pmd0.mdl",
        "sw_pt_root/pme0.mdl",
        "sw_pt_root/pmh0.mdl",
        "sw_pt_root/pmo0.mdl",
        "sw_pt_root/pmz0.mdl",
        "sw_cr_creature/a_rfl_pfd0.mdl",
        "sw_cr_creature/a_rfl_pfe0.mdl",
        "sw_cr_creature/a_rfl_pfh0.mdl",
        "sw_cr_creature/a_rfl_pfo0.mdl",
        "sw_cr_creature/a_rfl_pfz0.mdl",
        "sw_cr_creature/a_rfl_pmd0.mdl",
        "sw_cr_creature/a_rfl_pme0.mdl",
        "sw_cr_creature/a_rfl_pmh0.mdl",
        "sw_cr_creature/a_rfl_pmo0.mdl",
        "sw_cr_creature/a_rfl_pmz0.mdl",
        "sw_weapon/wbwsh_t_011.mdl",
        "sw_weapon/wbwsh_b_011.mdl",
        "sw_weapon/wbwxl_t_011.mdl",
        "sw_weapon/wbwxl_b_011.mdl"
    };
}

