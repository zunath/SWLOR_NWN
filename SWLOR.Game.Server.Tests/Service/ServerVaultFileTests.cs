using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Service;

public class ServerVaultFileTests
{
    private static readonly Type VaultFile = typeof(Player).Assembly.GetType("SWLOR.Game.Server.Service.MigrationService.ServerVaultFile")!;

    private static object Call(string method, params object[] args) =>
        VaultFile.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args)!;

    private static string DecodeName(byte[] name) => (string)Call("DecodeName", name);
    private static byte[] EncodePath(string path) => (byte[])Call("EncodePath", path);
    private static string DisplayPath(string path) => (string)Call("DisplayPath", path);

    // "indéshar.bic" as the game names it: Windows-1252, where é is the single byte 0xE9.
    private static readonly byte[] Windows1252Name = { (byte)'i', (byte)'n', (byte)'d', 0xE9, (byte)'s', (byte)'h', (byte)'a', (byte)'r', (byte)'.', (byte)'b', (byte)'i', (byte)'c' };

    [Test]
    public void ValidUtf8NamesKeepTheStringDotNetGivesThem()
    {
        var name = Encoding.UTF8.GetBytes("thorbjörn-😀.bic");
        DecodeName(name).Should().Be("thorbjörn-😀.bic");
        EncodePath(DecodeName(name)).Should().Equal(name);
        DisplayPath("thorbjörn-😀.bic").Should().Be("thorbjörn-😀.bic");
    }

    [Test]
    public void Windows1252NamesTurnBackIntoTheSameBytes()
    {
        var decoded = DecodeName(Windows1252Name);
        EncodePath(decoded).Should().Equal(Windows1252Name);
        DisplayPath(decoded).Should().Be("indéshar.bic", "logs show the letter the player typed");

        // A Windows-1252 name inside a directory whose name is valid UTF-8 keeps both parts.
        var path = "/vault/Jörg/" + decoded;
        EncodePath(path).Should().Equal(Encoding.UTF8.GetBytes("/vault/Jörg/").Concat(Windows1252Name));
    }

    [Test]
    public void Windows1252NamedFilesCanBeListedReadAndReplacedOnLinux()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Ignore("Linux stores file names as raw bytes; other platforms use .NET directly.");

        var root = Path.Combine(Path.GetTempPath(), "swlor-vault-" + Guid.NewGuid().ToString("N"));
        try
        {
            var vault = Path.Combine(root, "servervault");
            var directory = Path.Combine(vault, "UXTEST01");
            Call("CreateDirectory", directory);
            var path = Path.Combine(directory, DecodeName(Windows1252Name));
            Call("WriteAllBytes", path, new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(directory, "plain.bic"), new byte[] { 4 });
            File.WriteAllBytes(Path.Combine(directory, "plain.bic.upgrade"), new byte[] { 5 });

            // .NET lists the file under a name it cannot open; this is what failed on the live server.
            var dotNetName = Directory.EnumerateFiles(vault, "*.bic", SearchOption.AllDirectories).Single(file => file.Contains('�'));
            FluentActions.Invoking(() => File.ReadAllBytes(dotNetName)).Should().Throw<FileNotFoundException>();

            var files = ((IEnumerable<string>)Call("EnumerateFiles", vault, ".bic")).ToList();
            files.Should().BeEquivalentTo(new[] { Path.Combine(directory, "plain.bic"), path }, "pending .upgrade files are not character files");
            ((byte[])Call("ReadAllBytes", path)).Should().Equal(1, 2, 3);
            ((bool)Call("Exists", path)).Should().BeTrue();
            ((bool)Call("Exists", directory)).Should().BeFalse("a directory is not a file");

            var backup = Path.Combine(root, "servervault_backups", "UXTEST01", DecodeName(Windows1252Name));
            Call("CreateDirectory", Path.GetDirectoryName(backup)!);
            Call("Copy", path, backup);
            ((byte[])Call("ReadAllBytes", backup)).Should().Equal(1, 2, 3);
            FluentActions.Invoking(() => Call("Copy", path, backup)).Should().Throw<TargetInvocationException>()
                .WithInnerException<IOException>("a backup is never overwritten");

            var pending = path + ".upgrade";
            Call("WriteAllBytes", pending, new byte[] { 9 });
            Call("Move", pending, path);
            ((byte[])Call("ReadAllBytes", path)).Should().Equal(9);
            ((bool)Call("Exists", pending)).Should().BeFalse();

            Call("Delete", path);
            Call("Delete", path);
            ((bool)Call("Exists", path)).Should().BeFalse();
            FluentActions.Invoking(() => Call("ReadAllBytes", path)).Should().Throw<TargetInvocationException>()
                .WithInnerException<FileNotFoundException>().WithMessage("*indéshar.bic*");
        }
        finally
        {
            // .NET cannot delete what it cannot name, so the raw-named files go first.
            foreach (var file in (IEnumerable<string>)Call("EnumerateFiles", root, ".bic"))
                Call("Delete", file);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
