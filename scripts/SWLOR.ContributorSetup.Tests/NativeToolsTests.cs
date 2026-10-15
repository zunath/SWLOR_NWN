using System.IO.Compression;
using System.Text.Json;
using NUnit.Framework;

namespace SWLOR.ContributorSetup.Tests;

public class NativeToolsTests
{
    private string _root = null!;
    internal static NativeToolConfiguration Configuration => new("2.1.2", "https://example.invalid/native.zip", "",
        new[] { "nwn_erf", "nwn_gff", "nwn_script_comp", "nwn_tlk" },
        new[] { "nwn_erf.exe", "nwn_gff.exe", "nwn_script_comp.exe", "pcre64.dll", "libnwnscriptcomp.dll" });

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("swlor-setup-tests-").FullName;
    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [TestCase("pcre64.dll")]
    [TestCase("libnwnscriptcomp.dll")]
    public void WindowsDoctorReportsTheMissingRuntimeLibrary(string missing)
    {
        var bundled = Path.Combine(_root, "tools", "SWLOR.CLI");
        Directory.CreateDirectory(bundled);
        foreach (var name in Configuration.WindowsFiles.Where(name => name != missing))
            File.WriteAllText(Path.Combine(bundled, name), name);
        var native = new NativeTools(_root, new CommandRunner(), Configuration, windows: true);
        Assert.That(native.CheckPrerequisites("Toolset"), Is.EqualTo(new[] { "Restore the bundled utility " + missing + " from Git." }));
    }

    [Test]
    public void LinuxCacheRequiresEveryLibraryAndItsOriginalHash()
    {
        var cache = Path.Combine(_root, "cache");
        Directory.CreateDirectory(cache);
        var files = Configuration.NativeNames.Concat(new[] { "libnwnscriptcomp.so", "libpcre.so.3", "libsqlite3.so.0" });
        foreach (var name in files)
        {
            var path = Path.Combine(cache, name);
            File.WriteAllText(path, name);
            if (OperatingSystem.IsLinux()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }
        var hashes = files.ToDictionary(name => name, name => NativeTools.Hash(Path.Combine(cache, name)));
        var marker = Path.Combine(cache, "native-tools.json");
        File.WriteAllText(marker, JsonSerializer.Serialize(new NativeToolReceipt(Configuration.Version, "", "", hashes)));
        var native = new NativeTools(_root, new CommandRunner(), Configuration, cache, windows: false);
        Assert.That(native.IsPrepared(), Is.True);
        File.WriteAllText(Path.Combine(cache, "libnwnscriptcomp.so"), "changed");
        Assert.That(native.IsPrepared(), Is.False);
        File.WriteAllText(Path.Combine(cache, "libnwnscriptcomp.so"), "libnwnscriptcomp.so");
        File.Delete(Path.Combine(cache, "libsqlite3.so.0"));
        Assert.That(native.IsPrepared(), Is.False);
    }

    [TestCase("{")]
    [TestCase("{}")]
    [TestCase("null")]
    public void MalformedCacheIsReportedAsUnprepared(string contents)
    {
        File.WriteAllText(Path.Combine(_root, "native-tools.json"), contents);
        var native = new NativeTools(_root, new CommandRunner(), Configuration, _root, windows: false);
        Assert.That(native.IsPrepared(), Is.False);
    }

    [Test]
    public void WrongArchiveHashIsRejectedBeforeWritingAnyTools()
    {
        var archive = Path.Combine(_root, "native.zip");
        File.WriteAllText(archive, "invalid download");
        var staging = Path.Combine(_root, "staging");
        var native = new NativeTools(_root, new CommandRunner(), Configuration, windows: false);
        Assert.Throws<InvalidDataException>(() => native.ExtractVerifiedArchive(archive, staging));
        Assert.That(Directory.Exists(staging), Is.False);
    }

    [Test]
    public void VerifiedArchiveExtractsOnlyExpectedFilesWithExecutableModes()
    {
        var archive = Path.Combine(_root, "native.zip");
        using (var bundle = ZipFile.Open(archive, ZipArchiveMode.Create))
        {
            foreach (var name in Configuration.NativeNames.Append("libnwnscriptcomp.so").Append("../outside"))
            {
                using var writer = new StreamWriter(bundle.CreateEntry(name).Open());
                writer.Write(name);
            }
        }
        var config = Configuration with { LinuxSha256 = NativeTools.Hash(archive) };
        var staging = Path.Combine(_root, "staging");
        var native = new NativeTools(_root, new CommandRunner(), config, windows: false);
        native.ExtractVerifiedArchive(archive, staging);
        Assert.That(File.Exists(Path.Combine(_root, "outside")), Is.False);
        Assert.That(File.ReadAllText(Path.Combine(staging, "nwn_erf")), Is.EqualTo("nwn_erf"));
        if (OperatingSystem.IsLinux())
            Assert.That(File.GetUnixFileMode(Path.Combine(staging, "nwn_erf")) & UnixFileMode.UserExecute, Is.Not.Zero);
    }

    [Test]
    public async Task InvalidExistingCacheIsPreservedWithoutDownloading()
    {
        var cache = Path.Combine(_root, "cache");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "keep"), "in use");
        var native = new NativeTools(_root, new CommandRunner(), Configuration, cache, windows: false);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await native.PrepareAsync(CancellationToken.None));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(cache, "keep")), Is.EqualTo("in use"));
    }

    [Test]
    public void NativeEnvironmentIsScopedToChildAndPreservesSearchPaths()
    {
        var parentPath = Environment.GetEnvironmentVariable("PATH");
        var parentLibraries = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
        var native = new NativeTools(_root, new CommandRunner(), Configuration, windows: false);
        var environment = native.EnvironmentFor(_root);
        Assert.That(environment["SWLOR_NATIVE_TOOLS"], Is.EqualTo(_root));
        Assert.That(environment["PATH"], Is.EqualTo(_root + Path.PathSeparator + parentPath));
        Assert.That(environment["LD_LIBRARY_PATH"], Is.EqualTo(_root + (string.IsNullOrEmpty(parentLibraries) ? "" : ":" + parentLibraries)));
        Assert.That(Environment.GetEnvironmentVariable("PATH"), Is.EqualTo(parentPath));
        Assert.That(Environment.GetEnvironmentVariable("LD_LIBRARY_PATH"), Is.EqualTo(parentLibraries));
    }
}
