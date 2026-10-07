using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityDescriptionTests
{
    [TestCase(FeatType.DreadWaveTechnique, "Dread Wave", 8, 24)]
    [TestCase(FeatType.InnerVoidTechnique, "Inner Void", 9, 24)]
    public void ReportedTechniquesUseTheSameHotbarDescriptionFormatAsPerkAbilities(
        FeatType feat, string name, int stamina, int recast)
    {
        IAbilityListDefinition definition = feat == FeatType.DreadWaveTechnique
            ? new DreadWaveTechniqueAbilityDefinition()
            : new InnerVoidTechniqueAbilityDefinition();
        Format(definition.BuildAbilities()[feat], "Authored effects.").Should().Be(
            $"Name: {name}\nFP: 0\nSTM: {stamina}\nRecast: {recast}s\nDescription: Authored effects.\n");
    }

    [Test]
    public void EveryMimicryTechniqueUsesItsOwnRequirementsAndRecastWhileTraitsStayPassive()
    {
        var definitions = typeof(DreadWaveTechniqueAbilityDefinition).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(DreadWaveTechniqueAbilityDefinition).Namespace &&
                           typeof(IAbilityListDefinition).IsAssignableFrom(type) && !type.IsAbstract);
        var count = 0;
        foreach (var type in definitions)
        foreach (var (_, detail) in ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
        {
            var formatted = Format(detail, "Authored effects.");
            formatted.Should().Contain($"Name: {detail.Name}\n");
            formatted.Should().EndWith("Description: Authored effects.\n");
            if (detail.IsMimicryTrait)
            {
                formatted.Should().Contain("Type: Passive Trait\n");
                formatted.Should().NotContain("STM:");
                formatted.Should().NotContain("Recast:");
            }
            else
            {
                var stamina = detail.Requirements.OfType<AbilityRequirementStamina>().LastOrDefault()?.RequiredSTM ?? 0;
                formatted.Should().Contain($"STM: {stamina}\n");
                formatted.Should().Contain(FormattableString.Invariant($"Recast: {detail.RecastDelay?.Invoke(0x7f000000) ?? 0f}s\n"));
            }
            count++;
        }
        count.Should().BeGreaterThan(2, "the complete technique pool must be covered");
    }

    [Test]
    public void OtherAbilitiesRetainBothResourceCostsAndFractionalRecasts()
    {
        var detail = new AbilityDetail
        {
            Name = "Example",
            Requirements = new List<IAbilityActivationRequirement>
            {
                new AbilityRequirementFP(4), new AbilityRequirementStamina(6)
            },
            RecastDelay = _ => 2.5f
        };
        using var culture = new CultureScope("fr-FR");
        Format(detail, "Effects.").Should().Be("Name: Example\nFP: 4\nSTM: 6\nRecast: 2.5s\nDescription: Effects.\n");
    }

    private static string Format(AbilityDetail detail, string description) =>
        (string)typeof(TlkOverrides).GetMethod("BuildAbilityDescription", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { detail, description })!;

    private sealed class CultureScope : IDisposable
    {
        private readonly System.Globalization.CultureInfo _previous = System.Globalization.CultureInfo.CurrentCulture;
        public CultureScope(string name) => System.Globalization.CultureInfo.CurrentCulture = new(name);
        public void Dispose() => System.Globalization.CultureInfo.CurrentCulture = _previous;
    }
}
