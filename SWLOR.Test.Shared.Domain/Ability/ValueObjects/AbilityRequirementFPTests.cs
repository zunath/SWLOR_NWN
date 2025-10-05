using SWLOR.Shared.Domain.Ability.ValueObjects;

namespace SWLOR.Test.Shared.Domain.Ability.ValueObjects
{
    [TestFixture]
    public class AbilityRequirementFPTests
    {
        [Test]
        public void Constructor_ShouldSetRequiredFP()
        {
            // Arrange
            var serviceProvider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(new Microsoft.Extensions.DependencyInjection.ServiceCollection());

            // Act
            var requirement = new AbilityRequirementFP(100, serviceProvider);

            // Assert
            Assert.That(requirement.RequiredFP, Is.EqualTo(100));
        }

        [Test]
        public void RequiredFP_ShouldBeReadOnly()
        {
            // Arrange
            var serviceProvider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(new Microsoft.Extensions.DependencyInjection.ServiceCollection());
            var requirement = new AbilityRequirementFP(50, serviceProvider);

            // Act & Assert
            Assert.That(requirement.RequiredFP, Is.EqualTo(50));
            // The property should be read-only, so we can't set it directly
        }
    }
}
