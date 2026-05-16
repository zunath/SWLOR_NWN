using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class NPCAbilityDefinitionTests
{
    [Test]
    public void NPCAbilities_MatchBibleRecastAndResourceCosts()
    {
        var abilities = BuildNPCAbilities();
        var expected = ExpectedAbilities();

        abilities.Keys.Should().BeEquivalentTo(expected.Keys);

        foreach (var (feat, expectedAbility) in expected)
        {
            abilities.Should().ContainKey(feat);

            var ability = abilities[feat];
            ability.Name.Should().Be(expectedAbility.Name);
            ability.RecastGroup.Should().Be(expectedAbility.RecastGroup);
            ability.RecastDelay.Should().NotBeNull();
            ability.RecastDelay(0).Should().Be(expectedAbility.RecastSeconds);

            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(expectedAbility.StaminaCost);

            if (expectedAbility.FPCost.HasValue)
            {
                ability.Requirements
                    .OfType<AbilityRequirementFP>()
                    .Should()
                    .ContainSingle()
                    .Which
                    .RequiredFP
                    .Should()
                    .Be(expectedAbility.FPCost.Value);
            }
            else
            {
                ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();
            }
        }
    }

    private static Dictionary<FeatType, AbilityDetail> BuildNPCAbilities()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var npcAbilityNamespace = typeof(ArcPulseAbilityDefinition).Namespace;
        var abilities = new Dictionary<FeatType, AbilityDetail>();

        var definitions = typeof(ArcPulseAbilityDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Namespace == npcAbilityNamespace &&
                definitionType.IsAssignableFrom(type))
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!)
            .ToList();

        foreach (var definition in definitions)
        {
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                abilities.Add(feat, ability);
            }
        }

        return abilities;
    }

    private static Dictionary<FeatType, ExpectedNPCAbility> ExpectedAbilities()
    {
        return new Dictionary<FeatType, ExpectedNPCAbility>
        {
            [FeatType.RendingBite] = new("Rending Bite", RecastGroup.RendingBite, 14f, 3),
            [FeatType.CripplingTalons] = new("Crippling Talons", RecastGroup.CripplingTalons, 10f, 3),
            [FeatType.PiercingQuills] = new("Piercing Quills", RecastGroup.PiercingQuills, 18f, 5),
            [FeatType.ToxicSpit] = new("Toxic Spit", RecastGroup.ToxicSpit, 18f, 3),
            [FeatType.ScorchingBreath] = new("Scorching Breath", RecastGroup.ScorchingBreath, 22f, 5),
            [FeatType.InfernoBlast] = new("Inferno Blast", RecastGroup.InfernoBlast, 34f, 8),
            [FeatType.SeismicSlam] = new("Seismic Slam", RecastGroup.SeismicSlam, 28f, 6),
            [FeatType.RupturingQuake] = new("Rupturing Quake", RecastGroup.RupturingQuake, 48f, 10),
            [FeatType.TerrifyingBellow] = new("Terrifying Bellow", RecastGroup.TerrifyingBellow, 20f, 4),
            [FeatType.DisorientingScreech] = new("Disorienting Screech", RecastGroup.DisorientingScreech, 36f, 7),
            [FeatType.IronCarapace] = new("Iron Carapace", RecastGroup.IronCarapace, 32f, 4),
            [FeatType.MaulingBite] = new("Mauling Bite", RecastGroup.MaulingBite, 13f, 4),
            [FeatType.BonecrusherBite] = new("Bonecrusher Bite", RecastGroup.BonecrusherBite, 16f, 5),
            [FeatType.RakingClaws] = new("Raking Claws", RecastGroup.RakingClaws, 11f, 3),
            [FeatType.PouncingStrike] = new("Pouncing Strike", RecastGroup.PouncingStrike, 18f, 5),
            [FeatType.TailSweep] = new("Tail Sweep", RecastGroup.TailSweep, 18f, 5),
            [FeatType.GoringCharge] = new("Goring Charge", RecastGroup.GoringCharge, 22f, 6),
            [FeatType.BarbedVolley] = new("Barbed Volley", RecastGroup.BarbedVolley, 17f, 5),
            [FeatType.VenomSpray] = new("Venom Spray", RecastGroup.VenomSpray, 18f, 5),
            [FeatType.ToxicCloud] = new("Toxic Cloud", RecastGroup.ToxicCloud, 24f, 7),
            [FeatType.FrostSpit] = new("Frost Spit", RecastGroup.FrostSpit, 16f, 4),
            [FeatType.StaticBurst] = new("Static Burst", RecastGroup.StaticBurst, 20f, 6),
            [FeatType.SavageRoar] = new("Savage Roar", RecastGroup.SavageRoar, 21f, 4),
            [FeatType.SonicShriek] = new("Sonic Shriek", RecastGroup.SonicShriek, 19f, 5),
            [FeatType.ChitinGuard] = new("Chitin Guard", RecastGroup.ChitinGuard, 32f, 5),
            [FeatType.PrecisionShot] = new("Precision Shot", RecastGroup.PrecisionShot, 15f, 4),
            [FeatType.SuppressingShot] = new("Suppressing Shot", RecastGroup.SuppressingShot, 17f, 4),
            [FeatType.GrenadeBurst] = new("Grenade Burst", RecastGroup.GrenadeBurst, 22f, 6),
            [FeatType.SerratedSlash] = new("Serrated Slash", RecastGroup.SerratedSlash, 15f, 4),
            [FeatType.BrutalBash] = new("Brutal Bash", RecastGroup.BrutalBash, 16f, 4),
            [FeatType.TacticalMark] = new("Tactical Mark", RecastGroup.TacticalMark, 20f, 4),
            [FeatType.OverloadShot] = new("Overload Shot", RecastGroup.OverloadShot, 17f, 5),
            [FeatType.ArcPulse] = new("Arc Pulse", RecastGroup.ArcPulse, 20f, 6),
            [FeatType.IonBurst] = new("Ion Burst", RecastGroup.IonBurst, 18f, 5),
            [FeatType.TargetLock] = new("Target Lock", RecastGroup.TargetLock, 20f, 3),
            [FeatType.ShrapnelBurst] = new("Shrapnel Burst", RecastGroup.ShrapnelBurst, 20f, 6),
            [FeatType.ForceRend] = new("Force Rend", RecastGroup.ForceRend, 16f, 5),
            [FeatType.MindSpike] = new("Mind Spike", RecastGroup.MindSpike, 18f, 4),
            [FeatType.DarkShock] = new("Dark Shock", RecastGroup.DarkShock, 24f, 7),
            [FeatType.DreadWave] = new("Dread Wave", RecastGroup.DreadWave, 24f, 6),
        };
    }

    private sealed record ExpectedNPCAbility(
        string Name,
        RecastGroup RecastGroup,
        float RecastSeconds,
        int StaminaCost,
        int? FPCost = null);
}
