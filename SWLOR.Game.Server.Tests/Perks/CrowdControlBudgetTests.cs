using System.Globalization;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

public class CrowdControlBudgetTests
{
    [Test]
    public void PlayerAreaControl_AlwaysDeclaresATargetBudget()
    {
        var controlEffects = typeof(StatusEffectBase).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(StatusEffectBase).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (StatusEffectBase)Activator.CreateInstance(t)!)
            .Where(e => (e.Categories & StatusEffectCategory.HardCrowdControl) != 0).ToArray();
        foreach (var effect in controlEffects)
            effect.Categories.HasFlag(StatusEffectCategory.Debuff).Should().BeTrue(effect.Name);
        var controlTypes = controlEffects.Select(e => e.GetType().Name).ToHashSet();
        var offenders = new List<string>();
        var examined = 0;
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Root(), "SWLOR.Game.Server", "Feature", "AbilityDefinition"), "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(Path.GetDirectoryName(file)) == "NPC") continue;
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            foreach (var call in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var method = call.Expression.ToString();
                var args = call.ArgumentList.Arguments;
                IEnumerable<ArgumentSyntax> payload;
                string budget;
                if (method == "ConfigureWeaponAbility" && args.Count >= 22 && args[9].Expression.ToString() == "true")
                {
                    payload = new[] { args[4], args[5], args[21] };
                    budget = args[21].DescendantNodes().OfType<AssignmentExpressionSyntax>()
                        .FirstOrDefault(a => a.Left.ToString() == "MaximumAreaTargets")?.Right.ToString();
                }
                else if (method == "InnateAbility.BuildArea")
                {
                    payload = new[] { args[11] }.Concat(args.Where(a => a.NameColon?.Name.Identifier.Text == "additionalStatusEffects"));
                    budget = args.FirstOrDefault(a => a.NameColon?.Name.Identifier.Text == "maxTargets")?.Expression.ToString();
                }
                else if (method == "Ability.ApplyTelegraphedCombatImpact" && args.Count >= 7)
                {
                    payload = new[] { args[6] }.Concat(args.Where(a => a.NameColon?.Name.Identifier.Text is "additionalStatusEffects" or "statusEffectFactory"));
                    budget = args.FirstOrDefault(a => a.NameColon?.Name.Identifier.Text == "maxTargets")?.Expression.ToString();
                }
                else continue;

                if (!payload.SelectMany(a => a.DescendantNodes()).OfType<IdentifierNameSyntax>()
                        .Any(n => controlTypes.Contains(n.Identifier.Text))) continue;
                examined++;
                if (budget == null || budget == "0")
                    offenders.Add($"{Path.GetFileName(file)}:{call.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            }
        }
        examined.Should().BeGreaterThanOrEqualTo(18, "the audit must cover both weapon and Mimicry control areas");
        string.Join(", ", offenders).Should().BeEmpty("hard-control areas require an explicit finite target budget");
    }

    [Test]
    public void MimicryControl_CooldownIsAtLeastOneAndAHalfTimesItsAuthoredDuration()
    {
        var directory = Path.Combine(Root(), "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Mimicry");
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs"))
        foreach (var call in CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression.ToString() is not ("InnateAbility.BuildArea" or "InnateAbility.BuildSingleTarget")) continue;
            var args = call.ArgumentList.Arguments;
            var typeName = args[11].Expression is TypeOfExpressionSyntax typeOf ? typeOf.Type.ToString() : null;
            var type = typeof(StatusEffectBase).Assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            if (type == null || (((StatusEffectBase)Activator.CreateInstance(type)!).Categories & StatusEffectCategory.HardCrowdControl) == 0) continue;
            var cooldown = float.Parse(args[7].Expression.ToString().TrimEnd('f'), CultureInfo.InvariantCulture);
            var duration = int.Parse(args[10].Expression.ToString(), CultureInfo.InvariantCulture);
            cooldown.Should().BeGreaterThanOrEqualTo(duration * 1.5f, Path.GetFileName(file));
        }
    }

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
