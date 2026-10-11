using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.PreviewRender.Performance;
using Nwn.Formats.Erf;
using NwnResourceType = Nwn.Formats.Resources.ResourceType;

namespace SWLOR.Toolset.PreviewRender.Application;

internal static class AreaPerformanceAcceptance
{
    private static readonly TimeSpan PackDeadline = TimeSpan.FromMinutes(10);

    public static async Task EditSaveAndPackAsync(AreaEditorViewModel editor, string moduleRoot, string runRoot, ToolsetPerformanceObserver observer)
    {
        var areaResRef = editor.AreaResRef;
        var section = editor.Sections.Where(candidate => candidate.Rows.Count > 0)
            .OrderBy(candidate => candidate.BlueprintType).FirstOrDefault()
            ?? throw new InvalidDataException("The selected area has no placed instance available for the reversible save measurement.");
        var row = section.Rows[0];
        section.SelectedRow = row;
        var beforeX = section.DetailX;
        var afterX = beforeX + 0.125;
        if (!double.IsFinite(afterX) || Math.Abs(afterX - beforeX) < 0.124)
            throw new InvalidDataException("The selected native instance cannot represent the bounded reversible position edit.");
        var beforeHashes = await Task.Run(() => ResourceHashes(moduleRoot, areaResRef));
        section.DetailX = afterX;
        if (Math.Abs(section.DetailX - afterX) > 0.0001)
            throw new InvalidOperationException("The production area instance editor refused the reversible X-position edit.");

        observer.SetPhase(PerformancePhase.Save);
        var saveWatch = Stopwatch.StartNew();
        if (!await editor.TrySaveAsync())
        {
            observer.Record(PerformancePhase.Save, PerformanceOperation.AreaSave, saveWatch,
                "Production TrySaveAsync on a run-owned copy of the selected module.", completed: false);
            throw new InvalidOperationException("The production area editor refused to save the reversible area edit.");
        }
        if (editor.IsDirty)
            throw new InvalidDataException("The production save returned with dirty area documents.");
        var saveMilliseconds = observer.Record(PerformancePhase.Save, PerformanceOperation.AreaSave, saveWatch,
            "Production TrySaveAsync on a run-owned copy of the selected module.");
        var afterHashes = await Task.Run(() => ResourceHashes(moduleRoot, areaResRef));
        if (beforeHashes.Git == afterHashes.Git)
            throw new InvalidDataException("The native instance edit did not change the copied GIT resource.");

        var cli = Environment.GetEnvironmentVariable("SWLOR_CLI_DLL");
        if (string.IsNullOrWhiteSpace(cli) || !File.Exists(cli))
            throw new InvalidOperationException("SWLOR_CLI_DLL must point to the already-built production SWLOR.CLI.dll.");
        var packedModule = Path.Combine(moduleRoot, areaResRef + "_performance.mod");
        if (File.Exists(packedModule))
            throw new IOException("Refusing to overwrite an existing run-owned performance MOD.");
        var packStart = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = moduleRoot
        };
        packStart.ArgumentList.Add(Path.GetFullPath(cli));
        packStart.ArgumentList.Add("--pack");
        packStart.ArgumentList.Add(packedModule);
        packStart.ArgumentList.Add("--no-prompt");
        observer.SetPhase(PerformancePhase.Pack);
        var packWatch = Stopwatch.StartNew();
        using var process = Process.Start(packStart)
            ?? throw new InvalidOperationException("The production CLI pack process did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(PackDeadline);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await File.WriteAllTextAsync(Path.Combine(runRoot, "performance-pack-timeout.log"),
                $"Timed out after {PackDeadline}.{Environment.NewLine}stdout:{Environment.NewLine}{await stdout}{Environment.NewLine}stderr:{Environment.NewLine}{await stderr}");
            throw new TimeoutException($"The owned native module pack exceeded {PackDeadline}; logs were retained.");
        }
        var standardOutput = await stdout;
        var standardError = await stderr;
        await File.WriteAllTextAsync(Path.Combine(runRoot, "performance-pack.log"),
            $"ExitCode={process.ExitCode}{Environment.NewLine}stdout:{Environment.NewLine}{standardOutput}{Environment.NewLine}stderr:{Environment.NewLine}{standardError}");
        if (process.ExitCode != 0 || !File.Exists(packedModule) || new FileInfo(packedModule).Length == 0)
        {
            observer.Record(PerformancePhase.Pack, PerformanceOperation.NativeModulePack, packWatch,
                "Production SWLOR.CLI --pack on a run-owned copy.", completed: false);
            throw new InvalidOperationException($"The production native module pack failed with exit code {process.ExitCode}; logs were retained.");
        }
        (Dictionary<string, string> ResourceHashes, long Length, string ModuleSha256) packedVerification;
        try
        {
            packedVerification = await Task.Run(
                () => VerifyPackedModule(packedModule, moduleRoot, areaResRef));
        }
        catch
        {
            observer.Record(PerformancePhase.Pack, PerformanceOperation.NativeModulePack, packWatch,
                "Production SWLOR.CLI --pack plus ARE/GIT/GIC presence and nonempty-resource verification.", completed: false);
            throw;
        }
        var packMilliseconds = observer.Record(PerformancePhase.Pack, PerformanceOperation.NativeModulePack, packWatch,
            "Production SWLOR.CLI --pack plus ARE/GIT/GIC presence and nonempty-resource verification.");
        var packedResourceHashes = packedVerification.ResourceHashes;
        var evidence = new
        {
            Schema = "swlor.area-performance-edit.v1",
            AreaResRef = areaResRef,
            BlueprintType = section.BlueprintType.ToString(),
            ListIndex = row.Index,
            OriginalTag = row.Tag,
            OriginalX = beforeX,
            EditedX = afterX,
            BaselineResources = beforeHashes,
            SavedResources = afterHashes,
            SaveMilliseconds = saveMilliseconds,
            PackMilliseconds = packMilliseconds,
            PackedModule = Path.GetFullPath(packedModule),
            PackedModuleLength = packedVerification.Length,
            PackedModuleSha256 = packedVerification.ModuleSha256,
            PackedResourceSha256 = packedResourceHashes,
            ReversibleInOwnedCopy = true,
            SourceModuleUntouched = true
        };
        var path = Path.Combine(runRoot, "area-performance-edit.json");
        if (File.Exists(path))
            throw new IOException("Refusing to overwrite the run's reversible edit evidence.");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static async Task VerifyFreshReopenAsync(AreaEditorViewModel editor, string moduleRoot, string runRoot)
    {
        _ = moduleRoot;
        var priorPath = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE")
            ?? throw new InvalidOperationException("Fresh performance reopen requires the prior edit evidence path.");
        using var prior = JsonDocument.Parse(await File.ReadAllBytesAsync(priorPath));
        var root = prior.RootElement;
        var areaResRef = editor.AreaResRef;
        if (!string.Equals(areaResRef, root.GetProperty("AreaResRef").GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The fresh editor opened a different area than the saved performance edit.");
        var blueprintType = root.GetProperty("BlueprintType").GetString()
            ?? throw new InvalidDataException("The edit evidence has no placed-instance type.");
        var index = root.GetProperty("ListIndex").GetInt32();
        var section = editor.Sections.SingleOrDefault(candidate => candidate.BlueprintType.ToString() == blueprintType)
            ?? throw new InvalidDataException("Fresh workspace reopen did not restore the edited placed-instance section.");
        var row = section.Rows.SingleOrDefault(candidate => candidate.Index == index)
            ?? throw new InvalidDataException("Fresh workspace reopen did not restore the edited placed-instance row.");
        section.SelectedRow = row;
        var restoredX = section.DetailX;
        var expectedX = root.GetProperty("EditedX").GetDouble();
        if (Math.Abs(restoredX - expectedX) > 0.0001)
            throw new InvalidDataException($"Fresh workspace reopen did not restore the native instance X coordinate: expected={expectedX:R}, actual={restoredX:R}.");
        var packedModule = Path.GetFullPath(root.GetProperty("PackedModule").GetString()
            ?? throw new InvalidDataException("The edit evidence has no packed native module."));
        var expectedModuleSha256 = root.GetProperty("PackedModuleSha256").GetString()
            ?? throw new InvalidDataException("The edit evidence has no packed module hash.");
        var expectedModuleLength = root.GetProperty("PackedModuleLength").GetInt64();
        var expectedPackedResources = root.GetProperty("PackedResourceSha256")
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => property.Value.GetString()
                    ?? throw new InvalidDataException($"The edit evidence has no hash for packed {property.Name} bytes."),
                StringComparer.Ordinal);
        var packedVerification = await Task.Run(
            () => VerifyReopenedPackedModule(
                packedModule,
                areaResRef,
                expectedModuleSha256,
                expectedModuleLength,
                expectedPackedResources));
        var evidence = new
        {
            Schema = "swlor.area-performance-reopen.v1",
            AreaResRef = areaResRef,
            BlueprintType = blueprintType,
            ListIndex = index,
            RestoredX = restoredX,
            ExpectedX = expectedX,
            PackedModuleSha256 = packedVerification.ModuleSha256,
            PackedResourceSha256 = packedVerification.ResourceHashes,
            FreshWorkspaceAndEditor = true,
            PerformanceBudgetQualified = false
        };
        var path = Path.Combine(runRoot, "area-performance-reopen.json");
        if (File.Exists(path))
            throw new IOException("Refusing to overwrite the fresh reopen evidence.");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static (Dictionary<string, string> ResourceHashes, long Length, string ModuleSha256) VerifyPackedModule(
        string packedModule,
        string moduleRoot,
        string areaResRef)
    {
        var packedFile = new FileInfo(packedModule);
        if (!packedFile.Exists || packedFile.Length == 0)
            throw new InvalidDataException("The production pack did not produce a nonempty native module.");

        var packedModuleSha256 = HashFile(packedModule);
        var packedResourceHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        using var archive = ErfArchive.Open(packedModule);
        foreach (var (extension, type) in new[]
        {
            ("are", NwnResourceType.Are),
            ("git", NwnResourceType.Git),
            ("gic", NwnResourceType.Gic)
        })
        {
            var sourcePath = Path.Combine(moduleRoot, extension, areaResRef + "." + extension + ".json");
            if (!File.Exists(sourcePath) && extension == "gic")
                continue;
            var entry = archive.Entries.SingleOrDefault(candidate =>
                candidate.ResRef.Value.Equals(areaResRef, StringComparison.OrdinalIgnoreCase)
                && candidate.Type == type)
                ?? throw new InvalidDataException($"The finished native module omitted the edited area resource {areaResRef}.{extension}.");
            var bytes = archive.ReadAllBytes(entry);
            if (bytes.Length == 0)
                throw new InvalidDataException($"The finished native module contains an empty {areaResRef}.{extension} resource.");
            packedResourceHashes.Add(extension, Hash(bytes));
        }

        return (packedResourceHashes, packedFile.Length, packedModuleSha256);
    }

    private static (string ModuleSha256, Dictionary<string, string> ResourceHashes) VerifyReopenedPackedModule(
        string packedModule,
        string areaResRef,
        string expectedModuleSha256,
        long expectedModuleLength,
        IReadOnlyDictionary<string, string> expectedResourceHashes)
    {
        var packedFile = new FileInfo(packedModule);
        if (!packedFile.Exists || packedFile.Length == 0
            || packedFile.Length != expectedModuleLength
            || HashFile(packedModule) != expectedModuleSha256)
            throw new InvalidDataException("Fresh reopen cannot verify the exact native module packed from the saved edit.");

        var actualResourceHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        using var archive = ErfArchive.Open(packedModule);
        foreach (var (extension, type) in new[]
        {
            ("are", NwnResourceType.Are),
            ("git", NwnResourceType.Git),
            ("gic", NwnResourceType.Gic)
        })
        {
            if (!expectedResourceHashes.TryGetValue(extension, out var expectedHash))
                continue;
            var entry = archive.Entries.SingleOrDefault(candidate =>
                candidate.ResRef.Value.Equals(areaResRef, StringComparison.OrdinalIgnoreCase)
                && candidate.Type == type)
                ?? throw new InvalidDataException($"The retained native module no longer contains {areaResRef}.{extension}.");
            var bytes = archive.ReadAllBytes(entry);
            if (bytes.Length == 0 || Hash(bytes) != expectedHash)
                throw new InvalidDataException($"The retained native module {areaResRef}.{extension} differs from the edit-stage packed bytes.");
            actualResourceHashes.Add(extension, expectedHash);
        }

        if (actualResourceHashes.Count != expectedResourceHashes.Count)
            throw new InvalidDataException("The retained native module contains a different set of verified area resources than the edit-stage pack.");

        return (expectedModuleSha256, actualResourceHashes);
    }
    private static (string Are, string Git, string? Gic) ResourceHashes(string moduleRoot, string areaResRef)
    {
        var are = Path.Combine(moduleRoot, "are", areaResRef + ".are.json");
        var git = Path.Combine(moduleRoot, "git", areaResRef + ".git.json");
        var gic = Path.Combine(moduleRoot, "gic", areaResRef + ".gic.json");
        if (!File.Exists(are) || !File.Exists(git))
            throw new FileNotFoundException("The run-owned full area lacks its ARE/GIT pair.");
        return (HashFile(are), HashFile(git), File.Exists(gic) ? HashFile(gic) : null);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
