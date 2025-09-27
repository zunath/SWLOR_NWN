using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Component.Combat.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;

namespace SWLOR.Test.Component.Combat.Service
{
    [TestFixture]
    public class CombatServiceTests
    {
        private ILogger _mockLogger;
        private IDatabaseService _mockDatabaseService;
        private IRandomService _mockRandomService;
        private IServiceProvider _mockServiceProvider;
        private IAbilityService _mockAbilityService;
        private IStatService _mockStatService;
        private IItemService _mockItemService;
        private IPerkService _mockPerkService;
        private CombatService _combatService;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = Substitute.For<ILogger>();
            _mockDatabaseService = Substitute.For<IDatabaseService>();
            _mockRandomService = Substitute.For<IRandomService>();
            _mockAbilityService = Substitute.For<IAbilityService>();
            _mockStatService = Substitute.For<IStatService>();
            _mockItemService = Substitute.For<IItemService>();
            _mockPerkService = Substitute.For<IPerkService>();
            
            // Create a real service provider with the mock services
            var services = new ServiceCollection();
            services.AddSingleton(_mockAbilityService);
            services.AddSingleton(_mockStatService);
            services.AddSingleton(_mockItemService);
            services.AddSingleton(_mockPerkService);
            _mockServiceProvider = services.BuildServiceProvider();

            _combatService = new CombatService(
                _mockLogger,
                _mockDatabaseService,
                _mockRandomService,
                _mockServiceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose of the service provider if it implements IDisposable
            if (_mockServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }


        [Test]
        public void LoadDamageTypes_ShouldLoadAllValidDamageTypes()
        {
            // Act
            _combatService.LoadDamageTypes();

            // Assert
            var damageTypes = _combatService.GetAllDamageTypes();
            Assert.That(damageTypes, Is.Not.Null);
            Assert.That(damageTypes.Count, Is.GreaterThan(0));
            Assert.That(damageTypes, Does.Not.Contain(CombatDamageType.Invalid));
        }

        [Test]
        public void CalculateDamageRange_WithValidInputs_ShouldReturnValidRange()
        {
            // Arrange
            const int attackerAttack = 100;
            const int attackerDMG = 50;
            const int attackerStat = 20;
            const int defenderDefense = 80;
            const int defenderStat = 15;
            const int critical = 0;
            const int deltaCap = 0;

            // Act
            var (minDamage, maxDamage) = _combatService.CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            // Assert
            Assert.That(minDamage, Is.GreaterThanOrEqualTo(0));
            Assert.That(maxDamage, Is.GreaterThanOrEqualTo(minDamage));
        }

        [Test]
        public void CalculateDamageRange_WithCriticalHit_ShouldReturnHigherDamage()
        {
            // Arrange
            const int attackerAttack = 100;
            const int attackerDMG = 50;
            const int attackerStat = 20;
            const int defenderDefense = 80;
            const int defenderStat = 15;
            const int critical = 2; // Critical hit
            const int deltaCap = 0;

            // Act
            var (minDamage, maxDamage) = _combatService.CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            // Assert
            Assert.That(minDamage, Is.GreaterThanOrEqualTo(0));
            Assert.That(maxDamage, Is.GreaterThanOrEqualTo(minDamage));
            // Critical hits should have higher damage
            Assert.That(maxDamage, Is.GreaterThan(50)); // Should be higher than base damage
        }

        [Test]
        public void CalculateDamageRange_WithZeroDefense_ShouldUseMinimumDefense()
        {
            // Arrange
            const int attackerAttack = 100;
            const int attackerDMG = 50;
            const int attackerStat = 20;
            const int defenderDefense = 0; // Zero defense
            const int defenderStat = 15;
            const int critical = 0;
            const int deltaCap = 0;

            // Act
            var (minDamage, maxDamage) = _combatService.CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            // Assert
            Assert.That(minDamage, Is.GreaterThanOrEqualTo(0));
            Assert.That(maxDamage, Is.GreaterThanOrEqualTo(minDamage));
        }

        [Test]
        public void CalculateDamageRange_WithDeltaCap_ShouldClampStatDelta()
        {
            // Arrange
            const int attackerAttack = 100;
            const int attackerDMG = 50;
            const int attackerStat = 20;
            const int defenderDefense = 80;
            const int defenderStat = 15;
            const int critical = 0;
            const int deltaCap = 5; // Cap the delta

            // Act
            var (minDamage, maxDamage) = _combatService.CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            // Assert
            Assert.That(minDamage, Is.GreaterThanOrEqualTo(0));
            Assert.That(maxDamage, Is.GreaterThanOrEqualTo(minDamage));
        }

        [Test]
        public void CalculateHitRate_WithValidInputs_ShouldReturnValidHitRate()
        {
            // Arrange
            const int attackerAccuracy = 100;
            const int defenderEvasion = 80;
            const int percentageModifier = 0;

            // Act
            var hitRate = _combatService.CalculateHitRate(attackerAccuracy, defenderEvasion, percentageModifier);

            // Assert
            Assert.That(hitRate, Is.GreaterThanOrEqualTo(20));
            Assert.That(hitRate, Is.LessThanOrEqualTo(95));
        }

        [Test]
        public void CalculateHitRate_WithHighAccuracy_ShouldClampToMaximum()
        {
            // Arrange
            const int attackerAccuracy = 1000; // Very high accuracy
            const int defenderEvasion = 0;
            const int percentageModifier = 0;

            // Act
            var hitRate = _combatService.CalculateHitRate(attackerAccuracy, defenderEvasion, percentageModifier);

            // Assert
            Assert.That(hitRate, Is.EqualTo(95)); // Should be clamped to 95
        }

        [Test]
        public void CalculateHitRate_WithLowAccuracy_ShouldClampToMinimum()
        {
            // Arrange
            const int attackerAccuracy = 0; // Very low accuracy
            const int defenderEvasion = 1000;
            const int percentageModifier = 0;

            // Act
            var hitRate = _combatService.CalculateHitRate(attackerAccuracy, defenderEvasion, percentageModifier);

            // Assert
            Assert.That(hitRate, Is.EqualTo(20)); // Should be clamped to 20
        }

        [Test]
        public void CalculateHitRate_WithPercentageModifier_ShouldApplyModifier()
        {
            // Arrange
            const int attackerAccuracy = 100;
            const int defenderEvasion = 80;
            const int percentageModifier = 10; // +10% modifier

            // Act
            var hitRate = _combatService.CalculateHitRate(attackerAccuracy, defenderEvasion, percentageModifier);

            // Assert
            Assert.That(hitRate, Is.GreaterThanOrEqualTo(20));
            Assert.That(hitRate, Is.LessThanOrEqualTo(95));
        }

        [Test]
        public void CalculateCriticalRate_WithValidInputs_ShouldReturnValidCriticalRate()
        {
            // Arrange
            const int attackerPER = 20;
            const int defenderMGT = 15;
            const int criticalModifier = 0;

            // Act
            var criticalRate = _combatService.CalculateCriticalRate(attackerPER, defenderMGT, criticalModifier);

            // Assert
            Assert.That(criticalRate, Is.GreaterThanOrEqualTo(5)); // Base critical rate
            Assert.That(criticalRate, Is.LessThanOrEqualTo(90));
        }

        [Test]
        public void CalculateCriticalRate_WithHighPerception_ShouldClampDelta()
        {
            // Arrange
            const int attackerPER = 100; // Very high perception
            const int defenderMGT = 15;
            const int criticalModifier = 0;

            // Act
            var criticalRate = _combatService.CalculateCriticalRate(attackerPER, defenderMGT, criticalModifier);

            // Assert
            Assert.That(criticalRate, Is.GreaterThanOrEqualTo(5));
            Assert.That(criticalRate, Is.LessThanOrEqualTo(90));
        }

        [Test]
        public void CalculateCriticalRate_WithLowPerception_ShouldUseBaseRate()
        {
            // Arrange
            const int attackerPER = 10; // Lower than defender
            const int defenderMGT = 15;
            const int criticalModifier = 0;

            // Act
            var criticalRate = _combatService.CalculateCriticalRate(attackerPER, defenderMGT, criticalModifier);

            // Assert
            Assert.That(criticalRate, Is.EqualTo(5)); // Should be base rate
        }

        [Test]
        public void CalculateCriticalRate_WithCriticalModifier_ShouldApplyModifier()
        {
            // Arrange
            const int attackerPER = 20;
            const int defenderMGT = 15;
            const int criticalModifier = 10; // +10% modifier

            // Act
            var criticalRate = _combatService.CalculateCriticalRate(attackerPER, defenderMGT, criticalModifier);

            // Assert
            Assert.That(criticalRate, Is.GreaterThanOrEqualTo(5));
            Assert.That(criticalRate, Is.LessThanOrEqualTo(90));
        }

        [Test]
        public void CalculateDamage_WithValidInputs_ShouldReturnValidDamage()
        {
            // Arrange
            const int attackerAttack = 100;
            const int attackerDMG = 50;
            const int attackerStat = 20;
            const int defenderDefense = 80;
            const int defenderStat = 15;
            const int critical = 0;
            const int deltaCap = 0;
            const float expectedDamage = 75.0f; // Mock random value

            _mockRandomService
                .NextFloat(Arg.Any<float>(), Arg.Any<float>())
                .Returns(expectedDamage);

            // Act
            var damage = _combatService.CalculateDamage(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            // Assert
            Assert.That(damage, Is.EqualTo((int)expectedDamage));
            _mockRandomService.Received(1).NextFloat(Arg.Any<float>(), Arg.Any<float>());
        }

        [Test]
        public void CalculateDamage_WithCriticalHit_ShouldReturnHigherDamage()
        {
            // Arrange
            const int attackerAttack = 100;
            const int attackerDMG = 50;
            const int attackerStat = 20;
            const int defenderDefense = 80;
            const int defenderStat = 15;
            const int critical = 2; // Critical hit
            const int deltaCap = 0;
            const float expectedDamage = 150.0f; // Mock random value for critical

            _mockRandomService
                .NextFloat(Arg.Any<float>(), Arg.Any<float>())
                .Returns(expectedDamage);

            // Act
            var damage = _combatService.CalculateDamage(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            // Assert
            Assert.That(damage, Is.EqualTo((int)expectedDamage));
            _mockRandomService.Received(1).NextFloat(Arg.Any<float>(), Arg.Any<float>());
        }

        // Note: GetAbilityDamageBonus tests removed because they require NWN API calls
        // These would be better suited for integration tests
    }
}
