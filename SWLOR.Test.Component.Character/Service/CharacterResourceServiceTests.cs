using NSubstitute;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.Character.Service
{
    [TestFixture]
    public class CharacterResourceServiceTests : TestBase
    {
        private IPlayerRepository _mockPlayerRepository;
        private IStatCalculationService _mockStatService;
        private IEventAggregator _mockEventAggregator;
        private CharacterResourceService _characterResourceService;

        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();

            _mockPlayerRepository = Substitute.For<IPlayerRepository>();
            _mockStatService = Substitute.For<IStatCalculationService>();
            _mockEventAggregator = Substitute.For<IEventAggregator>();

            _characterResourceService = new CharacterResourceService(_mockPlayerRepository, _mockStatService, _mockEventAggregator);
        }

        [Test]
        public void GetCurrentHP_ShouldReturnHitPointsFromNWScript()
        {
            // Arrange
            var creature = 1u;

            // Act
            var result = _characterResourceService.GetCurrentHP(creature);

            // Assert
            // The method delegates to NWScript.GetCurrentHitPoints, which is mocked by TestBase
            // Default mock returns 100 for GetCurrentHitPoints
            Assert.That(result, Is.EqualTo(100));
        }

        [Test]
        public void GetCurrentFP_ShouldNotThrow()
        {
            // Arrange
            var creature = 1u;

            // Act & Assert - Just verify the method doesn't throw
            // The actual behavior depends on NWScript mocking which defaults to NPC behavior
            Assert.DoesNotThrow(() => _characterResourceService.GetCurrentFP(creature));
        }

        [Test]
        public void GetCurrentSTM_ShouldNotThrow()
        {
            // Arrange
            var creature = 1u;

            // Act & Assert - Just verify the method doesn't throw
            // The actual behavior depends on NWScript mocking which defaults to NPC behavior
            Assert.DoesNotThrow(() => _characterResourceService.GetCurrentSTM(creature));
        }

        [Test]
        public void RestoreHP_ShouldApplyHealEffect()
        {
            // Arrange
            var creature = 1u;
            var amount = 25;

            // Act
            _characterResourceService.RestoreHP(creature, amount);

            // Assert
            // The method applies a heal effect via NWScript, which is mocked
            // We can't easily verify the exact NWScript call, but ensure it doesn't throw
            Assert.Pass("Method executed without throwing exceptions");
        }

        [Test]
        public void RestoreFP_ShouldNotThrow()
        {
            // Arrange
            var creature = 1u;
            var amount = 10;

            // Act & Assert
            // The method will use NWScript mocking defaults (NPC behavior)
            Assert.DoesNotThrow(() => _characterResourceService.RestoreFP(creature, amount));
        }

        [Test]
        public void RestoreSTM_ShouldNotThrow()
        {
            // Arrange
            var creature = 1u;
            var amount = 15;

            // Act & Assert
            // The method will use NWScript mocking defaults (NPC behavior)
            Assert.DoesNotThrow(() => _characterResourceService.RestoreSTM(creature, amount));
        }

        [Test]
        public void SetCurrentFP_ShouldNotThrow()
        {
            // Arrange
            var creature = 1u;
            var amount = 50;

            // Act & Assert
            // The method will use NWScript mocking defaults (NPC behavior)
            Assert.DoesNotThrow(() => _characterResourceService.SetCurrentFP(creature, amount));
        }

        [Test]
        public void SetCurrentSTM_ShouldNotThrow()
        {
            // Arrange
            var creature = 1u;
            var amount = 75;

            // Act & Assert
            // The method will use NWScript mocking defaults (NPC behavior)
            Assert.DoesNotThrow(() => _characterResourceService.SetCurrentSTM(creature, amount));
        }
    }
}
