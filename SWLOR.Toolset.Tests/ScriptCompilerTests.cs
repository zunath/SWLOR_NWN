using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script.Compile;

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
            result.HasErrors.Should().BeFalse(result.Output);
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
    }
}
