using NSubstitute;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;


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
            abilityService.ToggleAbility(1, AbilityToggleType.Invalid, true);
            abilityService.IsAbilityToggled(1, AbilityToggleType.Invalid);
            abilityService.IsAbilityToggled("player-1", AbilityToggleType.Invalid);
            abilityService.IsAnyAbilityToggled(1);
            abilityService.AddLeadershipCombatPoint();
            abilityService.ApplyTemporaryImmunity(1, 5.0f, ImmunityType.None);

            // If we get here without exceptions, all methods exist
            Assert.Pass("All methods exist and can be called");
        }
    }
}
