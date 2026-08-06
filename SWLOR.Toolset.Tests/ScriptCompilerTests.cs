using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script.Compile;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The compiler wrapper, exercised against the real vendored binary where it is present.
    /// The byte-identity gate that qualified that binary is recorded in WORKLOG.md (WPS4.1-spike);
    /// what is pinned here is that the wrapper drives it correctly and reads its output.
    /// </summary>
    public class ScriptCompilerTests
    {
        private static string CompilerPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "tools", "SWLOR.CLI", "nwn_script_comp.exe");

        private static string NssDirectory => Path.Combine(CorpusLocator.ModuleDirectory, "nss");

        private static string EngineHeaderDirectory => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN");

        private string _staging = null!;

        [SetUp]
        public void SetUp()
        {
            // nwn_script_comp resolves the language spec as "nwscript.nss"; the in-repo header is
            // version-stamped, so it is staged under the plain name the compiler looks for.
            _staging = Path.Combine(Path.GetTempPath(), "swlor_nsscomp_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_staging);

            var header = Directory.Exists(EngineHeaderDirectory)
                ? Directory.EnumerateFiles(EngineHeaderDirectory, "nwscript*.nss").FirstOrDefault()
                : null;

            if (header != null)
                File.Copy(header, Path.Combine(_staging, "nwscript.nss"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_staging))
                Directory.Delete(_staging, recursive: true);
        }

        private ScriptCompiler Compiler() =>
            new(CompilerPath, new[] { NssDirectory, _staging });

        [Test]
        public void MissingCompiler_ReportsUnavailableRatherThanThrowing()
        {
            var compiler = new ScriptCompiler(Path.Combine(_staging, "not_here.exe"), Array.Empty<string>());

            compiler.IsAvailable.Should().BeFalse();
            var result = compiler.CompileAsync(Path.Combine(_staging, "x.nss")).GetAwaiter().GetResult();
            result.Succeeded.Should().BeFalse();
            result.Output.Should().Contain("not found");
        }

        [Test]
        public async Task BuildAllWithoutACompilerReportsAFailure()
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            var service = new ScriptCompileService(
                context,
                log,
                compilerPathOverride: Path.Combine(_staging, "not_here.exe"));

            var result = await service.BuildAllAsync();

            result.Ran.Should().BeFalse();
            result.Compiled.Should().Be(0);
            result.Failed.Should().Be(0);
        }

        [Test]
        public void EngineHeaderStagingRefreshesAfterTheRepositoryHeaderChanges()
        {
            var repository = Path.Combine(_staging, "Repository");
            Directory.CreateDirectory(Path.Combine(repository, "tools", "SWLOR.CLI"));
            var headerDirectory = Path.Combine(repository, "SWLOR.NWN.API", "NWN");
            Directory.CreateDirectory(headerDirectory);
            var module = Path.Combine(repository, "Module");
            foreach (var folder in new[] { "are", "utc", "nss", "ncs" })
                Directory.CreateDirectory(Path.Combine(module, folder));

            var sourceHeader = Path.Combine(headerDirectory, "nwscript-test.nss");
            const string firstHeader = "int FirstVersion();\r\n";
            const string secondHeader = "int SecondVersion();\r\n";
            File.WriteAllText(sourceHeader, firstHeader);

            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(module);
            var service = new ScriptCompileService(context, log);
            var stageHeader = typeof(ScriptCompileService).GetMethod(
                "StageEngineHeader",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            string? firstDirectory = null;
            string? secondDirectory = null;
            try
            {
                firstDirectory = (string)stageHeader.Invoke(service, null)!;
                File.ReadAllText(Path.Combine(firstDirectory, "nwscript.nss"))
                    .Should().Be(firstHeader);

                File.WriteAllText(sourceHeader, secondHeader);

                secondDirectory = (string)stageHeader.Invoke(service, null)!;
                secondDirectory.Should().NotBe(
                    firstDirectory,
                    "each checkout and header generation must have immutable compiler input");
                File.ReadAllText(Path.Combine(secondDirectory, "nwscript.nss"))
                    .Should().Be(secondHeader);
                File.ReadAllText(Path.Combine(firstDirectory, "nwscript.nss"))
                    .Should().Be(firstHeader, "a concurrent compiler may still be using it");
            }
            finally
            {
                foreach (var directory in new[] { firstDirectory, secondDirectory }
                             .OfType<string>()
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, recursive: true);
                }
            }
        }

        [Test]
        public async Task CheckOnly_CompilesAKnownGoodScriptAndWritesNothing()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var source = Path.Combine(_staging, "spike_ok.nss");
            await File.WriteAllTextAsync(source,
                "void main()\r\n{\r\n    object oPC = GetPCSpeaker();\r\n    SetName(oPC, \"\");\r\n}\r\n");

            var result = await Compiler().CompileAsync(source, checkOnly: true);

            result.Succeeded.Should().BeTrue(result.Output);
            result.HasErrors.Should().BeFalse();
            File.Exists(Path.ChangeExtension(source, ".ncs")).Should().BeFalse("-s must write nothing");
        }

        [Test]
        public async Task ABrokenScript_ReportsAnErrorWithItsLine()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var source = Path.Combine(_staging, "spike_bad.nss");
            await File.WriteAllTextAsync(source,
                "void main()\r\n{\r\n    ThisFunctionDoesNotExist();\r\n}\r\n");

            var result = await Compiler().CompileAsync(source, checkOnly: true);

            result.Succeeded.Should().BeFalse();
            result.HasErrors.Should().BeTrue();
            result.Diagnostics.Should().Contain(d => d.IsError && d.Line == 3);
        }

        [Test]
        public async Task AnIncludeWithNoMain_IsSkippedNotFailed()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var source = Path.Combine(_staging, "spike_inc.nss");
            await File.WriteAllTextAsync(source, "int Helper(int n) { return n + 1; }\r\n");

            var result = await Compiler().CompileAsync(source, checkOnly: true);

            // 19 of the module's 87 scripts are includes; treating them as failures would make the
            // Build All command permanently red.
            result.Succeeded.Should().BeTrue(result.Output);
            result.Skipped.Should().BeTrue(result.Output);
            result.HasErrors.Should().BeFalse(result.Output);
        }

        [Test]
        public async Task CompilingAnIncludeRebuildsItsTransitiveEntryPointDependents()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var module = Path.Combine(_staging, "Module");
            foreach (var folder in new[] { "are", "utc", "nss", "ncs" })
                Directory.CreateDirectory(Path.Combine(module, folder));

            var nss = Path.Combine(module, "nss");
            await File.WriteAllTextAsync(
                Path.Combine(nss, "shared_inc.nss"),
                "int SharedValue() { return 1; }\r\n");
            await File.WriteAllTextAsync(
                Path.Combine(nss, "middle_inc.nss"),
                "#include \"shared_inc\"\r\nint MiddleValue() { return SharedValue(); }\r\n");
            await File.WriteAllTextAsync(
                Path.Combine(nss, "entry_script.nss"),
                "#include \"middle_inc\"\r\nvoid main() { int value = MiddleValue(); }\r\n");

            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(module);
            var service = new ScriptCompileService(
                context, log, compilerPathOverride: CompilerPath);

            (await service.CompileAsync("entry_script")).Succeeded.Should().BeTrue();
            var output = Path.Combine(module, "ncs", "entry_script.ncs");
            var before = await File.ReadAllBytesAsync(output);

            await File.WriteAllTextAsync(
                Path.Combine(nss, "shared_inc.nss"),
                "int SharedValue() { return 2; }\r\n");

            var includeOutcome = await service.CompileAsync("shared_inc");

            includeOutcome.Succeeded.Should().BeTrue();
            (await File.ReadAllBytesAsync(output)).Should().NotEqual(before,
                "saving the transitive include must replace the dependent entry point's bytecode");
        }

        [Test]
        public async Task CompilingAScriptThatBecameAnIncludeRemovesItsObsoleteBytecode()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var module = Path.Combine(_staging, "Module");
            foreach (var folder in new[] { "are", "utc", "nss", "ncs" })
                Directory.CreateDirectory(Path.Combine(module, folder));

            await File.WriteAllTextAsync(
                Path.Combine(module, "nss", "former_entry.nss"),
                "int SharedValue() { return 1; }\r\n");
            var obsolete = Path.Combine(module, "ncs", "former_entry.ncs");
            await File.WriteAllBytesAsync(obsolete, new byte[] { 1, 2, 3, 4 });

            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(module);
            var service = new ScriptCompileService(
                context, log, compilerPathOverride: CompilerPath);

            var outcome = await service.CompileAsync("former_entry");

            outcome.Succeeded.Should().BeTrue();
            File.Exists(obsolete).Should().BeFalse(
                "the packer copies NCS files verbatim and must not ship behavior the source can no longer produce");
        }

        /// <summary>
        /// The compile writes to a transaction-unique temp file beside the canonical .ncs and only
        /// installs it via <c>File.Move(..., overwrite: true)</c> once the compiler reports success.
        /// A crash or kill mid-write then leaves a stray temp file, never a half-written canonical
        /// artifact - and a clean run must leave no such debris behind either.
        /// </summary>
        [Test]
        public async Task ASuccessfulCompileLeavesNoTemporaryArtifactsBehind()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var module = Path.Combine(_staging, "Module");
            foreach (var folder in new[] { "are", "utc", "nss", "ncs" })
                Directory.CreateDirectory(Path.Combine(module, folder));

            await File.WriteAllTextAsync(
                Path.Combine(module, "nss", "spike_atomic.nss"),
                "void main() { }\r\n");

            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(module);
            var service = new ScriptCompileService(context, log, compilerPathOverride: CompilerPath);

            var outcome = await service.CompileAsync("spike_atomic");

            outcome.Succeeded.Should().BeTrue();
            var ncsDirectory = Path.Combine(module, "ncs");
            File.Exists(Path.Combine(ncsDirectory, "spike_atomic.ncs")).Should().BeTrue();
            Directory.EnumerateFiles(ncsDirectory).Select(Path.GetFileName)
                .Should().BeEquivalentTo(
                    [".script-staleness-cache.json", "spike_atomic.ncs"],
                    "a successful compile may persist its fingerprint cache but must not leave its staging file behind");
        }

        /// <summary>
        /// The previous valid artifact must survive a failed recompile untouched - it is compiled to a
        /// temp path first, so a compile error (or, in production, a crash mid-write) never overwrites
        /// it with a partial file that <c>ScriptStalenessScanner</c> would then read as newer than its
        /// source.
        /// </summary>
        [Test]
        public async Task AFailedRecompileLeavesThePreviousArtifactUntouched()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var module = Path.Combine(_staging, "Module");
            foreach (var folder in new[] { "are", "utc", "nss", "ncs" })
                Directory.CreateDirectory(Path.Combine(module, folder));

            var source = Path.Combine(module, "nss", "spike_atomic2.nss");
            await File.WriteAllTextAsync(source, "void main() { }\r\n");

            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(module);
            var service = new ScriptCompileService(context, log, compilerPathOverride: CompilerPath);

            (await service.CompileAsync("spike_atomic2")).Succeeded.Should().BeTrue();
            var output = Path.Combine(module, "ncs", "spike_atomic2.ncs");
            var before = await File.ReadAllBytesAsync(output);

            await File.WriteAllTextAsync(source, "void main() { ThisFunctionDoesNotExist(); }\r\n");

            var outcome = await service.CompileAsync("spike_atomic2");

            outcome.Succeeded.Should().BeFalse();
            (await File.ReadAllBytesAsync(output)).Should().Equal(before,
                "a failed compile must never replace the last good bytecode");
            Directory.EnumerateFiles(Path.Combine(module, "ncs")).Select(Path.GetFileName)
                .Should().BeEquivalentTo(
                    [".script-staleness-cache.json", "spike_atomic2.ncs"],
                    "a failed compile may retain its fingerprint cache but must clean up its own staging file");
        }

        /// <summary>
        /// The fresh evidence from review: dmfi_dmw_inc.ncs survived every Build All since its source
        /// stopped declaring main(), because the per-source loop simply skipped an include and the
        /// packer went on shipping whatever ncs/ still had. Build All must purge the same-resref
        /// output whenever it classifies a source as an include, not only when that source is
        /// compiled directly through <see cref="ScriptCompileService.CompileAsync"/>.
        /// </summary>
        [Test]
        public async Task BuildAllRemovesBytecodeForASourceThatBecameAnInclude()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var module = Path.Combine(_staging, "Module");
            foreach (var folder in new[] { "are", "utc", "nss", "ncs" })
                Directory.CreateDirectory(Path.Combine(module, folder));

            // No main() - this is what an include looks like once its entry point has been removed.
            await File.WriteAllTextAsync(
                Path.Combine(module, "nss", "dmfi_dmw_inc.nss"),
                "int SharedValue() { return 1; }\r\n");
            var obsolete = Path.Combine(module, "ncs", "dmfi_dmw_inc.ncs");
            await File.WriteAllBytesAsync(obsolete, new byte[] { 1, 2, 3, 4 });

            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(module);
            var service = new ScriptCompileService(context, log, compilerPathOverride: CompilerPath);

            var outcome = await service.BuildAllAsync();

            outcome.Ran.Should().BeTrue();
            outcome.Purged.Should().Be(1);
            File.Exists(obsolete).Should().BeFalse(
                "Build All must not leave known-obsolete bytecode for the packer to ship verbatim");
        }

        /// <summary>
        /// The headline gate, reproduced as a test: recompiling a committed script must reproduce
        /// its committed .ncs byte for byte. dmfi_unact_nam03 is the smallest such script and uses
        /// no includes, so a failure here is unambiguous.
        /// </summary>
        [Test]
        public async Task RecompilingACommittedScript_ReproducesItsNcsByteForByte()
        {
            if (!File.Exists(CompilerPath))
                Assert.Ignore("vendored compiler not present");

            var source = Path.Combine(NssDirectory, "dmfi_unact_nam03.nss");
            var committed = Path.Combine(CorpusLocator.ModuleDirectory, "ncs", "dmfi_unact_nam03.ncs");
            if (!File.Exists(source) || !File.Exists(committed))
                Assert.Ignore("spike fixture not present in this corpus");

            var output = Path.Combine(_staging, "dmfi_unact_nam03.ncs");
            var result = await Compiler().CompileAsync(source, output);

            result.Succeeded.Should().BeTrue(result.Output);
            (await File.ReadAllBytesAsync(output))
                .Should().Equal(await File.ReadAllBytesAsync(committed),
                    "the vendored compiler must match whatever produced the committed artifacts");
        }

        // ---- output parsing, which needs no binary ----

        [Test]
        public void ParsesErrorsAndWarningsWithTheirPositions()
        {
            const string output = """
                E [2026-07-26T00:00:00] [1/1]
                C:\m\nss\bad.nss(23): ERROR: UNDEFINED IDENTIFIER (Foo)
                C:\m\nss\bad.nss(41): WARNING: something milder
                I [2026-07-26T00:00:00] 0 successful, 0 skipped, 1 errored
                """;

            var diagnostics = ScriptCompiler.ParseDiagnostics(output);

            diagnostics.Should().HaveCount(2);
            diagnostics[0].IsError.Should().BeTrue();
            diagnostics[0].Line.Should().Be(23);
            diagnostics[0].Message.Should().Contain("UNDEFINED IDENTIFIER");
            diagnostics[1].IsError.Should().BeFalse();
            diagnostics[1].Line.Should().Be(41);
        }

        [Test]
        public void ParsesDiagnosticPathsContainingSpaces()
        {
            const string output =
                @"C:\Users\First Last\SWLOR Workspace\Module\nss\bad.nss(23): ERROR: UNDEFINED IDENTIFIER";

            var diagnostic = ScriptCompiler.ParseDiagnostics(output).Should().ContainSingle().Subject;

            diagnostic.File.Should().Be(
                @"C:\Users\First Last\SWLOR Workspace\Module\nss\bad.nss");
            diagnostic.Line.Should().Be(23);
            diagnostic.IsError.Should().BeTrue();
        }

        [Test]
        public async Task CancellingACompilerWaitTerminatesTheChildProcess()
        {
            if (!OperatingSystem.IsWindows())
                Assert.Ignore("the Toolset compiler process is Windows-only");

            var powershell = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(powershell)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-NoProfile");
            process.StartInfo.ArgumentList.Add("-Command");
            process.StartInfo.ArgumentList.Add("Start-Sleep -Seconds 30");
            process.Start().Should().BeTrue();

            try
            {
                var waitMethod = typeof(ScriptCompiler).GetMethod(
                    "WaitForExitOrKillAsync",
                    BindingFlags.Static | BindingFlags.NonPublic)!;
                using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                var wait = (Task)waitMethod.Invoke(
                    null,
                    new object[] { process, cancellation.Token })!;

                Func<Task> act = () => wait;
                await act.Should().ThrowAsync<OperationCanceledException>();

                process.HasExited.Should().BeTrue(
                    "cancellation must not release the compiler gate while its child is still running");
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
        }

        [Test]
        public void RecognisesTheMissingGameInstallCase()
        {
            const string output = "NW_I0_GENERIC.nss(106): ERROR: FILE NOT FOUND";
            var result = new ScriptCompileResult(false, ScriptCompiler.ParseDiagnostics(output), output, false);

            // 16 module scripts reach base-game includes; that failure needs a different message
            // from an ordinary syntax error.
            ScriptCompiler.RequiresGameInstall(result).Should().BeTrue();
        }

        [Test]
        public void CleanOutput_YieldsNoDiagnostics()
        {
            ScriptCompiler.ParseDiagnostics("I [2026-07-26] 1 successful, 0 skipped, 0 errored")
                .Should().BeEmpty();
        }

        [Test]
        public void AnalysisMappingPreservesForeignIncludeFilenameAndDoesNotSquiggleTheEntryPoint()
        {
            var result = new ScriptCompileResult(
                false,
                new[]
                {
                    new ScriptDiagnostic(@"C:\module\nss\shared_inc.nss", 2, "include error", true),
                    new ScriptDiagnostic(@"C:\module\nss\entry.nss", 3, "entry error", true)
                },
                string.Empty,
                false);

            var diagnostics = ScriptCompileService.ToAnalysisDiagnostics(
                result,
                "line one\nline two\nline three\n",
                "entry");

            diagnostics[0].ResRef.Should().Be("shared_inc");
            diagnostics[0].Start.Should().Be(0);
            diagnostics[0].Length.Should().Be(0);
            diagnostics[1].ResRef.Should().BeNull();
            diagnostics[1].Start.Should().BeGreaterThan(0);
            diagnostics[1].Length.Should().BeGreaterThan(0);
        }
    }
}
