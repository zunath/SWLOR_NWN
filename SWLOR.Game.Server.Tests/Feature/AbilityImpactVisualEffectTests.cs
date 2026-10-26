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
    public void EveryActiveAbilityRank_BindsItsDeclaredImpactEffect()
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
        var nativeReference = File.ReadAllLines(Path.Combine(root, "SWLOR.Game.Server", "Readmes", "VisualEffectReference.csv"));

        foreach (var entry in manifest.RootElement.EnumerateArray())
        {
            var id = entry.GetProperty("Id").GetString()!;
            // Unarmed's authored motions use stock contact feedback only on positive damage.
            // Its stance transitions have an authored motion without a target-impact burst.
            VisualEffect? nativeContact = id switch
            {
                "Jab" or "PalmStrike" or "FinishingCombination" or "DirtyJab" or "LowKick" => VisualEffect.Vfx_Com_Blood_Spark_Small,
                "Uppercut" => VisualEffect.Vfx_Com_Blood_Spark_Medium,
                "PerfectForm" or "Haymaker" => VisualEffect.Vfx_Com_Blood_Spark_Large,
                "FlowStance" or "BruiserStance" => VisualEffect.None,
                _ => null
            };
            var declaresNativeEffect = entry.TryGetProperty("NativeImpactVisualEffect", out var nativeEffect);
            var enumName = declaresNativeEffect ? nativeEffect.GetString()! :
                id switch
                {
                    "ShieldBash" => "Vfx_Imp_Shield_Bash",
                    "MercilessAngleTechnique" => "Vfx_Imp_Merciless_Angle",
                    _ => "Vfx_Ability_" + id
                };
            var expected = nativeContact.GetValueOrDefault();
            if (!nativeContact.HasValue)
            {
                Enum.TryParse(enumName, out expected).Should().BeTrue($"{id} must have a stable effect reference");
                Enum.IsDefined(expected).Should().BeTrue($"{id} must reference a declared effect enum");
                expected.Should().NotBe(VisualEffect.None);
            }
            ((int)expected).Should().NotBe(999,
                $"{id} must avoid NWN's hardcoded Cows from Hell effect, which overrides visualeffects.2da");
            if (declaresNativeEffect)
            {
                enumName.Should().NotStartWith("Vfx_Ability_", "authored effects retain their unique resource contract");
                nativeReference.Should().Contain(line => Regex.IsMatch(line,
                    $@"^(COM|IMP|FNF),[^,]+,{Regex.Escape(enumName)},{(int)expected},"),
                    $"{id} must select its native impact enum and numeric row from the visual reference guide");
            }
            else if (!nativeContact.HasValue)
                usedEffects.Add(expected).Should().BeTrue($"{id} should have its own authored effect, shared only across ranks");
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
                // These capstones share Overclocked Analyzer's animation, not its impact VFX.
                var animationOnlyDefinition = feat switch
                {
                    FeatType.BloodFrenzyBurst => "BloodFrenzyAbilityDefinition",
                    FeatType.SoulAscensionBurst => "SoulAscensionAbilityDefinition",
                    _ => null
                };
                if (animationOnlyDefinition != null)
                {
                    id.Should().Be("OverclockedAnalyzer");
                    var type = types.Single(type => type.Name == animationOnlyDefinition);
                    matches = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities()
                        .Where(pair => pair.Key == feat).ToArray();
                }
                matches.Should().ContainSingle($"{id}/{feat} should resolve to one active definition");
                // These lines use native visuals in the impact action or positive-damage path.
                var usesNativeImpact = nativeContact.HasValue || animationOnlyDefinition != null ||
                    id is "WristRocket" or "ForceBurst" or "ForceJudgment" or "RadiantLance";
                var ability = matches.Single().Value;
                ability.SuccessfulImpactVisualEffect.Should().Be(usesNativeImpact ? VisualEffect.None : expected,
                    $"{id}/{feat} must use its intended impact path");
                ability.DamageImpactVisualEffect.Should().Be(
                    nativeContact ?? (id is "ForceJudgment" or "RadiantLance" ? VisualEffect.Vfx_Imp_Sunstrike : VisualEffect.None),
                    $"{id}/{feat} must bind the intended positive-damage visual");
                if (usesNativeImpact)
                    ability.ImpactAction.Should().NotBeNull($"{id}/{feat} still needs its native impact action");
            }

            if (nativeContact.HasValue)
                continue;
            rows.Should().ContainKey((int)expected, $"{id} needs an installed visualeffects.2da row");
            var row = rows[(int)expected];
            row[Array.IndexOf(header, "Type_FD")].Should().Be("F", $"{id} is a finite impact burst");
            var models = header.Select((name, index) => (name, index))
                .Where(column => column.name.StartsWith("Imp_") && column.name.EndsWith("_Node"))
                .Select(column => row[column.index]).Where(value => value != "****").ToArray();
            models.Should().NotBeEmpty($"{id} needs a model attachment");
            if (id == "MercilessAngleTechnique")
            {
                ((int)expected).Should().Be(1127);
                models.Should().Equal(new[] { "vx_merc_angl" },
                    "Merciless Angle must retain its authored crimson slash model");
            }
            foreach (var model in models)
            {
                model.Length.Should().BeLessThanOrEqualTo(16);
                // Native model resources ship in NWN's base archives; authored effects must
                // remain compiled and present in our HAK source.
                if (declaresNativeEffect) continue;
                var path = Path.Combine(root, "SWLOR_Haks", "sw_vfx", model + ".mdl");
                File.Exists(path).Should().BeTrue($"{id} needs compiled model {model}");
                var bytes = File.ReadAllBytes(path);
                bytes.Length.Should().BeGreaterThan(12);
                bytes.Take(4).Should().Equal(new byte[4], $"{model} must be binary MDL, not ASCII");
            }
        }
    }

    [Test]
    public void NativeReplacements_KeepTheirImpactActionsAndDamageGate()
    {
        var root = FindRepositoryRoot();
        var burst = File.ReadAllText(Path.Combine(root,
            "SWLOR.Game.Server/Feature/AbilityDefinition/Force/ForceBurstAbilityDefinition.cs"));
        burst.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Fireball)");
        burst.Should().Contain("targetVisualEffect: VisualEffect.VFX_IMP_KIN_L");
        burst.Should().Contain("afterSuccessfulHit: creature =>");
        burst.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Imp_Silence)");
        burst.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Wind)");
        burst.Should().Contain("onGeometryResolved: geometry => PlayCentralVisuals(activator, geometry)");
        burst.Should().Contain("Location(geometry.Area, geometry.Position, 0f)");
        burst.Should().Contain("CreateObject(ObjectType.Placeable, \"plc_invisobj\", center)");
        burst.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Fireball), visualAnchor)");
        burst.Should().Contain("ApplyWindPulse(visualAnchor);");
        burst.Should().Contain("DelayCommand(0.1f, () => ApplyWindPulse(visualAnchor))");
        burst.Should().Contain("DelayCommand(0.2f, () => ApplyWindPulse(visualAnchor))");
        burst.Should().Contain("DestroyObject(visualAnchor, 3f)");
        burst.Should().NotContain("ApplyWindPulse(target)");
        burst.Should().NotContain("EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Fireball), target)");

        var lance = File.ReadAllText(Path.Combine(root,
            "SWLOR.Game.Server/Feature/AbilityDefinition/Force/RadiantLanceAbilityDefinition.cs"));
        lance.Should().Contain("onGeometryResolved: geometry =>");
        Regex.IsMatch(lance, @"if \(level == 3\)\s+LaunchRadiantLance\(activator, geometry\)").Should().BeTrue();
        lance.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_BoltGlory)");
        lance.Should().NotContain("GetPosition(target)");

        var service = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server/Service/Ability.cs"));
        service.Should().Contain("onGeometryResolved?.Invoke(geometry)");
        service.Should().Contain("if (damage > 0 && trackedImpact?.Ability is { } impactAbility &&");
        service.Should().Contain("EffectVisualEffect(impactAbility.DamageImpactVisualEffect)");
    }

    [Test]
    public void ElectricalTechniqueAreaBursts_AreUniquePerAbilityAndRenderWithASound()
    {
        var root = FindRepositoryRoot();
        var abilityRoot = Path.Combine(root, "SWLOR.Game.Server", "Feature", "AbilityDefinition");

        // Row 151 (Vfx_Fnf_Storm) is a blank placeholder that renders nothing.
        foreach (var file in Directory.GetFiles(abilityRoot, "*.cs", SearchOption.AllDirectories))
        {
            File.ReadAllText(file).Should().NotContain("VisualEffect.Vfx_Fnf_Storm",
                $"{Path.GetFileName(file)} must not use the blank Vfx_Fnf_Storm row");
        }

        // Each ability has its own area burst; the player technique and its NPC twin share it.
        var expected = new Dictionary<string, VisualEffect>
        {
            ["StaticBurst"] = VisualEffect.Vfx_Fnf_Electric_Explosion,
            ["ArcPulse"] = VisualEffect.Fnf_Dispel,
            ["IonBurst"] = VisualEffect.Vfx_Fnf_Mystical_Explosion,
            ["FinalSuppression"] = VisualEffect.Vfx_Fnf_Pwstun,
            ["WardenMark"] = VisualEffect.Vfx_Fnf_Word,
        };
        expected.Values.Distinct().Should().HaveCount(expected.Count, "each ability needs its own area burst");
        foreach (var (name, effect) in expected)
        {
            foreach (var path in new[] { $"Mimicry/{name}TechniqueAbilityDefinition.cs", $"NPC/{name}AbilityDefinition.cs" })
            {
                File.ReadAllText(Path.Combine(abilityRoot, path)).Should().Contain($"VisualEffect.{effect}",
                    $"{path} must use its ability's own area burst");
            }
        }

        var table = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "visualeffects.2da"));
        var header = Regex.Split(table.First(line => line.TrimStart().StartsWith("Label ")).Trim(), @"\s+");
        var rows = table.Where(line => Regex.IsMatch(line, @"^\s*\d+\s"))
            .Select(line => Regex.Split(line.Trim(), @"\s+"))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts.Skip(1).ToArray());
        foreach (var effect in expected.Values)
        {
            var row = rows[(int)effect];
            header.Where(name => name.StartsWith("Imp_") && name.EndsWith("_Node"))
                .Select(name => row[Array.IndexOf(header, name)])
                .Should().Contain(value => value != "****", $"{effect} needs a model");
            row[Array.IndexOf(header, "SoundImpact")].Should().NotBe("****", $"{effect} needs a sound");
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
