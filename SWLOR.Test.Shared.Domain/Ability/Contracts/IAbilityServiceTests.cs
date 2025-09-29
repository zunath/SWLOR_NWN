using NSubstitute;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Test.Shared.Domain.Ability.Contracts
{
    [TestFixture]
    public class IAbilityServiceTests
    {
        [Test]
        public void IAbilityService_ShouldBeInterface()
        {
            // Act
            var type = typeof(IAbilityService);

            // Assert
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IAbilityService_ShouldHaveCacheDataMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.CacheData();

            // Assert
            abilityService.Received(1).CacheData();
        }

        [Test]
        public void IAbilityService_ShouldHaveCacheAbilitiesMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.CacheAbilities();

            // Assert
            abilityService.Received(1).CacheAbilities();
        }

        [Test]
        public void IAbilityService_ShouldHaveCacheToggleActionsMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.CacheToggleActions();

            // Assert
            abilityService.Received(1).CacheToggleActions();
        }

        [Test]
        public void IAbilityService_ShouldHaveIsFeatRegisteredMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            abilityService.IsFeatRegistered(FeatType.Invalid).Returns(true);

            // Act
            var result = abilityService.IsFeatRegistered(FeatType.Invalid);

            // Assert
            Assert.That(result, Is.True);
            abilityService.Received(1).IsFeatRegistered(FeatType.Invalid);
        }

        [Test]
        public void IAbilityService_ShouldHaveGetAbilityDetailMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            var abilityDetail = new AbilityDetail();
            abilityService.GetAbilityDetail(FeatType.Invalid).Returns(abilityDetail);

            // Act
            var result = abilityService.GetAbilityDetail(FeatType.Invalid);

            // Assert
            Assert.That(result, Is.EqualTo(abilityDetail));
            abilityService.Received(1).GetAbilityDetail(FeatType.Invalid);
        }

        [Test]
        public void IAbilityService_ShouldHaveCanUseAbilityMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            abilityService.CanUseAbility(1, 2, FeatType.Invalid, 3, null).Returns(true);

            // Act
            var result = abilityService.CanUseAbility(1, 2, FeatType.Invalid, 3, null);

            // Assert
            Assert.That(result, Is.True);
            abilityService.Received(1).CanUseAbility(1, 2, FeatType.Invalid, 3, null);
        }

        [Test]
        public void IAbilityService_ShouldHaveCanUseConcentrationMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            abilityService.CanUseConcentration(1, FeatType.Invalid).Returns(true);

            // Act
            var result = abilityService.CanUseConcentration(1, FeatType.Invalid);

            // Assert
            Assert.That(result, Is.True);
            abilityService.Received(1).CanUseConcentration(1, FeatType.Invalid);
        }

        [Test]
        public void IAbilityService_ShouldHaveProcessConcentrationEffectsMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ProcessConcentrationEffects();

            // Assert
            abilityService.Received(1).ProcessConcentrationEffects();
        }

        [Test]
        public void IAbilityService_ShouldHaveStartConcentrationAbilityMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.StartConcentrationAbility(1, 2, FeatType.Invalid, StatusEffectType.Invalid);

            // Assert
            abilityService.Received(1).StartConcentrationAbility(1, 2, FeatType.Invalid, StatusEffectType.Invalid);
        }

        [Test]
        public void IAbilityService_ShouldHaveGetActiveConcentrationMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            var activeConcentration = new ActiveConcentrationAbility(uint.MinValue, FeatType.ForceBody1, StatusEffectType.ForceAttunement);
            abilityService.GetActiveConcentration(1).Returns(activeConcentration);

            // Act
            var result = abilityService.GetActiveConcentration(1);

            // Assert
            Assert.That(result, Is.EqualTo(activeConcentration));
            abilityService.Received(1).GetActiveConcentration(1);
        }

        [Test]
        public void IAbilityService_ShouldHaveEndConcentrationAbilityMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.EndConcentrationAbility(1);

            // Assert
            abilityService.Received(1).EndConcentrationAbility(1);
        }

        [Test]
        public void IAbilityService_ShouldHaveToggleAbilityMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ToggleAbility(1, AbilityToggleType.Invalid, true);

            // Assert
            abilityService.Received(1).ToggleAbility(1, AbilityToggleType.Invalid, true);
        }

        [Test]
        public void IAbilityService_ShouldHaveIsAbilityToggledWithUintMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            abilityService.IsAbilityToggled(1, AbilityToggleType.Invalid).Returns(true);

            // Act
            var result = abilityService.IsAbilityToggled(1, AbilityToggleType.Invalid);

            // Assert
            Assert.That(result, Is.True);
            abilityService.Received(1).IsAbilityToggled(1, AbilityToggleType.Invalid);
        }

        [Test]
        public void IAbilityService_ShouldHaveIsAbilityToggledWithStringMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            abilityService.IsAbilityToggled("player-1", AbilityToggleType.Invalid).Returns(true);

            // Act
            var result = abilityService.IsAbilityToggled("player-1", AbilityToggleType.Invalid);

            // Assert
            Assert.That(result, Is.True);
            abilityService.Received(1).IsAbilityToggled("player-1", AbilityToggleType.Invalid);
        }

        [Test]
        public void IAbilityService_ShouldHaveIsAnyAbilityToggledMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            abilityService.IsAnyAbilityToggled(1).Returns(true);

            // Act
            var result = abilityService.IsAnyAbilityToggled(1);

            // Assert
            Assert.That(result, Is.True);
            abilityService.Received(1).IsAnyAbilityToggled(1);
        }

        [Test]
        public void IAbilityService_ShouldHaveAddLeadershipCombatPointMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.AddLeadershipCombatPoint();

            // Assert
            abilityService.Received(1).AddLeadershipCombatPoint();
        }

        [Test]
        public void IAbilityService_ShouldHaveApplyAuraMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ApplyAura(1, StatusEffectType.Invalid, true, true, true);

            // Assert
            abilityService.Received(1).ApplyAura(1, StatusEffectType.Invalid, true, true, true);
        }

        [Test]
        public void IAbilityService_ShouldHaveToggleAuraMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();
            abilityService.ToggleAura(1, StatusEffectType.Invalid).Returns(true);

            // Act
            var result = abilityService.ToggleAura(1, StatusEffectType.Invalid);

            // Assert
            Assert.That(result, Is.True);
            abilityService.Received(1).ToggleAura(1, StatusEffectType.Invalid);
        }

        [Test]
        public void IAbilityService_ShouldHaveReapplyPlayerAuraAOEMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ReapplyPlayerAuraAOE(1);

            // Assert
            abilityService.Received(1).ReapplyPlayerAuraAOE(1);
        }

        [Test]
        public void IAbilityService_ShouldHaveApplyAuraAOEMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ApplyAuraAOE();

            // Assert
            abilityService.Received(1).ApplyAuraAOE();
        }

        [Test]
        public void IAbilityService_ShouldHaveClearAurasOnExitMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ClearAurasOnExit();

            // Assert
            abilityService.Received(1).ClearAurasOnExit();
        }

        [Test]
        public void IAbilityService_ShouldHaveClearAurasOnDeathMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ClearAurasOnDeath();

            // Assert
            abilityService.Received(1).ClearAurasOnDeath();
        }

        [Test]
        public void IAbilityService_ShouldHaveReapplyAuraOnRespawnMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ReapplyAuraOnRespawn();

            // Assert
            abilityService.Received(1).ReapplyAuraOnRespawn();
        }

        [Test]
        public void IAbilityService_ShouldHaveClearAurasOnSpaceEntryMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ClearAurasOnSpaceEntry();

            // Assert
            abilityService.Received(1).ClearAurasOnSpaceEntry();
        }

        [Test]
        public void IAbilityService_ShouldHaveAuraEnterMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.AuraEnter();

            // Assert
            abilityService.Received(1).AuraEnter();
        }

        [Test]
        public void IAbilityService_ShouldHaveAuraExitMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.AuraExit();

            // Assert
            abilityService.Received(1).AuraExit();
        }

        [Test]
        public void IAbilityService_ShouldHaveApplyTemporaryImmunityMethod()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act
            abilityService.ApplyTemporaryImmunity(1, 5.0f, ImmunityType.None);

            // Assert
            abilityService.Received(1).ApplyTemporaryImmunity(1, 5.0f, ImmunityType.None);
        }

        [Test]
        public void IAbilityService_ShouldHaveAllRequiredMethods()
        {
            // Arrange
            var abilityService = Substitute.For<IAbilityService>();

            // Act & Assert - This test ensures all methods exist and can be called
            abilityService.CacheData();
            abilityService.CacheAbilities();
            abilityService.CacheToggleActions();
            abilityService.IsFeatRegistered(FeatType.Invalid);
            abilityService.GetAbilityDetail(FeatType.Invalid);
            abilityService.CanUseAbility(1, 2, FeatType.Invalid, 3, null);
            abilityService.CanUseConcentration(1, FeatType.Invalid);
            abilityService.ProcessConcentrationEffects();
            abilityService.StartConcentrationAbility(1, 2, FeatType.Invalid, StatusEffectType.Invalid);
            abilityService.GetActiveConcentration(1);
            abilityService.EndConcentrationAbility(1);
            abilityService.ToggleAbility(1, AbilityToggleType.Invalid, true);
            abilityService.IsAbilityToggled(1, AbilityToggleType.Invalid);
            abilityService.IsAbilityToggled("player-1", AbilityToggleType.Invalid);
            abilityService.IsAnyAbilityToggled(1);
            abilityService.AddLeadershipCombatPoint();
            abilityService.ApplyAura(1, StatusEffectType.Invalid, true, true, true);
            abilityService.ToggleAura(1, StatusEffectType.Invalid);
            abilityService.ReapplyPlayerAuraAOE(1);
            abilityService.ApplyAuraAOE();
            abilityService.ClearAurasOnExit();
            abilityService.ClearAurasOnDeath();
            abilityService.ReapplyAuraOnRespawn();
            abilityService.ClearAurasOnSpaceEntry();
            abilityService.AuraEnter();
            abilityService.AuraExit();
            abilityService.ApplyTemporaryImmunity(1, 5.0f, ImmunityType.None);

            // If we get here without exceptions, all methods exist
            Assert.Pass("All methods exist and can be called");
        }
    }
}
