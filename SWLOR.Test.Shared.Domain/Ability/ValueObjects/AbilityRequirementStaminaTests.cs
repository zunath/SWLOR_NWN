using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Shared.Domain.Ability.ValueObjects;

namespace SWLOR.Test.Shared.Domain.Ability.ValueObjects
{
    [TestFixture]
    public class AbilityRequirementStaminaTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void Constructor_ShouldSetRequiredSTM()
        {
            // Arrange
            var serviceProvider = Substitute.For<IServiceProvider>();

            // Act
            var requirement = new AbilityRequirementStamina(100, serviceProvider);

            // Assert
            Assert.That(requirement.RequiredSTM, Is.EqualTo(100));
        }

        [Test]
        public void RequiredSTM_ShouldBeReadOnly()
        {
            // Arrange
            var serviceProvider = Substitute.For<IServiceProvider>();
            var requirement = new AbilityRequirementStamina(50, serviceProvider);

            // Act & Assert
            Assert.That(requirement.RequiredSTM, Is.EqualTo(50));
            // The property should be read-only, so we can't set it directly
        }
    }
}
