using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Data
{
    [TestFixture]
    public class AbilityCatalogTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void Load_ReadsTheGameServersAbilityDefinitions()
        {
            _catalog.Abilities.Should().NotBeEmpty();
            _catalog.Abilities.Keys.Should().Contain(new[]
            {
                FeatType.Benevolence3, FeatType.MedKit1, FeatType.QuickDraw4, FeatType.Resuscitation2
            });
        }

        [Test]
        public void BenevolenceIII_HasItsCostTimingAndPerkData()
        {
            var ability = _catalog.Get(FeatType.Benevolence3);

            ability.Name.Should().Be("Benevolence III");
            ability.PerkName.Should().Be("Benevolence");
            ability.RecastGroup.Should().Be("Benevolence");
            ability.SkillRequirement.Should().Be(38);
            ability.FP.Should().Be(8);
            ability.STM.Should().Be(0);
            ability.CastSeconds.Should().Be(1f);
            ability.RecastSeconds.Should().Be(10f);
            ability.IsHealing.Should().BeTrue();
            ability.IsHostile.Should().BeFalse();
            ability.UnreadableTiming.Should().BeNull();
        }

        [Test]
        public void EveryAbilityTiming_IsReadOutsideTheEngine()
        {
            _catalog.Abilities.Values.Where(a => a.UnreadableTiming != null).Select(a => a.Feat).Should().BeEmpty();
            _catalog.Get(FeatType.Anger1).RecastSeconds.Should().Be(12f, "Anger computes its recast per beast, but the value is fixed");
        }

        [Test]
        public void ATimingThatNeedsTheEngine_IsFlaggedAndLogged_InsteadOfReadAsZero()
        {
            var catalog = AbilityCatalog.Load();
            var detail = new AbilityDetail
            {
                Name = "Engine-bound",
                RecastDelay = _ => throw new InvalidOperationException("needs the engine"),
                ActivationDelay = (_, _, _) => throw new InvalidOperationException("needs the engine")
            };

            var info = catalog.Describe(FeatType.Anger1, detail);

            info.UnreadableTiming.Should().Be("recast and cast time");
            info.RecastSeconds.Should().Be(0f);
            catalog.LoadErrors.Should().Contain("Anger1: recast and cast time needs the game engine");
        }

        [Test]
        public void MedKitI_CostsFourStaminaAndHasNoSkillRequirement()
        {
            var ability = _catalog.Get(FeatType.MedKit1);

            ability.RecastGroup.Should().Be("MedKit");
            ability.STM.Should().Be(4);
            ability.FP.Should().Be(0);
            ability.SkillRequirement.Should().Be(0);
        }

        [Test]
        public void QuickDrawIV_IsAHostileTwelveStaminaAbilityRequiringSkillForty()
        {
            var ability = _catalog.Get(FeatType.QuickDraw4);

            ability.IsHostile.Should().BeTrue();
            ability.STM.Should().Be(12);
            ability.SkillRequirement.Should().Be(40);
            ability.CastSeconds.Should().Be(0f);
        }

        [Test]
        public void ResuscitationII_TakesFourSecondsToCastAndSixtyToRecast()
        {
            var ability = _catalog.Get(FeatType.Resuscitation2);

            ability.CastSeconds.Should().Be(4f);
            ability.RecastSeconds.Should().Be(60f);
            ability.STM.Should().Be(10);
        }

        [Test]
        public void Get_ForAFeatThatIsNotAnAbility_ThrowsWithTheFeatName()
        {
            var act = () => _catalog.Get(FeatType.Invalid);

            act.Should().Throw<KeyNotFoundException>().WithMessage("*Invalid*");
        }

        [Test]
        public void EveryModeledEffect_HasAnAbilityInTheCatalog()
        {
            var modeled = Enum.GetValues<FeatType>().Where(feat => EffectCatalog.TryGet(feat, out _)).ToList();

            modeled.Should().NotBeEmpty();
            modeled.Where(feat => !_catalog.Abilities.ContainsKey(feat)).Should().BeEmpty();
        }
    }
}
