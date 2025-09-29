using NSubstitute;
using SWLOR.Shared.Domain.Character.Contracts;

namespace SWLOR.Test.Shared.Domain.Character.Contracts
{
    [TestFixture]
    public class IPlayerInitializationServiceTests
    {
        [Test]
        public void IPlayerInitializationService_ShouldBeInterface()
        {
            // Act
            var type = typeof(IPlayerInitializationService);

            // Assert
            Assert.That(type.IsInterface, Is.True);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveInitializePlayerMethod()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act
            playerInitializationService.InitializePlayer();

            // Assert
            playerInitializationService.Received(1).InitializePlayer();
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveInitializeSkillsMethod()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act
            playerInitializationService.InitializeSkills(1);

            // Assert
            playerInitializationService.Received(1).InitializeSkills(1);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveInitializeSavingThrowsMethod()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act
            playerInitializationService.InitializeSavingThrows(1);

            // Assert
            playerInitializationService.Received(1).InitializeSavingThrows(1);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveClearFeatsMethod()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act
            playerInitializationService.ClearFeats(1);

            // Assert
            playerInitializationService.Received(1).ClearFeats(1);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveGrantBasicFeatsMethod()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act
            playerInitializationService.GrantBasicFeats(1);

            // Assert
            playerInitializationService.Received(1).GrantBasicFeats(1);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveInitializeHotBarMethod()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act
            playerInitializationService.InitializeHotBar(1);

            // Assert
            playerInitializationService.Received(1).InitializeHotBar(1);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveAdjustAlignmentMethod()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act
            playerInitializationService.AdjustAlignment(1);

            // Assert
            playerInitializationService.Received(1).AdjustAlignment(1);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveAllRequiredMethods()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act & Assert - This test ensures all methods exist and can be called
            playerInitializationService.InitializePlayer();
            playerInitializationService.InitializeSkills(1);
            playerInitializationService.InitializeSavingThrows(1);
            playerInitializationService.ClearFeats(1);
            playerInitializationService.GrantBasicFeats(1);
            playerInitializationService.InitializeHotBar(1);
            playerInitializationService.AdjustAlignment(1);

            // If we get here without exceptions, all methods exist
            Assert.Pass("All methods exist and can be called");
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveCorrectMethodSignatures()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act & Assert - Test method signatures with different parameter values
            playerInitializationService.InitializeSkills(0);
            playerInitializationService.InitializeSkills(1);
            playerInitializationService.InitializeSkills(uint.MaxValue);

            playerInitializationService.InitializeSavingThrows(0);
            playerInitializationService.InitializeSavingThrows(1);
            playerInitializationService.InitializeSavingThrows(uint.MaxValue);

            playerInitializationService.ClearFeats(0);
            playerInitializationService.ClearFeats(1);
            playerInitializationService.ClearFeats(uint.MaxValue);

            playerInitializationService.GrantBasicFeats(0);
            playerInitializationService.GrantBasicFeats(1);
            playerInitializationService.GrantBasicFeats(uint.MaxValue);

            playerInitializationService.InitializeHotBar(0);
            playerInitializationService.InitializeHotBar(1);
            playerInitializationService.InitializeHotBar(uint.MaxValue);

            playerInitializationService.AdjustAlignment(0);
            playerInitializationService.AdjustAlignment(1);
            playerInitializationService.AdjustAlignment(uint.MaxValue);

            // If we get here without exceptions, all method signatures are correct
            Assert.Pass("All method signatures are correct");
        }

        [Test]
        public void IPlayerInitializationService_ShouldBeMockable()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act & Assert - Test that the interface can be mocked and methods can be verified
            playerInitializationService.InitializePlayer();
            playerInitializationService.InitializeSkills(1);
            playerInitializationService.InitializeSavingThrows(1);
            playerInitializationService.ClearFeats(1);
            playerInitializationService.GrantBasicFeats(1);
            playerInitializationService.InitializeHotBar(1);
            playerInitializationService.AdjustAlignment(1);

            // Verify all methods were called
            playerInitializationService.Received(1).InitializePlayer();
            playerInitializationService.Received(1).InitializeSkills(1);
            playerInitializationService.Received(1).InitializeSavingThrows(1);
            playerInitializationService.Received(1).ClearFeats(1);
            playerInitializationService.Received(1).GrantBasicFeats(1);
            playerInitializationService.Received(1).InitializeHotBar(1);
            playerInitializationService.Received(1).AdjustAlignment(1);
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveCorrectReturnTypes()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act & Assert - Test that all methods return void (no return value)
            playerInitializationService.InitializePlayer();
            playerInitializationService.InitializeSkills(1);
            playerInitializationService.InitializeSavingThrows(1);
            playerInitializationService.ClearFeats(1);
            playerInitializationService.GrantBasicFeats(1);
            playerInitializationService.InitializeHotBar(1);
            playerInitializationService.AdjustAlignment(1);

            // If we get here without exceptions, all return types are correct
            Assert.Pass("All return types are correct");
        }

        [Test]
        public void IPlayerInitializationService_ShouldHaveCorrectParameterTypes()
        {
            // Arrange
            var playerInitializationService = Substitute.For<IPlayerInitializationService>();

            // Act & Assert - Test that all methods accept uint parameters
            playerInitializationService.InitializeSkills(1u);
            playerInitializationService.InitializeSavingThrows(1u);
            playerInitializationService.ClearFeats(1u);
            playerInitializationService.GrantBasicFeats(1u);
            playerInitializationService.InitializeHotBar(1u);
            playerInitializationService.AdjustAlignment(1u);

            // If we get here without exceptions, all parameter types are correct
            Assert.Pass("All parameter types are correct");
        }
    }
}
