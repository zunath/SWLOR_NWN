using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SWLOR.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>
/// Small recipe definition files in the shapes the writer must handle, plus the stub types they compile against.
/// </summary>
public static class RecipeSourceFixtures
{
    public const string Arguments = "center: 50, width: 20";

    public const string Stubs = @"
using System;
using System.Collections.Generic;
public enum RecipeType { Invalid, AdventTunic, AmateurTunic, CombatTunic, InstructionA, InstructionB, TraceOne, TraceTwo }
public enum SkillType { Smithery, Engineering }
public enum RecipeCategoryType { Tunic, Tool }
public interface IRecipeListDefinition { Dictionary<RecipeType, RecipeDetail> BuildRecipes(); }
public class RecipeDetail { }
public abstract class RecipeMinigameParameters { }
public sealed class TestForgeParameters : RecipeMinigameParameters
{
    public TestForgeParameters(int center, int width) { }
}
public class RecipeBuilder
{
    public RecipeBuilder Create(RecipeType type, SkillType skill) => this;
    public RecipeBuilder Category(RecipeCategoryType category) => this;
    public RecipeBuilder Level(int level) => this;
    public RecipeBuilder Component(string resref, int quantity) => this;
    public RecipeBuilder EffectDescription(IEnumerable<string> lines) => this;
    public RecipeBuilder TestForgeParameters(int center, int width) => this;
    public RecipeBuilder MinigameParameters(RecipeMinigameParameters parameters) => this;
    public Dictionary<RecipeType, RecipeDetail> Build() => null;
}
";

    public const string DirectChains = @"using System.Collections.Generic;

namespace Fixture
{
    public class Direct : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // First recipe
            _builder.Create(RecipeType.AdventTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Level(10)
                .Component(""ref_veldite"", 5); // trailing note

            _builder.Create(RecipeType.AmateurTunic, SkillType.Smithery).Level(1);

            _builder.Create(RecipeType.CombatTunic, SkillType.Smithery);
            return _builder.Build();
        }
    }
}
";

    public const string VoidHelper = @"using System.Collections.Generic;

namespace Fixture
{
    public class VoidHelper : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Add(RecipeType.AdventTunic, 5, ""ref_veldite"");
            Add(RecipeType.AmateurTunic, 15, ""ref_flawed"");
            return _builder.Build();
        }

        private void Add(RecipeType type, int level, string metal)
        {
            _builder.Create(type, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Level(level)
                .Component(metal, 3);
        }
    }
}
";

    public const string ReturningHelperWithChainedRow = @"using System.Collections.Generic;

namespace Fixture
{
    public class ReturningHelper : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Add(RecipeType.TraceOne, 5, ""elec_ruined"");
            Add(RecipeType.TraceTwo, 15, ""elec_flawed"")
                .EffectDescription(new[]
                {
                    ""Second line.""
                });
            return _builder.Build();
        }

        private RecipeBuilder Add(
            RecipeType type,
            int level,
            string metal)
        {
            return _builder.Create(type, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Level(level)
                .Component(metal, 3);
        }
    }
}
";

    public const string RecordLoop = @"using System.Collections.Generic;

namespace Fixture
{
    public class RecordLoop : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            CreateTier(
                10,
                new InstructionRecipe(RecipeType.InstructionA, ""id_a""),
                new InstructionRecipe(RecipeType.InstructionB, ""id_b""));
            return _builder.Build();
        }

        private void CreateTier(int level, params InstructionRecipe[] recipes)
        {
            foreach (var recipe in recipes)
            {
                _builder.Create(recipe.Type, SkillType.Engineering)
                    .Category(RecipeCategoryType.Tool)
                    .Level(level)
                    .Component(recipe.Resref, 1);
            }
        }

        private readonly record struct InstructionRecipe(RecipeType Type, string Resref);
    }
}
";

    public static string Lf(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    public static string Crlf(string text)
    {
        return Lf(text).Replace("\n", "\r\n");
    }

    public static Func<string, RecipeParameterPlan> Writing(params string[] recipes)
    {
        return name => recipes.Contains(name)
            ? RecipeParameterPlan.Write("TestForgeParameters", Arguments)
            : RecipeParameterPlan.Leave;
    }

    public static Func<string, RecipeParameterPlan> Removing(params string[] recipes)
    {
        return name => recipes.Contains(name)
            ? RecipeParameterPlan.Remove("TestForgeParameters")
            : RecipeParameterPlan.Leave;
    }

    /// <summary>Compiles the stubs and a rewritten fixture together and returns the compiler errors.</summary>
    public static IReadOnlyList<string> CompileErrors(params string[] sources)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        var trees = sources.Append(Stubs).Select(source => CSharpSyntaxTree.ParseText(source)).ToList();
        var compilation = CSharpCompilation.Create(
            "RewrittenFixture" + Guid.NewGuid().ToString("N"),
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToList();
    }
}
