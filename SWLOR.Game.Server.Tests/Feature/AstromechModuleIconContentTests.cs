using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The inventory icons of astromech modules. A module keeps the base item of the humanoid piece it copies, and those
/// base items pick their icon by model number, so the number is all that makes a module show a droid part. The numbers
/// are MODULE_ICONS in tools/GenerateAstromechModules.py. They belong to the modules alone: no humanoid piece may carry
/// one, and no appearance catalog may offer one, or a humanoid piece would show a droid part.
/// </summary>
public class AstromechModuleIconContentTests
{
    private const int Helmet = 17;
    private const int Cloak = 80;

    private static readonly string Root = FindRepositoryRoot();
    private static readonly IReadOnlyDictionary<int, HashSet<int>> ModuleIcons = AstromechModuleIconNumbers.ByBaseItem;

    [Test]
    public void EveryModuleKindHasItsOwnIconNumbers()
    {
        ModuleIcons.Keys.Should().BeEquivalentTo(new[] { Helmet, 19, 21, 26, 36, 52, 78, Cloak });
        ModuleIcons.Values.Should().OnlyContain(numbers => numbers.Count >= 4);
    }

    [Test]
    public void EveryModuleShowsADroidPartAndNoHumanoidPieceDoes()
    {
        var problems = new List<string>();
        var modules = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file, Encoding.Latin1));
            var item = document.RootElement;
            if (!ModuleIcons.TryGetValue(item.GetProperty("BaseItem").GetProperty("value").GetInt32(), out var icons))
                continue;

            var name = Path.GetFileName(file).Split('.')[0];
            var models = item.EnumerateObject()
                .Where(field => field.Name.StartsWith("ModelPart") || field.Name.StartsWith("xModelPart"))
                .Select(field => field.Value.GetProperty("value").GetInt32())
                .Distinct()
                .ToList();
            if (name.StartsWith("am_"))
            {
                modules++;
                if (models.Count != 1 || !AstromechModuleIconNumbers.IsModuleIcon(name, item.GetProperty("BaseItem").GetProperty("value").GetInt32(), models[0]))
                    problems.Add($"{name} is a module with model {string.Join("/", models)}: it shows a humanoid icon");
            }
            else if (models.Any(icons.Contains))
            {
                problems.Add($"{name} is a humanoid piece with model {string.Join("/", models)}: it shows a module icon");
            }
        }

        modules.Should().BeGreaterThan(900);
        problems.Should().BeEmpty("rerun the astromech generators, or give the humanoid piece another model" +
                                  Environment.NewLine + string.Join(Environment.NewLine, problems.Take(40)));
    }

    /// <summary>
    /// A helmet and a cloak draw their icon through the dye palette, so those two kinds have palette icons and each
    /// module wears the dyes its icon was drawn in. Other dyes would tint the picture.
    /// </summary>
    [Test]
    public void SensorPackagesAndDeflectorEmittersWearTheDyesOfTheirIcon()
    {
        var generator = AstromechModuleIconNumbers.Generator;
        var metal = int.Parse(Regex.Match(generator, @"^ICON_METAL_DYE = (\d+)", RegexOptions.Multiline).Groups[1].Value);
        var glows = Regex.Match(generator, @"^ICON_GLOW_DYES = \(([\d, ]+)\)", RegexOptions.Multiline).Groups[1].Value
            .Split(',').Select(int.Parse).ToList();
        glows.Should().HaveCount(ModuleIcons[Helmet].Count).And.OnlyHaveUniqueItems();

        var problems = new List<string>();
        var modules = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "uti"), "am_*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file, Encoding.Latin1));
            var item = document.RootElement;
            var baseItem = item.GetProperty("BaseItem").GetProperty("value").GetInt32();
            if (baseItem is not (Helmet or Cloak))
                continue;

            modules++;
            var variant = item.GetProperty("ModelPart1").GetProperty("value").GetInt32() - ModuleIcons[baseItem].Min();
            int Dye(string field) => item.TryGetProperty(field, out var dye) ? dye.GetProperty("value").GetInt32() : -1;
            if (Dye("Metal1Color") != metal || variant < 0 || variant >= glows.Count || Dye("Cloth1Color") != glows[variant])
                problems.Add($"{Path.GetFileName(file).Split('.')[0]}: metal {Dye("Metal1Color")}, glow {Dye("Cloth1Color")}");
        }

        modules.Should().BeGreaterThan(300);
        problems.Should().BeEmpty("rerun the astromech generators" + Environment.NewLine +
                                  string.Join(Environment.NewLine, problems.Take(40)));
    }

    [Test]
    public void NoAppearanceCatalogOffersAModuleIcon()
    {
        var catalogs = typeof(BaseArmorAppearanceDefinition).Assembly.GetTypes()
            .Where(type => typeof(BaseArmorAppearanceDefinition).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => (BaseArmorAppearanceDefinition)Activator.CreateInstance(type)!)
            .ToList();

        catalogs.Count.Should().BeGreaterThan(2);
        foreach (var catalog in catalogs)
        {
            catalog.Helmet.Should().NotContain(ModuleIcons[Helmet], catalog.GetType().Name);
            catalog.Cloak.Should().NotContain(ModuleIcons[Cloak], catalog.GetType().Name);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
