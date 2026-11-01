using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class CapstoneProofDefinitionTests
{
    [Test]
    public void ViralCascade_RejectsItemVenomAndForeignAbilitiesButCountsVibroknifeVenomAndInfection()
    {
        var proof = Proof("viral_cascade");
        var virulentBlade = new VirulentBladeAbilityDefinition().BuildAbilities();
        var quickDraw = new QuickDrawAbilityDefinition().BuildAbilities()[FeatType.QuickDraw1];

        proof.CountsEnemyStatusEffect(typeof(VenomStatusEffect), null).Should().BeFalse();
        proof.CountsEnemyStatusEffect(typeof(VenomStatusEffect), quickDraw).Should().BeFalse();
        foreach (var ability in virulentBlade.Values)
            proof.CountsEnemyStatusEffect(typeof(VenomStatusEffect), ability).Should().BeTrue();
        proof.CountsEnemyStatusEffect(typeof(InfectionStatusEffect), null).Should().BeTrue();
        proof.CountsEnemyStatusEffect(typeof(BleedStatusEffect), null).Should().BeFalse();
    }

    [Test]
    public void CripplingDefense_CountsTheFirstRankOfEveryQualifyingTechnique()
    {
        Proof("crippling_defense").AbilityHits.Should().Contain(new[]
        {
            FeatType.DisablingStrike1, FeatType.DisruptionField1, FeatType.HamperingBarrage1,
            FeatType.InterruptionStrike1, FeatType.SweepingFlank1, FeatType.VigorThrust1,
        });
    }

    [Test]
    public void HoldTheLine_CountsRousingShoutShieldingWithoutItsLowHealthStatus()
    {
        var proof = Proof("hold_the_line");
        proof.SupportAbilityHits.Should().BeEquivalentTo(new[]
        {
            FeatType.RousingShout1, FeatType.RousingShout2, FeatType.RousingShout3,
        });
        proof.SupportStatusEffects.Should().NotContain(typeof(RousingShout1StatusEffect));
    }

    [Test]
    public void BeastLines_CountOwnedBeastDamageWithoutRoleOrAbilityRequirements()
    {
        var proofs = CapstoneProofDefinitions.All.Where(proof => proof.CountsOwnedBeastDamage).ToArray();
        proofs.Select(proof => proof.Key).Should().BeEquivalentTo(new[]
        {
            "primal_overrun", "untouchable_instinct", "force_bonded_beast",
            "apex_bite", "unbreakable_beast", "alpha_rhythm",
        });
        foreach (var proof in proofs)
        {
            proof.AbilityHits.Should().BeEmpty();
            proof.EnemyStatusEffects.Should().BeEmpty();
            proof.SupportStatusEffects.Should().BeEmpty();
        }
    }

    private static CapstoneProofDefinition Proof(string key) =>
        CapstoneProofDefinitions.All.Single(proof => proof.Key == key);
}
