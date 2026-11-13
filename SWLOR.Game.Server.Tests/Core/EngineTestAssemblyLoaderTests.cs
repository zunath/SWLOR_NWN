using System.Reflection;
using System.Runtime.Loader;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// <see cref="EngineTestAssemblyLoader"/> (P4-16): the engine-test assembly is loaded into the generation's own load
/// context from bytes, so the file is not held open and the test code leaves with the generation.
/// </summary>
[TestFixture]
public class EngineTestAssemblyLoaderTests
{
    private string _folder = null!;

    [SetUp]
    public void CreateFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "engine-test-loader-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void DeleteFolder()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    private string CopyEngineTestAssembly(string folder)
    {
        Directory.CreateDirectory(folder);
        var source = Assembly.Load(new AssemblyName(EngineTestAssemblyLoader.AssemblyName)).Location;
        var target = Path.Combine(folder, EngineTestAssemblyLoader.AssemblyName + ".dll");
        File.Copy(source, target);
        return target;
    }

    private sealed class CollectibleContext : AssemblyLoadContext
    {
        public CollectibleContext() : base("engine-test-loader-test", true)
        {
        }
    }

    [Test]
    public void FindPath_returns_the_first_directory_that_has_the_assembly_and_lists_what_it_probed()
    {
        var withAssembly = Path.Combine(_folder, "second");
        var path = CopyEngineTestAssembly(withAssembly);
        var probed = new List<string>();

        var found = EngineTestAssemblyLoader.FindPath(new[] { Path.Combine(_folder, "first"), null, " ", withAssembly, _folder }, probed);

        found.Should().Be(path);
        probed.Should().Equal(Path.Combine(_folder, "first", EngineTestAssemblyLoader.AssemblyName + ".dll"), path);
    }

    [Test]
    public void FindPath_returns_null_and_lists_every_path_when_no_directory_has_the_assembly()
    {
        var probed = new List<string>();

        EngineTestAssemblyLoader.FindPath(new[] { _folder, Path.Combine(_folder, "other") }, probed).Should().BeNull();

        probed.Should().HaveCount(2);
    }

    [Test]
    public void Load_puts_the_assembly_into_the_given_context_and_leaves_the_file_free()
    {
        var path = CopyEngineTestAssembly(_folder);
        var context = new CollectibleContext();
        try
        {
            var assembly = EngineTestAssemblyLoader.Load(context, path);

            AssemblyLoadContext.GetLoadContext(assembly).Should().BeSameAs(context);
            assembly.GetName().Name.Should().Be(EngineTestAssemblyLoader.AssemblyName);
            File.Delete(path);
            File.Exists(path).Should().BeFalse("the context holds the bytes, not the file");
        }
        finally
        {
            context.Unload();
        }
    }
}
