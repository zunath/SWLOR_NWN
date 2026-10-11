using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Doors;
using Nwn.Authoring.Resources;
using Nwn.Formats.Erf;
using Nwn.Formats.Gff;
using NwnResourceType = Nwn.Formats.Resources.ResourceType;
using SwlorResourceType = Nwn.Formats.Resources.ResourceType;
using Nwn.Preview.Areas;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Views;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.PreviewRender.Application;

internal static class SharedAreaNativeAcceptance
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(90);

    public static async Task RunAsync(
        Window window,
        AreaEditorViewModel editor,
        PaletteViewModel palette,
        WorkspaceContext workspaceContext,
        string moduleRoot,
        string runRoot,
        long resourceIndexMilliseconds,
        long sceneReadyMilliseconds,
        long? firstQualifiedWindowFrameMilliseconds)
    {
        var viewport = window.GetVisualDescendants().OfType<AreaSceneView>().Single().Viewport;
        var areaScene = editor.AreaScene ?? throw new InvalidOperationException("The production Area Editor has no built scene.");
        var state = palette.PresentationState;
        var baselineAreHash = HashFile(Path.Combine(moduleRoot, "are", editor.AreaResRef + ".are.json"));
        var baselineGitHash = HashFile(Path.Combine(moduleRoot, "git", editor.AreaResRef + ".git.json"));
        var baselineCreatureListHash = CreatureListHash(moduleRoot, editor.AreaResRef);
        var seedCreatureListHash = CreatureListHash(moduleRoot, "xm_check_area");
        var gicPath = Path.Combine(moduleRoot, "gic", editor.AreaResRef + ".gic.json");
        var baselineGicHash = OptionalGicStableHash(gicPath);
        var baselineGicDoorCount = GicDoorCount(gicPath);
        var phase = Stopwatch.StartNew();

        state.SelectTypeCommand.Execute(state.Types.Single(type => type.Option.Type is null));
        state.UseManualTilePaintCommand.Execute(null);
        state.SelectedRow = state.Rows.Single(row => row.Name == "All tiles");
        var tile = state.Tiles.SingleOrDefault(row => row.Snapshot.ResRef == "ttr01_h18_01")
            ?? throw new InvalidOperationException("The shared Manual tile Palette does not contain licensed ttr01_h18_01.");
        state.PlaceCommand.Execute(tile);
        await WaitForAsync(() => viewport.IsTilePlacementActive, "the Palette tile placement to arm");
        await ClickWorldAsync(window, viewport, new Vector3(25, 15, 0));
        await WaitForAsync(() => editor.AreaScene?.Tiles.Any(item => item.TileId == 75) == true,
            "the real viewport tile pick to commit tile 75");
        var tilePlacementMilliseconds = phase.ElapsedMilliseconds;
        phase.Restart();

        state.SelectTypeCommand.Execute(state.Types.Single(type => type.Option.Type == Nwn.Authoring.Resources.ModuleResourceType.Utd));
        state.ShowStandardCommand.Execute(null);
        var candidateCategories = state.Rows.Where(row => row.Count > 0).ToArray();
        if (candidateCategories.Length == 0)
            throw new InvalidOperationException("The Standard UTD palette has no category with entries.");
        state.SelectedRow = candidateCategories[0];
        await WaitForAsync(() => state.Tiles.Count > 0, "the selected Standard UTD category entries");
        var workspace = workspaceContext.Workspace
            ?? throw new InvalidOperationException("The production shell no longer has an open module workspace.");
        var candidateDescriptions = new List<string>();
        PaletteEntryRow? door = null;
        long selectedDoorGenericId = -1;
        bool selectedDoorLocked = true;
        string? selectedDoorNativeSha256 = null;
        foreach (var candidate in state.Tiles)
        {
            var blueprint = workspace.LoadIndexedBlueprint(ResourceType.Utd, candidate.Snapshot.ResRef);
            var store = new DoorValueStore(blueprint.Document.Root);
            var storedAppearance = DoorAppearanceValueStore.Read(store);
            var locked = store.GetInteger(BehaviorFieldStorage.Field, "Locked") == 1;
            candidateDescriptions.Add($"{candidate.Snapshot.ResRef}: {storedAppearance.Kind}/{storedAppearance.Id}, locked={locked}");
            if (door is null && storedAppearance.Kind == DoorAppearanceKind.Generic && !locked)
            {
                door = candidate;
                selectedDoorGenericId = storedAppearance.Id;
                selectedDoorLocked = locked;
                selectedDoorNativeSha256 = Hash(GffWriter.Write(NativeGffBridge.ToNativeDocument(blueprint.Document)));
            }
        }
        if (door is null)
        {
            throw new InvalidOperationException(
                $"The populated Standard UTD catalog has no unlocked Generic door suitable for this fixture: {string.Join("; ", candidateDescriptions)}.");
        }
        var doorTemplate = door.Snapshot.ResRef;
        state.PlaceCommand.Execute(door);
        await WaitForAsync(() => viewport.IsPlacementActive, "the native door placement to arm");
        var anchor = (editor.AreaScene ?? throw new InvalidOperationException("The scene disappeared."))
            .DoorAnchors.Single(item => item.Type == 1 && !editor.AreaScene.IsDoorwayFilled(item));
        await ClickWorldAsync(window, viewport, anchor.Position);
        await WaitForAsync(() => editor.AreaScene?.Instances.Count(item => item.Kind == InstanceMarkerKind.Door) == 1,
            "the real viewport door pick to commit");
        var doorPlacementMilliseconds = phase.ElapsedMilliseconds;

        var doorIndex = editor.AreaScene!.Instances.ToList().FindIndex(item => item.Kind == InstanceMarkerKind.Door);
        var doorMarker = editor.AreaScene.Instances[doorIndex];
        var section = editor.Sections.Single(item => item.UsesDoorEditor);
        await Dispatcher.UIThread.InvokeAsync(() => { });
        if (!section.Rows.Any(item => item.Index == doorMarker.ListIndex))
        {
            var rowIndexes = string.Join(",", section.Rows.Select(item => item.Index));
            throw new InvalidOperationException(
                $"Viewport door marker has no production GIT list row: area={editor.AreaResRef}, " +
                $"resref={doorMarker.TemplateResRef}, markerIndex={doorMarker.ListIndex}, rows=[{rowIndexes}], " +
                $"sceneStatus='{editor.SceneStatus}', placementPending={editor.IsPlacementPending}, " +
                $"placementStatus='{editor.PlacementStatus}', selected={editor.SelectedSceneInstance?.TemplateResRef}.");
        }
        section.SelectedRow = section.Rows.Single(item => item.Index == doorMarker.ListIndex);
        if (section.DoorEditor is null)
            throw new InvalidOperationException("Selecting the placed door did not create the production door detail editor.");
        await WaitForAsync(() => section.DoorEditor is not null
            && section.DoorEditor.Appearance.Tiles.Any(item => item.Option.Id.Value == "Specific:1"),
            "the native Specific:1 gallery option to load");
        var specific = section.DoorEditor.Appearance.Tiles.Single(item => item.Option.Id.Value == "Specific:1");
        section.DoorEditor.Appearance.Highlighted = specific;
        await WaitForAsync(() => section.DoorEditor!.Appearance.Tiles.Any(item => item.IsCurrent && item.Option.Id.Value == "Specific:1"),
            "the shared Specific:1 selection transaction");
        if (!section.DoorEditor.Appearance.Tiles.Any(item => item.IsCurrent && item.Option.Id.Value == "Specific:1"))
            throw new InvalidOperationException("The shared door appearance choice did not commit through the selected instance transaction.");
        section.DetailTag = "sw_editor_door";

        var saveWatch = Stopwatch.StartNew();
        if (!await editor.TrySaveAsync()) throw new InvalidOperationException("The production Area Editor refused to save the edited area.");
        var saveMilliseconds = saveWatch.ElapsedMilliseconds;
        if (editor.IsDirty) throw new InvalidOperationException("The production save returned with a dirty area session.");

        var areaResRef = editor.AreaResRef;
        var sourcePaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["are"] = Path.Combine(moduleRoot, "are", areaResRef + ".are.json"),
            ["git"] = Path.Combine(moduleRoot, "git", areaResRef + ".git.json"),
            ["gic"] = Path.Combine(moduleRoot, "gic", areaResRef + ".gic.json"),
        };
        foreach (var (extension, path) in sourcePaths)
        {
            if (!File.Exists(path))
            {
                if (extension == "gic" && baselineGicHash is null) continue;
                throw new FileNotFoundException("The saved area resource is missing.", path);
            }
        }

        var cli = Environment.GetEnvironmentVariable("SWLOR_CLI_DLL");
        if (string.IsNullOrWhiteSpace(cli) || !File.Exists(cli))
            throw new InvalidOperationException("SWLOR_CLI_DLL must point to the already-built production SWLOR.CLI.dll.");
        var packedPath = Path.Combine(moduleRoot, areaResRef + "_editor_acceptance.mod");
        if (File.Exists(packedPath)) throw new IOException("Refusing to overwrite an existing acceptance MOD.");
        var packWatch = Stopwatch.StartNew();
        var packInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = moduleRoot,
        };
        packInfo.ArgumentList.Add(Path.GetFullPath(cli));
        packInfo.ArgumentList.Add("--pack");
        packInfo.ArgumentList.Add(packedPath);
        packInfo.ArgumentList.Add("--no-prompt");
        using var pack = Process.Start(packInfo) ?? throw new InvalidOperationException("The existing CLI packer process did not start.");
        var packStdout = pack.StandardOutput.ReadToEndAsync();
        var packStderr = pack.StandardError.ReadToEndAsync();
        var logPath = Path.Combine(runRoot, "cli-pack.log");
        try
        {
            using var deadline = new CancellationTokenSource(Deadline);
            await pack.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!pack.HasExited)
        {
            pack.Kill(entireProcessTree: true);
            await pack.WaitForExitAsync();
            var timeoutStdout = await packStdout;
            var timeoutStderr = await packStderr;
            await File.WriteAllTextAsync(logPath, $"CLI pack timed out after {Deadline}.{Environment.NewLine}--- stdout ---{Environment.NewLine}{timeoutStdout}{Environment.NewLine}--- stderr ---{Environment.NewLine}{timeoutStderr}");
            throw new TimeoutException($"The owned CLI pack process exceeded {Deadline}; output retained at {logPath}.");
        }
        var stdout = await packStdout;
        var stderr = await packStderr;
        await File.WriteAllTextAsync(logPath, $"ExitCode={pack.ExitCode}{Environment.NewLine}--- stdout ---{Environment.NewLine}{stdout}{Environment.NewLine}--- stderr ---{Environment.NewLine}{stderr}");
        var packMilliseconds = packWatch.ElapsedMilliseconds;
        if (pack.ExitCode != 0) throw new InvalidOperationException($"CLI ModulePacker failed ({pack.ExitCode}); output retained at {logPath}.");

        var packedHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        var converterHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        var converterEvidenceRoot = Path.Combine(runRoot, "production-gff-equivalence");
        Directory.CreateDirectory(converterEvidenceRoot);
        var gffConverter = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(cli))!, "nwn_gff.exe");
        if (!File.Exists(gffConverter)) throw new FileNotFoundException("The production SWLOR GFF converter is missing beside the CLI.", gffConverter);
        using (var archive = ErfArchive.Open(packedPath))
        {
            foreach (var (extension, type) in new[]
            {
                ("are", NwnResourceType.Are), ("git", NwnResourceType.Git), ("gic", NwnResourceType.Gic),
            })
            {
                if (extension == "gic" && baselineGicHash is null) continue;
                var entry = archive.Entries.SingleOrDefault(item => item.ResRef.Value == areaResRef && item.Type == type)
                    ?? throw new InvalidDataException($"The CLI-packed MOD omitted {areaResRef}.{extension}.");
                var packed = archive.ReadAllBytes(entry);
                var savedJson = sourcePaths[extension];
                var converterGff = Path.Combine(converterEvidenceRoot, $"{areaResRef}.{extension}");
                var decodedJson = converterGff + ".packed.json";
                var packedGff = Path.Combine(converterEvidenceRoot, $"{areaResRef}-packed.{extension}");
                await RunGffConverterAsync(gffConverter, converterEvidenceRoot,
                    ["-l", "json", "-i", savedJson, "-o", converterGff, "-k", "gff"],
                    converterGff + ".encode.log");
                var productionBytes = await File.ReadAllBytesAsync(converterGff);
                CollectionAssertEqual(productionBytes, packed, $"Packed {extension} bytes differ from production nwn_gff conversion of the saved document.");
                await File.WriteAllBytesAsync(packedGff, packed);
                await RunGffConverterAsync(gffConverter, converterEvidenceRoot,
                    ["-i", packedGff, "-o", decodedJson, "-p"], decodedJson + ".decode.log");
                CollectionAssertEqual(CanonicalJsonBytes(savedJson), CanonicalJsonBytes(decodedJson),
                    $"Packed {extension} decoded tree differs from the complete saved document.");
                packedHashes.Add(extension, Hash(packed));
                converterHashes.Add(extension, Hash(productionBytes));
            }
        }

        var seedPreservation = await VerifySeedAreaBaselineAsync(moduleRoot, packedPath, seedCreatureListHash, runRoot);
        var savedGicHash = OptionalHashFile(gicPath);
        var savedGicStableHash = OptionalGicStableHash(gicPath);
        var savedGicDoorCount = GicDoorCount(gicPath);
        var evidence = new
        {
            Schema = "swlor.shared-area-official-client-fixture.v1",
            SeedAreaPreservation = seedPreservation,
            Area = areaResRef,
            ModuleRoot = Path.GetFullPath(moduleRoot),
            PackedModule = Path.GetFullPath(packedPath),
            TileSetResRef = editor.TilesetResRef,
            TileModel = "ttr01_h18_01",
            TileId = 75,
            DoorTemplate = doorTemplate,
            DoorTemplateNativeSha256 = selectedDoorNativeSha256,
            DoorTemplateInitialAppearance = $"Generic/{selectedDoorGenericId}",
            DoorTemplateInitiallyLocked = selectedDoorLocked,
            DoorTag = section.DetailTag,
            DoorAppearance = "Specific:1",
            PointerPath = "shared Palette Place command -> production viewport pointer handler -> TileCellPicked/PlacementPointPicked",
            PhaseMilliseconds = new { ResourceIndex = resourceIndexMilliseconds, AreaLoadToSceneReady = sceneReadyMilliseconds, FirstQualifiedWindowFrame = firstQualifiedWindowFrameMilliseconds, TilePlacement = tilePlacementMilliseconds, DoorPlacement = doorPlacementMilliseconds, Save = saveMilliseconds, ModulePack = packMilliseconds },
            BaselineAreSha256 = baselineAreHash,
            BaselineGitSha256 = baselineGitHash,
            SourceSha256 = sourcePaths.ToDictionary(pair => pair.Key, pair => OptionalHashFile(pair.Value)),
            SourceCanonicalSha256 = sourcePaths.ToDictionary(pair => pair.Key, pair => OptionalCanonicalJsonHash(pair.Value)),
            PreservedCreatureListSha256 = baselineCreatureListHash,
            SeedCreatureListSha256 = seedCreatureListHash,
            PreservedGicSha256 = baselineGicHash,
            AreaDocumentChanged = baselineAreHash != HashFile(sourcePaths["are"]),
            InstanceDocumentChanged = baselineGitHash != HashFile(sourcePaths["git"]),
            CreatureListPreserved = baselineCreatureListHash == CreatureListHash(moduleRoot, areaResRef),
            GicUnchangedExceptDoorList = baselineGicHash is not null && baselineGicHash == savedGicStableHash,
            NewAreaGicExistedBeforeEdits = baselineGicHash is not null,
            PackedResourceSha256 = packedHashes,
            ProductionGffConverterSha256 = converterHashes,
            PackedDecodedTreesMatchSavedDocuments = true,
            PackedModuleSha256 = HashFile(packedPath),
            LoadedAssemblies = LoadedPackageEvidence(),
        };
        if (baselineAreHash == HashFile(sourcePaths["are"]) || baselineGitHash == HashFile(sourcePaths["git"]))
            throw new InvalidDataException("The area and instance documents were not both changed by the editor actions.");
        if (baselineCreatureListHash != CreatureListHash(moduleRoot, areaResRef)
            || baselineGicHash is null
            || baselineGicHash != savedGicStableHash
            || baselineGicDoorCount != 0
            || savedGicDoorCount != 1)
            throw new InvalidDataException("The area edits changed its creature list, modified GIC fields outside Door List, or failed to record the placed door in GIC.");

        var evidencePath = Path.Combine(runRoot, "editor-acceptance.json");
        if (File.Exists(evidencePath)) throw new IOException("Refusing to overwrite acceptance evidence.");
        await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Shared area editor acceptance saved and packed: {evidencePath}; exact existing native resources ARE/GIT/GIC={packedHashes.Count}/3; pack stdout={stdout.Trim()}");
    }

    public static async Task VerifyReopenAsync(
        AreaEditorViewModel editor,
        string moduleRoot,
        string runRoot,
        long resourceIndexMilliseconds,
        long sceneReadyMilliseconds,
        long? firstQualifiedWindowFrameMilliseconds)
    {
        var areaResRef = editor.AreaResRef;
        var arePath = Path.Combine(moduleRoot, "are", areaResRef + ".are.json");
        var gitPath = Path.Combine(moduleRoot, "git", areaResRef + ".git.json");
        var gicPath = Path.Combine(moduleRoot, "gic", areaResRef + ".gic.json");
        var sourceAreHash = HashFile(arePath);
        var sourceGitHash = HashFile(gitPath);
        var sourceGicHash = OptionalHashFile(gicPath);
        var creatureListHash = CreatureListHash(moduleRoot, areaResRef);
        var gicHash = OptionalHashFile(gicPath);
        var scene = editor.AreaScene ?? throw new InvalidOperationException("Fresh workspace reopen produced no area scene.");
        if (!scene.Tiles.Any(tile => tile.TileId == 75))
            throw new InvalidDataException("Fresh workspace reopen did not read the editor-saved tile 75.");
        var door = scene.Instances.SingleOrDefault(item => item.Kind == InstanceMarkerKind.Door)
            ?? throw new InvalidDataException("Fresh workspace reopen did not read exactly one saved door placement.");
        var section = editor.Sections.Single(item => item.UsesDoorEditor);
        section.SelectedRow = section.Rows.Single(row => row.Index == door.ListIndex);
        if (section.DoorEditor is null || section.DetailTag != "sw_editor_door")
            throw new InvalidDataException("Fresh workspace reopen did not restore the saved door tag/detail state.");
        await WaitForAsync(() => section.DoorEditor is not null
            && section.DoorEditor.Appearance.Tiles.Any(item => item.IsCurrent && item.Option.Id.Value == "Specific:1"),
            "the saved Specific:1 door appearance to load in a fresh workspace");
        var baseline = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE")
            ?? throw new InvalidOperationException("Reopen stage requires the edit-stage evidence path.");
        if (!File.Exists(baseline)) throw new FileNotFoundException("The edit-stage evidence is missing.", baseline);
        using var prior = JsonDocument.Parse(await File.ReadAllBytesAsync(baseline));
        var priorSourceHashes = prior.RootElement.GetProperty("SourceSha256");
        var priorCanonicalHashes = prior.RootElement.GetProperty("SourceCanonicalSha256");
        var seedCreatureListHash = prior.RootElement.GetProperty("SeedCreatureListSha256").GetString()
            ?? throw new InvalidDataException("The edit evidence has no original seed creature-list hash.");
        if (prior.RootElement.GetProperty("PreservedCreatureListSha256").GetString() != creatureListHash
            || priorCanonicalHashes.GetProperty("are").GetString() != CanonicalJsonHash(arePath)
            || priorCanonicalHashes.GetProperty("git").GetString() != CanonicalJsonHash(gitPath)
            || ReadNullableString(priorCanonicalHashes.GetProperty("gic")) != OptionalCanonicalJsonHash(gicPath))
            throw new InvalidDataException("The fresh workspace changed the complete saved ARE/GIT/GIC trees or original creature list.");
        var packedPath = Path.GetFullPath(prior.RootElement.GetProperty("PackedModule").GetString()
            ?? throw new InvalidDataException("The edit evidence has no packed module path."));
        if (!File.Exists(packedPath)) throw new FileNotFoundException("The edit-stage packed module is missing.", packedPath);
        using (var archive = ErfArchive.Open(packedPath))
        {
            VerifyPackedBytes(archive, areaResRef, prior.RootElement.GetProperty("PackedResourceSha256"));
        }
        var seedPreservation = await VerifySeedAreaBaselineAsync(moduleRoot, packedPath, seedCreatureListHash, runRoot);
        var evidence = new
        {
            Schema = "swlor.shared-area-fresh-reopen.v1",
            SeedAreaPreservation = seedPreservation,
            Area = areaResRef,
            SceneTiles = scene.Tiles.Count,
            Tile75Restored = true,
            DoorCount = scene.Instances.Count(item => item.Kind == InstanceMarkerKind.Door),
            DoorTag = section.DetailTag,
            DoorAppearance = section.DoorEditor.Appearance.Tiles.Single(item => item.IsCurrent).Option.Id.Value,
            DoorListIndex = door.ListIndex,
            ReopenedJsonSha256 = new { Are = sourceAreHash, Git = sourceGitHash, Gic = sourceGicHash },
            ReopenedCanonicalTreeSha256 = new { Are = CanonicalJsonHash(arePath), Git = CanonicalJsonHash(gitPath), Gic = OptionalCanonicalJsonHash(gicPath) },
            ReopenedPackedBytesVerified = true,
            PreservedCreatureListSha256 = creatureListHash,
            PreservedGicSha256 = gicHash,
            PhaseMilliseconds = new { ResourceIndex = resourceIndexMilliseconds, FreshAreaLoadToSceneReady = sceneReadyMilliseconds, FirstQualifiedWindowFrame = firstQualifiedWindowFrameMilliseconds },
        };
        var path = Path.Combine(runRoot, "fresh-reopen.json");
        if (File.Exists(path)) throw new IOException("Refusing to overwrite fresh workspace reopen evidence.");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task<object> VerifySeedAreaBaselineAsync(string moduleRoot, string packedPath, string expectedCreatureListHash, string runRoot)
    {
        const string seedAreaResRef = "xm_check_area";
        var seedArePath = Path.Combine(moduleRoot, "are", seedAreaResRef + ".are.json");
        var seedGitPath = Path.Combine(moduleRoot, "git", seedAreaResRef + ".git.json");
        var seedGicPath = Path.Combine(moduleRoot, "gic", seedAreaResRef + ".gic.json");
        var expectedAreJsonHash = RequireEnvironmentHash("SWLOR_AREA_EDITOR_CAPTURE_SEED_ARE_SHA256");
        var expectedGitJsonHash = RequireEnvironmentHash("SWLOR_AREA_EDITOR_CAPTURE_SEED_GIT_SHA256");
        if (HashFile(seedArePath) != expectedAreJsonHash || HashFile(seedGitPath) != expectedGitJsonHash)
            throw new InvalidDataException("The original seed area JSON changed during acceptance editing.");
        var actualCreatureListHash = CreatureListHash(moduleRoot, seedAreaResRef);
        if (actualCreatureListHash != expectedCreatureListHash)
            throw new InvalidDataException("The original seed area creature list changed during acceptance editing.");
        if (File.Exists(seedGicPath))
            throw new InvalidDataException("The original seed area unexpectedly gained a GIC resource.");

        var seedBaselinePath = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_SEED_BASELINE_MOD")
            ?? throw new InvalidOperationException("The CLI-packed unedited seed baseline path is required.");
        var originalSeedPath = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_SEED_MOD")
            ?? throw new InvalidOperationException("The original seed MOD path is required for byte-preservation checks.");
        var cli = Environment.GetEnvironmentVariable("SWLOR_CLI_DLL")
            ?? throw new InvalidOperationException("The production CLI path is required for native resource checks.");
        var gffConverter = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(cli))!, "nwn_gff.exe");
        if (!File.Exists(gffConverter)) throw new FileNotFoundException("The production SWLOR GFF converter is missing beside the CLI.", gffConverter);
        using var originalSeed = ErfArchive.Open(originalSeedPath);
        using var seedBaseline = ErfArchive.Open(seedBaselinePath);
        using var editedModule = ErfArchive.Open(packedPath);
        var nativeResourceHashes = new Dictionary<string, object>(StringComparer.Ordinal);
        var semanticRoot = Path.Combine(runRoot, "seed-semantic-equivalence");
        Directory.CreateDirectory(semanticRoot);
        foreach (var (extension, type) in new[]
        {
            ("are", NwnResourceType.Are), ("git", NwnResourceType.Git),
        })
        {
            var seedEntry = seedBaseline.Entries.SingleOrDefault(item => item.ResRef.Value == seedAreaResRef && item.Type == type)
                ?? throw new InvalidDataException($"The baseline packed MOD lacks the original seed area {extension} resource.");
            var editedEntry = editedModule.Entries.SingleOrDefault(item => item.ResRef.Value == seedAreaResRef && item.Type == type)
                ?? throw new InvalidDataException($"The edited packed MOD lacks the original seed area {extension} resource.");
            var originalEntry = originalSeed.Entries.SingleOrDefault(item => item.ResRef.Value == seedAreaResRef && item.Type == type)
                ?? throw new InvalidDataException($"The original seed MOD lacks the original seed area {extension} resource.");
            var original = originalSeed.ReadAllBytes(originalEntry);
            var expected = seedBaseline.ReadAllBytes(seedEntry);
            var actual = editedModule.ReadAllBytes(editedEntry);
            CollectionAssertEqual(expected, actual, $"The unchanged seed area's packed native {extension} bytes changed during editing.");
            var originalPath = Path.Combine(semanticRoot, $"original.{extension}");
            var decodedPath = Path.Combine(semanticRoot, $"original.{extension}.json");
            await File.WriteAllBytesAsync(originalPath, original);
            await RunGffConverterAsync(gffConverter, semanticRoot, ["-i", originalPath, "-o", decodedPath, "-p"], decodedPath + ".decode.log");
            var sourceJson = extension == "are" ? seedArePath : seedGitPath;
            CollectionAssertEqual(CanonicalJsonBytes(sourceJson), CanonicalJsonBytes(decodedPath),
                $"The original seed area's complete {extension} tree differs from its native module resource.");
            nativeResourceHashes.Add(extension, new
            {
                OriginalModuleSha256 = Hash(original),
                BaselinePackedSha256 = Hash(expected),
                EditedPackedSha256 = Hash(actual),
                OriginalAndProductionPackedBytesEqual = original.AsSpan().SequenceEqual(expected),
                FullDecodedTreeMatchesSeedJson = true,
            });
        }
        var originalGic = originalSeed.Entries.SingleOrDefault(item => item.ResRef.Value == seedAreaResRef && item.Type == NwnResourceType.Gic);
        var baselineGic = seedBaseline.Entries.SingleOrDefault(item => item.ResRef.Value == seedAreaResRef && item.Type == NwnResourceType.Gic);
        var editedGic = editedModule.Entries.SingleOrDefault(item => item.ResRef.Value == seedAreaResRef && item.Type == NwnResourceType.Gic);
        if (originalGic is not null || baselineGic is not null || editedGic is not null)
            throw new InvalidDataException("The original seed's GIC absence was not preserved in the packed module.");

        return new
        {
            Area = seedAreaResRef,
            AreJsonSha256 = expectedAreJsonHash,
            GitJsonSha256 = expectedGitJsonHash,
            CreatureListSha256 = expectedCreatureListHash,
            GicAbsentInOriginalSeedBaselineAndOutput = true,
            OriginalSeedModSha256 = HashFile(originalSeedPath),
            NativeResources = nativeResourceHashes,
            BaselineAndEditedPackedAreaBytesMatch = true,
        };
    }

    private static string RequireEnvironmentHash(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value is not { Length: 64 }) throw new InvalidOperationException($"{name} must be a 64-character SHA256 from the owned seed manifest.");
        return value;
    }

    private static void VerifyPackedBytes(ErfArchive archive, string areaResRef, JsonElement expectedHashes)
    {
        VerifyPackedResource(archive, areaResRef, NwnResourceType.Are, ReadNullableString(expectedHashes.GetProperty("are")));
        VerifyPackedResource(archive, areaResRef, NwnResourceType.Git, ReadNullableString(expectedHashes.GetProperty("git")));
        VerifyPackedResource(archive, areaResRef, NwnResourceType.Gic, ReadNullableString(expectedHashes.GetProperty("gic")));
    }

    private static void VerifyPackedResource(ErfArchive archive, string areaResRef, NwnResourceType type, string? expectedHash)
    {
        var entry = archive.Entries.SingleOrDefault(item => item.ResRef.Value == areaResRef && item.Type == type);
        if (expectedHash is null)
        {
            if (entry is not null) throw new InvalidDataException($"The fresh workspace introduced {areaResRef}.{type} into the packed MOD.");
            return;
        }
        if (entry is null) throw new InvalidDataException($"The CLI-packed MOD omitted {areaResRef}.{type}.");
        if (Hash(archive.ReadAllBytes(entry)) != expectedHash)
            throw new InvalidDataException($"The reopened native {type} bytes no longer match the CLI-packed MOD.");
    }

    private static string? OptionalCanonicalJsonHash(string path) => File.Exists(path) ? CanonicalJsonHash(path) : null;
    private static string CanonicalJsonHash(string path) => Hash(CanonicalJsonBytes(path));

    private static string? ReadNullableString(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null ? null : value.GetString();

    private static string CreatureListHash(string moduleRoot, string areaResRef)
    {
        using var json = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(moduleRoot, "git", areaResRef + ".git.json")));
        var root = json.RootElement;
        if (root.TryGetProperty("root", out var wrappedRoot)) root = wrappedRoot;
        var fields = root.TryGetProperty("fields", out var wrappedFields) ? wrappedFields : root;
        if (!fields.TryGetProperty("Creature List", out var creatureList))
            return Hash(System.Text.Encoding.UTF8.GetBytes("<Creature List absent>"));
        return Hash(CanonicalJsonBytes(creatureList));
    }

    private static async Task ClickWorldAsync(Window window, AreaViewportControl viewport, Vector3 world)
    {
        var picked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnTilePicked(int column, int row) => picked.TrySetResult();
        void OnPlacementPicked(PlacementPick pick) => picked.TrySetResult();
        viewport.TileCellPicked += OnTilePicked;
        viewport.PlacementPointPicked += OnPlacementPicked;
        try
        {
            var state = viewport.CaptureViewportState() ?? throw new InvalidOperationException("The rendered area has no camera state.");
            var width = (int)viewport.Bounds.Width;
            var height = (int)viewport.Bounds.Height;
            if (width <= 0 || height <= 0) throw new InvalidOperationException("The actual scene viewport has no drawable size.");
            var eye = state.Target + AreaCameraMath.OrbitEyeOffset(state.Azimuth, state.Elevation, state.Distance);
            var view = Matrix4x4.CreateLookAt(eye, state.Target, Vector3.UnitZ);
            var projection = AreaCameraMath.CreateProjection(false, state.Distance, MathF.PI / 4,
                (float)width / height, AreaCameraMath.NearPlaneFor(state.Distance),
                MathF.Max(state.Distance, state.InitialDistance) * 25 + 100);
            var projected = Vector4.Transform(new Vector4(world, 1), view * projection);
            if (projected.W <= 0) throw new InvalidOperationException("The placement point is behind the scene camera.");
            var point = new Point((projected.X / projected.W + 1) * width / 2,
                (1 - projected.Y / projected.W) * height / 2);
            if (!viewport.Bounds.Contains(point)) throw new InvalidOperationException($"The placement point is outside the viewport: {point}.");
            var root = (Visual?)viewport.GetVisualRoot() ?? throw new InvalidOperationException("The real shell visual root is absent.");
            var rootPoint = viewport.TranslatePoint(point, root) ?? throw new InvalidOperationException("The viewport point could not be mapped to the shell.");
            using var pointer = new Pointer(703, PointerType.Mouse, true);
            viewport.HandlePointerPressed(new(viewport, pointer, root, rootPoint, 1,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            viewport.HandlePointerReleased(new(viewport, pointer, root, rootPoint, 2,
                new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            await picked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            viewport.TileCellPicked -= OnTilePicked;
            viewport.PlacementPointPicked -= OnPlacementPicked;
        }
    }

    private static async Task WaitForAsync(Func<bool> ready, string operation)
    {
        using var deadline = new CancellationTokenSource(Deadline);
        try
        {
            while (!ready()) await Task.Delay(30, deadline.Token);
        }
        catch (OperationCanceledException exception) when (deadline.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for {operation}.", exception);
        }
    }

    private static object[] LoadedPackageEvidence() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => !assembly.IsDynamic && assembly.GetName().Name is "Nwn.Authoring" or "Nwn.Formats" or "Nwn.Preview" or "Nwn.Toolset.Avalonia")
        .Select(assembly => new
        {
            Name = assembly.GetName().Name,
            InformationalVersion = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion,
            Path = Path.GetFullPath(assembly.Location),
            Sha256 = HashFile(assembly.Location),
        }).Cast<object>().ToArray();

    private static async Task RunGffConverterAsync(string executable, string workingDirectory, IReadOnlyList<string> arguments, string logPath)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("The production nwn_gff process did not start.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            var timeoutStdout = await stdoutTask;
            var timeoutStderr = await stderrTask;
            await File.WriteAllTextAsync(logPath, $"Timed out after 60 seconds.{Environment.NewLine}--- stdout ---{Environment.NewLine}{timeoutStdout}{Environment.NewLine}--- stderr ---{Environment.NewLine}{timeoutStderr}");
            throw new TimeoutException($"The production GFF conversion exceeded 60 seconds; logs retained at {logPath}.");
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        await File.WriteAllTextAsync(logPath, $"ExitCode={process.ExitCode}{Environment.NewLine}--- stdout ---{Environment.NewLine}{stdout}{Environment.NewLine}--- stderr ---{Environment.NewLine}{stderr}");
        if (process.ExitCode != 0) throw new InvalidOperationException($"Production nwn_gff failed with {process.ExitCode}; logs retained at {logPath}.");
    }

    private static byte[] CanonicalJsonBytes(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        return CanonicalJsonBytes(document.RootElement);
    }

    private static byte[] CanonicalJsonBytes(JsonElement element)
    {
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output)) WriteCanonicalJson(element, writer);
        return output.ToArray();
    }

    private static void WriteCanonicalJson(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonicalJson(item, writer);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void CollectionAssertEqual(byte[] expected, byte[] actual, string message)
    {
        if (!expected.AsSpan().SequenceEqual(actual)) throw new InvalidDataException(message);
    }

    private static string? OptionalHashFile(string path) => File.Exists(path) ? HashFile(path) : null;
    private static string? OptionalGicStableHash(string path) => File.Exists(path) ? Hash(GicStableJsonBytes(path)) : null;
    private static int GicDoorCount(string path)
    {
        if (!File.Exists(path)) return 0;
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = document.RootElement;
        if (root.TryGetProperty("root", out var wrappedRoot)) root = wrappedRoot;
        var fields = root.TryGetProperty("fields", out var wrappedFields) ? wrappedFields : root;
        return fields.TryGetProperty("Door List", out var doors) && doors.TryGetProperty("value", out var value)
            ? value.GetArrayLength()
            : 0;
    }

    private static byte[] GicStableJsonBytes(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = document.RootElement;
        if (root.TryGetProperty("root", out var wrappedRoot)) root = wrappedRoot;
        var fields = root.TryGetProperty("fields", out var wrappedFields) ? wrappedFields : root;
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            foreach (var field in fields.EnumerateObject().Where(field => field.Name != "Door List").OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(field.Name);
                WriteCanonicalJson(field.Value, writer);
            }
            writer.WriteEndObject();
        }
        return output.ToArray();
    }

    private static string HashFile(string path) => Hash(File.ReadAllBytes(path));
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
