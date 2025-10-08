using NSubstitute;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.Events;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.Character.Service
{
    [TestFixture]
    public class CharacterStatServiceTests : TestBase
    {
        private IPlayerRepository _mockPlayerRepository;
        private IEventAggregator _mockEventAggregator;
        private CharacterStatService _characterStatService;

        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();

            _mockPlayerRepository = Substitute.For<IPlayerRepository>();
            _mockEventAggregator = Substitute.For<IEventAggregator>();

            _characterStatService = new CharacterStatService(_mockPlayerRepository, _mockEventAggregator);
        }

        [Test]
        public void CharacterStatService_ConstructsSuccessfully()
        {
            // Arrange & Act & Assert
            Assert.That(_characterStatService, Is.Not.Null);
        }

        [Test]
        public void ModifyMaxHP_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 100;
            var adjustment = 25;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.MaxHP, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyMaxHP(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.MaxHP] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterMaxHPChanged>(), creature);
        }

        [Test]
        public void ModifyMaxFP_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 50;
            var adjustment = -10;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.MaxFP, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyMaxFP(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.MaxFP] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterMaxFPChanged>(), creature);
        }

        [Test]
        public void ModifyMaxSTM_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 75;
            var adjustment = 15;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.MaxSTM, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyMaxSTM(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.MaxSTM] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterMaxSTMChanged>(), creature);
        }

        [Test]
        public void ModifyHPRegen_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 5;
            var adjustment = 2;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.HPRegen, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyHPRegen(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.HPRegen] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterHPRegenChanged>(), creature);
        }

        [Test]
        public void ModifyFPRegen_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 3;
            var adjustment = -1;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.FPRegen, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyFPRegen(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.FPRegen] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterFPRegenChanged>(), creature);
        }

        [Test]
        public void ModifySTMRegen_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 4;
            var adjustment = 1;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.STMRegen, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifySTMRegen(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.STMRegen] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterSTMRegenChanged>(), creature);
        }

        [Test]
        public void ModifyDefense_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 20;
            var adjustment = 5;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Defense, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyDefense(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Defense] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterDefenseChanged>(), creature);
        }

        [Test]
        public void ModifyAttack_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 15;
            var adjustment = 3;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Attack, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyAttack(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Attack] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterAttackChanged>(), creature);
        }

        [Test]
        public void ModifyRecastReduction_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 10;
            var adjustment = -2;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.RecastReduction, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyRecastReduction(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.RecastReduction] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterRecastReductionChanged>(), creature);
        }

        [Test]
        public void ModifyEvasion_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 8;
            var adjustment = 4;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Evasion, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyEvasion(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Evasion] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterEvasionChanged>(), creature);
        }

        [Test]
        public void ModifyAccuracy_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 12;
            var adjustment = 6;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Accuracy, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyAccuracy(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Accuracy] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterAccuracyChanged>(), creature);
        }

        [Test]
        public void ModifyForceAttack_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 18;
            var adjustment = 7;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.ForceAttack, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyForceAttack(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.ForceAttack] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterForceAttackChanged>(), creature);
        }

        [Test]
        public void ModifyMight_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 14;
            var adjustment = -3;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Might, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyMight(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Might] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterMightChanged>(), creature);
        }

        [Test]
        public void ModifyPerception_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 11;
            var adjustment = 2;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Perception, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyPerception(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Perception] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterPerceptionChanged>(), creature);
        }

        [Test]
        public void ModifyVitality_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 16;
            var adjustment = 4;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Vitality, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyVitality(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Vitality] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterVitalityChanged>(), creature);
        }

        [Test]
        public void ModifyAgility_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 13;
            var adjustment = 5;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Agility, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyAgility(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Agility] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterAgilityChanged>(), creature);
        }

        [Test]
        public void ModifyWillpower_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 17;
            var adjustment = -1;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Willpower, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyWillpower(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Willpower] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterWillpowerChanged>(), creature);
        }

        [Test]
        public void ModifySocial_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 9;
            var adjustment = 3;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Social, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifySocial(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Social] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterSocialChanged>(), creature);
        }

        // Additional tests for remaining Modify methods - testing a few more to show pattern
        [Test]
        public void ModifyLevel_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 5;
            var adjustment = 1;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Level, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyLevel(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Level] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterLevelChanged>(), creature);
        }

        [Test]
        public void ModifyShieldDeflection_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 6;
            var adjustment = 2;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.ShieldDeflection, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyShieldDeflection(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.ShieldDeflection] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterShieldDeflectionChanged>(), creature);
        }

        [Test]
        public void ModifyAttackDeflection_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 7;
            var adjustment = -2;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.AttackDeflection, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyAttackDeflection(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.AttackDeflection] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterAttackDeflectionChanged>(), creature);
        }

        [Test]
        public void ModifyCriticalRate_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 5;
            var adjustment = 1;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.CriticalRate, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyCriticalRate(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.CriticalRate] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterCriticalRateChanged>(), creature);
        }

        [Test]
        public void ModifyEnmity_ShouldAddAdjustmentToCurrentValue()
        {
            // Arrange
            var creature = 1u;
            var currentValue = 10;
            var adjustment = 5;
            var expectedNewValue = currentValue + adjustment;

            // Setup mock player
            var mockPlayer = new Player { Stats = new Dictionary<StatType, int> { { StatType.Enmity, currentValue } } };
            _mockPlayerRepository.GetById(Arg.Any<string>()).Returns(mockPlayer);

            // Act
            _characterStatService.ModifyEnmity(creature, adjustment);

            // Assert
            _mockPlayerRepository.Received(1).Save(Arg.Is<Player>(p => p.Stats[StatType.Enmity] == expectedNewValue));
            _mockEventAggregator.Received(1).Publish(Arg.Any<OnCharacterEnmityChanged>(), creature);
        }
    }
}
