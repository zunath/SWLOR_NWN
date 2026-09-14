using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactVisualEffectTests
{
    [Test]
    public void EveryActiveAbilityRank_BindsItsOwnCompiledFiniteEffect()
    {
        var root = FindRepositoryRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "design", "animations", "active-abilities.json")));
        var table = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "visualeffects.2da"));
        var header = Regex.Split(table.First(line => line.TrimStart().StartsWith("Label ")).Trim(), @"\s+");
        var rows = table.Where(line => Regex.IsMatch(line, @"^\s*\d+\s"))
            .Select(line => Regex.Split(line.Trim(), @"\s+"))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts.Skip(1).ToArray());
        var types = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IAbilityListDefinition).IsAssignableFrom(type)).ToArray();
        var built = new Dictionary<string, Dictionary<FeatType, AbilityDetail>>();
        var usedEffects = new HashSet<VisualEffect>();

        foreach (var entry in manifest.RootElement.EnumerateArray())
        {
            var id = entry.GetProperty("Id").GetString()!;
            var enumName = id == "ShieldBash" ? "Vfx_Imp_Shield_Bash" : "Vfx_Ability_" + id;
            Enum.TryParse<VisualEffect>(enumName, out var expected).Should().BeTrue($"{id} must have a stable effect reference");
            usedEffects.Add(expected).Should().BeTrue($"{id} should have its own effect, shared only across ranks");
            var definitions = entry.GetProperty("DefinitionFiles").EnumerateArray()
                .Select(file => Path.ChangeExtension(file.GetString()!, null).Replace('/', '.').Replace('\\', '.')).ToArray();
            foreach (var definition in definitions)
            {
                if (!built.ContainsKey(definition))
                {
                    var type = types.Single(type => type.FullName == definition);
                    built[definition] = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
                }
            }

            foreach (var featName in entry.GetProperty("Feats").EnumerateArray())
            {
                var feat = Enum.Parse<FeatType>(featName.GetString()!);
                var matches = definitions.SelectMany(definition => built[definition]).Where(pair => pair.Key == feat).ToArray();
                matches.Should().ContainSingle($"{id}/{feat} should resolve to one active definition");
                matches.Single().Value.SuccessfulImpactVisualEffect.Should().Be(expected, $"{id}/{feat} must share its line's effect");
            }

            rows.Should().ContainKey((int)expected, $"{id} needs an installed visualeffects.2da row");
            var row = rows[(int)expected];
            row[Array.IndexOf(header, "Type_FD")].Should().Be("F", $"{id} is a finite impact burst");
            var models = header.Select((name, index) => (name, index))
                .Where(column => column.name.StartsWith("Imp_") && column.name.EndsWith("_Node"))
                .Select(column => row[column.index]).Where(value => value != "****").ToArray();
            models.Should().NotBeEmpty($"{id} needs a model attachment");
            foreach (var model in models)
            {
                model.Length.Should().BeLessThanOrEqualTo(16);
                var path = Path.Combine(root, "SWLOR_Haks", "sw_vfx", model + ".mdl");
                File.Exists(path).Should().BeTrue($"{id} needs compiled model {model}");
                var bytes = File.ReadAllBytes(path);
                bytes.Length.Should().BeGreaterThan(12);
                bytes.Take(4).Should().Equal(new byte[4], $"{model} must be binary MDL, not ASCII");
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_NWN repository root.");
    }

    [Test]
    public void SeveralSuccessfulRiders_EmitOnlyOncePerRecipient()
    {
        var impact = new AbilityImpactVisualEffects(VisualEffect.Vfx_Imp_Shield_Bash);
        impact.TryRecordRecipient(10).Should().BeTrue();
        impact.TryRecordRecipient(10).Should().BeFalse();
        impact.TryRecordRecipient(11).Should().BeTrue();
    }

    [Test]
    public void SubsequentPulse_HasItsOwnReceiptState()
    {
        var first = new AbilityImpactVisualEffects(VisualEffect.Vfx_Imp_Shield_Bash);
        var next = new AbilityImpactVisualEffects(VisualEffect.Vfx_Imp_Shield_Bash);
        first.TryRecordRecipient(10).Should().BeTrue();
        next.TryRecordRecipient(10).Should().BeTrue();
    }

    [Test]
    public void UnconfiguredAbility_DoesNotEmitAReceipt()
    {
        var impact = new AbilityImpactVisualEffects(new AbilityDetail().SuccessfulImpactVisualEffect);
        impact.TryRecordRecipient(10).Should().BeFalse();
    }

    [Test]
    public void VisualBinding_IsScopedToItsRankAndDoesNotReplaceImpactAction()
    {
        var builder = new AbilityBuilder();
        AbilityImpactAction action = (_, _, _, _) => { };
        var abilities = builder
            .Create(FeatType.ShieldBash1, PerkType.ShieldBash)
            .HasImpactAction(action)
            .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Imp_Shield_Bash)
            .Create(FeatType.ShieldBash2, PerkType.ShieldBash)
            .Build();

        abilities[FeatType.ShieldBash1].ImpactAction.Should().BeSameAs(action);
        abilities[FeatType.ShieldBash1].SuccessfulImpactVisualEffect.Should().Be(VisualEffect.Vfx_Imp_Shield_Bash);
        abilities[FeatType.ShieldBash2].SuccessfulImpactVisualEffect.Should().Be(VisualEffect.None);
    }
}
