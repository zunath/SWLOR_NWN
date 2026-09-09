using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Tests;

public class ChoreographyProjectParityTests
{
    [Test]
    public void EveryRecipeBackedProjectMatchesCurrentAuthoring()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        var animations = Path.Combine(root, "design/animations");
        var models = Path.Combine(root, "SWLOR_Haks/sw_cr_creature");
        var reader = new MdlReader();
        var model = reader.Parse(File.ReadAllBytes(Path.Combine(models, "a_ba.mdl")));
        var sources = new Dictionary<string, MdlModel>(StringComparer.OrdinalIgnoreCase);
        var recipeFiles = Directory.EnumerateFiles(animations, "choreographies.json", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(animations, path).Replace('\\', '/'),
                path => ChoreographyAuthor.Read(File.ReadAllText(path)), StringComparer.OrdinalIgnoreCase);
        var recipeIds = recipeFiles.Values.SelectMany(recipes => recipes).Select(recipe => recipe.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mismatches = new List<string>();
        var checkedProjects = 0;
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(animations, "active-manifest.json")));
        foreach (var entry in manifest.RootElement.GetProperty("Animations").EnumerateArray())
        {
            var id = entry.GetProperty("Id").GetString()!;
            // Projects authored manually or imported without a recipe have no bake
            // to reproduce. Every entry claiming recipe provenance is checked,
            // including preserved native clips and recipes without RootOffset.
            if (!entry.TryGetProperty("ChoreographyPath", out var path) || path.ValueKind == JsonValueKind.Null)
            {
                recipeIds.Should().NotContain(id, "an existing recipe must not bypass parity by dropping its manifest provenance");
                continue;
            }
            var recipePath = path.GetString();
            recipePath.Should().NotBeNullOrWhiteSpace("recipe provenance must identify an actual choreography file");
            recipePath = recipePath!.Replace('\\', '/');
            recipeFiles.Should().ContainKey(recipePath, "manifest recipe paths must identify a checked-in choreography file");
            var recipes = recipeFiles[recipePath];
            var recipe = recipes.Single(value => value.Id == id);
            foreach (var name in recipe.Beats.Where(beat => beat.SourceModel != null).Select(beat => beat.SourceModel!).Distinct())
                if (!sources.ContainsKey(name)) sources.Add(name, reader.Parse(File.ReadAllBytes(Path.Combine(models, name + ".mdl"))));
            var baked = ChoreographyAuthor.Bake(model, recipe, sources);
            var projectPath = entry.GetProperty("Project").GetString()!;
            var saved = File.ReadAllText(Path.Combine(animations, projectPath)).TrimStart('\uFEFF').Replace("\r\n", "\n");
            if ((baked.Serialize() + "\n").Replace("\r\n", "\n") != saved) mismatches.Add(id + " (" + projectPath + ")");
            checkedProjects++;
        }
        checkedProjects.Should().BeGreaterThan(0, "the manifest must include the active authored animation corpus");
        mismatches.Should().BeEmpty("recipe-backed projects must be regenerated after authoring behavior changes; stale projects: " + string.Join(", ", mismatches));
        TestContext.Progress.WriteLine($"Verified all {checkedProjects} recipe-backed projects against a fresh bake.");
    }
}
