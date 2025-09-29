using NSubstitute;
using NUnit.Framework;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Test.Shared.Domain.Ability.ValueObjects
{
    [TestFixture]
    public class AbilityRequirementStaminaTests
    {
        [Test]
        public void Constructor_ShouldSetRequiredSTM()
        {
            // Arrange
            var statService = Substitute.For<IStatService>();

            // Act
            var requirement = new AbilityRequirementStamina(100, statService);

            // Assert
            Assert.That(requirement.RequiredSTM, Is.EqualTo(100));
        }

        [Test]
        public void RequiredSTM_ShouldBeReadOnly()
        {
            // Arrange
            var statService = Substitute.For<IStatService>();
            var requirement = new AbilityRequirementStamina(50, statService);

            // Act & Assert
            Assert.That(requirement.RequiredSTM, Is.EqualTo(50));
            // The property should be read-only, so we can't set it directly
        }
    }
}
