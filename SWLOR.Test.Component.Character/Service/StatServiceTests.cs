using System.Reflection;
using NSubstitute;
using SWLOR.Component.Character.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.Character.Service
{
    [TestFixture]
    public class StatCalculationServiceTests : TestBase
    {
        private IStatGroupService _mockStatGroupService;
        private IStatusEffectService _mockStatusEffectService;
        private ISkillService _mockSkillService;
        private StatCalculationService _statService;

        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
            
            _mockStatGroupService = Substitute.For<IStatGroupService>();
            _mockStatusEffectService = Substitute.For<IStatusEffectService>();
            _mockSkillService = Substitute.For<ISkillService>();
            
            _statService = new StatCalculationService(_mockStatGroupService, _mockStatusEffectService, _mockSkillService);
        }

        private float CalculateAttackSpeedModifier(uint creature)
        {
            var method = typeof(StatCalculationService).GetMethod("CalculateAttackSpeedModifier", BindingFlags.NonPublic | BindingFlags.Instance);
            return (float)method.Invoke(_statService, new object[] { creature });
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithHaste_ReturnsNegativeModifier()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Haste, 30); // 30% haste
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(-0.3f).Within(0.0001f)); // 30% haste = -0.3 modifier
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithSlow_ReturnsPositiveModifier()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Slow, 50); // 50% slow
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.5f)); // 50% slow = +0.5 modifier
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithHasteAndSlow_ReturnsNetModifier()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Haste, 20); // 20% haste
            stats.SetStat(StatType.Slow, 30); // 30% slow
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.1f).Within(0.0001f)); // (30 - 20) * 0.01 = 0.1 modifier
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithStatusEffectHaste_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Haste, 10); // 10% haste from stats
            effects.SetStat(StatType.Haste, 20); // 20% haste from status effects
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(-0.3f).Within(0.0001f)); // (0 - 30) * 0.01 = -0.3 modifier
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithStatusEffectSlow_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Slow, 25); // 25% slow from stats
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 15); // 15% slow from status effects
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.4f).Within(0.0001f)); // (40 - 0) * 0.01 = 0.4 modifier
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithExcessiveHaste_CapsAt50Percent()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Haste, 100); // 100% haste (should be capped)
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(-0.5f).Within(0.0001f)); // Capped at -0.5 modifier
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithExcessiveSlow_NoCap()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Slow, 200); // 200% slow (no cap)
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(2.0f).Within(0.0001f)); // 200% slow = 2.0 modifier (no cap)
        }

        [Test]
        public void CalculateAttackSpeedModifier_WithNegativeValues_ClampsToZero()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Haste, -10); // Negative haste (should be clamped to 0)
            stats.SetStat(StatType.Slow, -5); // Negative slow (should be clamped to 0)
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = CalculateAttackSpeedModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.0f)); // (0 - 0) * 0.01 = 0 modifier
        }

        [Test]
        public void CalculateAttackDelay_WithBaseDelay_ReturnsCorrectMilliseconds()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            // Set weapon delays (60 delay units = 1 second)
            stats.RightHandStat.Delay = 60; // 1 second
            stats.LeftHandStat.Delay = 30; // 0.5 seconds
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAttackDelay(creature);

            // Assert
            // Total delay: 90 units = 1.5 seconds = 1500ms
            Assert.That(result, Is.EqualTo(1500));
        }

        [Test]
        public void CalculateAttackDelay_WithHaste_ReducesDelay()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            // Set weapon delays
            stats.RightHandStat.Delay = 60; // 1 second
            stats.LeftHandStat.Delay = 60; // 1 second
            // Total: 120 units = 2 seconds = 2000ms
            
            // 50% haste
            stats.SetStat(StatType.Haste, 50);
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAttackDelay(creature);

            // Assert
            // Base delay: 2000ms, with 50% haste: 2000 * (1.0 - 0.5) = 1000ms
            Assert.That(result, Is.EqualTo(1000));
        }

        [Test]
        public void CalculateAttackDelay_WithSlow_IncreasesDelay()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            // Set weapon delays
            stats.RightHandStat.Delay = 60; // 1 second
            stats.LeftHandStat.Delay = 60; // 1 second
            // Total: 120 units = 2 seconds = 2000ms
            
            // 100% slow
            stats.SetStat(StatType.Slow, 100);
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAttackDelay(creature);

            // Assert
            // Base delay: 2000ms, with 100% slow: 2000 * (1.0 + 1.0) = 4000ms
            Assert.That(result, Is.EqualTo(4000));
        }

        [Test]
        public void CalculateAttackDelay_WithHasteAndSlow_AppliesNetEffect()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            // Set weapon delays
            stats.RightHandStat.Delay = 60; // 1 second
            stats.LeftHandStat.Delay = 60; // 1 second
            // Total: 120 units = 2 seconds = 2000ms
            
            // 30% haste, 20% slow = net 10% haste (30 - 20 = 10% net haste)
            stats.SetStat(StatType.Haste, 30);
            stats.SetStat(StatType.Slow, 20);
            effects.SetStat(StatType.Haste, 0);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAttackDelay(creature);

            // Assert
            // Base delay: 2000ms, with net 10% haste: 2000 * (1.0 - 0.1) = 1800ms
            Assert.That(result, Is.EqualTo(1800));
        }

        [Test]
        public void CalculateAttackDelay_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            // Set weapon delays
            stats.RightHandStat.Delay = 60; // 1 second
            stats.LeftHandStat.Delay = 60; // 1 second
            // Total: 120 units = 2 seconds = 2000ms
            
            // 20% haste from stats, 30% haste from status effects = 50% total haste
            stats.SetStat(StatType.Haste, 20);
            effects.SetStat(StatType.Haste, 30);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAttackDelay(creature);

            // Assert
            // Base delay: 2000ms, with 50% haste: 2000 * (1.0 - 0.5) = 1000ms
            Assert.That(result, Is.EqualTo(1000));
        }

        #region CalculateMaxHP Tests

        [Test]
        public void CalculateMaxHP_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxHP, 100);
            effects.SetStat(StatType.MaxHP, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxHP(creature);

            // Assert
            // BaseHP (70) + stats (100) + effects (0) = 170
            Assert.That(result, Is.EqualTo(170));
        }

        [Test]
        public void CalculateMaxHP_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxHP, 100);
            effects.SetStat(StatType.MaxHP, 50);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxHP(creature);

            // Assert
            // BaseHP (70) + stats (100) + effects (50) = 220
            Assert.That(result, Is.EqualTo(220));
        }

        #endregion

        #region CalculateMaxFP Tests

        [Test]
        public void CalculateMaxFP_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxFP, 80);
            stats.SetStat(StatType.Willpower, 5); // Willpower modifier
            effects.SetStat(StatType.MaxFP, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxFP(creature);

            // Assert
            // BaseFP (10) + Willpower modifier (5 * 10 = 50) + stats (80) + effects (0) = 140
            Assert.That(result, Is.EqualTo(140));
        }

        [Test]
        public void CalculateMaxFP_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxFP, 80);
            stats.SetStat(StatType.Willpower, 5); // Willpower modifier
            effects.SetStat(StatType.MaxFP, 20);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxFP(creature);

            // Assert
            // BaseFP (10) + Willpower modifier (5 * 10 = 50) + stats (80) + effects (20) = 160
            Assert.That(result, Is.EqualTo(160));
        }

        #endregion

        #region CalculateMaxSTM Tests

        [Test]
        public void CalculateMaxSTM_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxSTM, 60);
            stats.SetStat(StatType.Perception, 4); // Perception modifier
            effects.SetStat(StatType.MaxSTM, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxSTM(creature);

            // Assert
            // BaseSTM (10) + Perception modifier (4 * 10 = 40) + stats (60) + effects (0) = 110
            Assert.That(result, Is.EqualTo(110));
        }

        [Test]
        public void CalculateMaxSTM_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxSTM, 60);
            stats.SetStat(StatType.Perception, 4); // Perception modifier
            effects.SetStat(StatType.MaxSTM, 15);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxSTM(creature);

            // Assert
            // BaseSTM (10) + Perception modifier (4 * 10 = 40) + stats (60) + effects (15) = 125
            Assert.That(result, Is.EqualTo(125));
        }

        #endregion

        #region CalculateHPRegen Tests

        [Test]
        public void CalculateHPRegen_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.HPRegen, 5);
            stats.SetStat(StatType.Vitality, 10);
            effects.SetStat(StatType.HPRegen, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateHPRegen(creature);

            // Assert
            // HPRegen (5) + Vitality bonus (10 * 4 = 40) = 45
            Assert.That(result, Is.EqualTo(45));
        }

        [Test]
        public void CalculateHPRegen_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.HPRegen, 5);
            stats.SetStat(StatType.Vitality, 10);
            effects.SetStat(StatType.HPRegen, 3);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateHPRegen(creature);

            // Assert
            // HPRegen (5 + 3 = 8) + Vitality bonus (10 * 4 = 40) = 48
            Assert.That(result, Is.EqualTo(48));
        }

        #endregion

        #region CalculateFPRegen Tests

        [Test]
        public void CalculateFPRegen_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.FPRegen, 3);
            stats.SetStat(StatType.Vitality, 8);
            effects.SetStat(StatType.FPRegen, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFPRegen(creature);

            // Assert
            // Base 1 + FPRegen (3) + Vitality bonus (8 / 2 = 4) = 8
            Assert.That(result, Is.EqualTo(8));
        }

        [Test]
        public void CalculateFPRegen_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.FPRegen, 3);
            stats.SetStat(StatType.Vitality, 8);
            effects.SetStat(StatType.FPRegen, 2);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFPRegen(creature);

            // Assert
            // Base 1 + FPRegen (3) + Status effect (2) + Vitality bonus (8 / 2 = 4) = 10
            Assert.That(result, Is.EqualTo(10));
        }

        #endregion

        #region CalculateSTMRegen Tests

        [Test]
        public void CalculateSTMRegen_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.STMRegen, 2);
            stats.SetStat(StatType.Vitality, 12);
            effects.SetStat(StatType.STMRegen, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSTMRegen(creature);

            // Assert
            // Base 1 + STMRegen (2) + Vitality bonus (12 / 2 = 6) = 9
            Assert.That(result, Is.EqualTo(9));
        }

        [Test]
        public void CalculateSTMRegen_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.STMRegen, 2);
            stats.SetStat(StatType.Vitality, 12);
            effects.SetStat(StatType.STMRegen, 1);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSTMRegen(creature);

            // Assert
            // Base 1 + STMRegen (2) + Status effect (1) + Vitality bonus (12 / 2 = 6) = 10
            Assert.That(result, Is.EqualTo(10));
        }

        #endregion

        #region CalculateRecastReduction Tests

        [Test]
        public void CalculateRecastReduction_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.RecastReduction, 10);
            effects.SetStat(StatType.RecastReduction, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateRecastReduction(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.1f).Within(0.0001f)); // 10% = 0.1
        }

        [Test]
        public void CalculateRecastReduction_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.RecastReduction, 10);
            effects.SetStat(StatType.RecastReduction, 5);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateRecastReduction(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.15f).Within(0.0001f)); // 15% = 0.15
        }

        #endregion

        #region Combat Stats Tests

        [Test]
        public void CalculateDefense_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Defense, 15);
            stats.SetStat(StatType.Vitality, 10); // Vitality ability
            effects.SetStat(StatType.Defense, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Armor).Returns(5); // Armor skill
            
            // Act
            var result = _statService.CalculateDefense(creature);

            // Assert
            // Base (8) + ability (10 * 1.5 = 15) + skill (5) + bonus (15) = 43
            Assert.That(result, Is.EqualTo(43));
        }

        [Test]
        public void CalculateDefense_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Defense, 15);
            stats.SetStat(StatType.Vitality, 10); // Vitality ability
            effects.SetStat(StatType.Defense, 5);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Armor).Returns(5); // Armor skill
            
            // Act
            var result = _statService.CalculateDefense(creature);

            // Assert
            // Base (8) + ability (10 * 1.5 = 15) + skill (5) + bonus (15 + 5 = 20) = 48
            Assert.That(result, Is.EqualTo(48));
        }

        [Test]
        public void CalculateEvasion_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Evasion, 12);
            stats.SetStat(StatType.Agility, 8); // Agility ability
            effects.SetStat(StatType.Evasion, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Armor).Returns(3); // Armor skill
            
            // Act
            var result = _statService.CalculateEvasion(creature);

            // Assert
            // Ability (8 * 3 = 24) + skill (3) + bonus (12) = 39
            Assert.That(result, Is.EqualTo(39));
        }

        [Test]
        public void CalculateAccuracy_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Accuracy, 18);
            stats.SetStat(StatType.Might, 12); // Might ability
            effects.SetStat(StatType.Accuracy, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.OneHanded).Returns(4); // OneHanded skill
            
            // Act
            var result = _statService.CalculateAccuracy(creature, AbilityType.Might, SkillType.OneHanded);

            // Assert
            // Ability (12 * 3 = 36) + skill (4) + bonus (18) = 58
            Assert.That(result, Is.EqualTo(58));
        }

        [Test]
        public void CalculateAttack_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Attack, 25);
            stats.SetStat(StatType.Might, 14); // Might ability
            effects.SetStat(StatType.Attack, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.OneHanded).Returns(6); // OneHanded skill
            
            // Act
            var result = _statService.CalculateAttack(creature, AbilityType.Might, SkillType.OneHanded);

            // Assert
            // Base (8) + skill (2 * 6 = 12) + ability (14) + bonus (25) = 59
            Assert.That(result, Is.EqualTo(59));
        }

        [Test]
        public void CalculateForceAttack_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.ForceAttack, 20);
            stats.SetStat(StatType.Willpower, 16); // Willpower ability
            effects.SetStat(StatType.ForceAttack, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Force).Returns(8); // Force skill
            
            // Act
            var result = _statService.CalculateForceAttack(creature);

            // Assert
            // Base (8) + skill (2 * 8 = 16) + ability (16) + bonus (20) = 60
            Assert.That(result, Is.EqualTo(60));
        }

        [Test]
        public void CalculateForceDefense_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.ForceDefense, 12);
            stats.SetStat(StatType.Willpower, 10); // Willpower ability
            effects.SetStat(StatType.ForceDefense, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Armor).Returns(4); // Armor skill
            
            // Act
            var result = _statService.CalculateForceDefense(creature);

            // Assert
            // Base (8) + ability (10 * 1.5 = 15) + skill (4) + bonus (12) = 39
            Assert.That(result, Is.EqualTo(39));
        }

        [Test]
        public void CalculateForceDefense_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.ForceDefense, 12);
            stats.SetStat(StatType.Willpower, 10); // Willpower ability
            effects.SetStat(StatType.ForceDefense, 8);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Armor).Returns(4); // Armor skill
            
            // Act
            var result = _statService.CalculateForceDefense(creature);

            // Assert
            // Base (8) + ability (10 * 1.5 = 15) + skill (4) + bonus (12 + 8 = 20) = 47
            Assert.That(result, Is.EqualTo(47));
        }

        #endregion

        #region Attribute Tests

        [Test]
        public void CalculateMight_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Might, 14);
            effects.SetStat(StatType.Might, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMight(creature);

            // Assert
            Assert.That(result, Is.EqualTo(14));
        }

        [Test]
        public void CalculatePerception_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Perception, 16);
            effects.SetStat(StatType.Perception, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculatePerception(creature);

            // Assert
            Assert.That(result, Is.EqualTo(16));
        }

        [Test]
        public void CalculateVitality_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Vitality, 18);
            effects.SetStat(StatType.Vitality, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateVitality(creature);

            // Assert
            Assert.That(result, Is.EqualTo(18));
        }

        [Test]
        public void CalculateAgility_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Agility, 15);
            effects.SetStat(StatType.Agility, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAgility(creature);

            // Assert
            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void CalculateWillpower_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Willpower, 17);
            effects.SetStat(StatType.Willpower, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateWillpower(creature);

            // Assert
            Assert.That(result, Is.EqualTo(17));
        }

        [Test]
        public void CalculateSocial_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Social, 13);
            effects.SetStat(StatType.Social, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSocial(creature);

            // Assert
            Assert.That(result, Is.EqualTo(13));
        }

        #endregion

        #region Special Combat Stats Tests

        [Test]
        public void CalculateShieldDeflection_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.ShieldDeflection, 8);
            effects.SetStat(StatType.ShieldDeflection, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateShieldDeflection(creature);

            // Assert
            Assert.That(result, Is.EqualTo(8));
        }

        [Test]
        public void CalculateAttackDeflection_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.AttackDeflection, 6);
            effects.SetStat(StatType.AttackDeflection, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAttackDeflection(creature);

            // Assert
            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void CalculateCriticalRate_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.CriticalRate, 5);
            effects.SetStat(StatType.CriticalRate, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateCriticalRate(creature);

            // Assert
            // Base (5) + stats (5) + effects (0) = 10
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void CalculateEnmity_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Enmity, 10);
            effects.SetStat(StatType.Enmity, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateEnmity(creature);

            // Assert
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void CalculateHaste_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Haste, 30);
            effects.SetStat(StatType.Haste, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateHaste(creature);

            // Assert
            Assert.That(result, Is.EqualTo(30));
        }

        [Test]
        public void CalculateSlow_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Slow, 20);
            effects.SetStat(StatType.Slow, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSlow(creature);

            // Assert
            Assert.That(result, Is.EqualTo(20));
        }

        [Test]
        public void CalculateDamageReduction_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.DamageReduction, 15);
            effects.SetStat(StatType.DamageReduction, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateDamageReduction(creature);

            // Assert
            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void CalculateQueuedDMGBonus_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.QueuedDMGBonus, 8);
            effects.SetStat(StatType.QueuedDMGBonus, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateQueuedDMGBonus(creature);

            // Assert
            Assert.That(result, Is.EqualTo(8));
        }

        [Test]
        public void CalculateParalysis_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Paralysis, 5);
            effects.SetStat(StatType.Paralysis, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateParalysis(creature);

            // Assert
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void CalculatePoisonResist_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.PoisonResist, 25);
            effects.SetStat(StatType.PoisonResist, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculatePoisonResist(creature);

            // Assert
            Assert.That(result, Is.EqualTo(25));
        }

        [Test]
        public void CalculateLevel_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Level, 10);
            effects.SetStat(StatType.Level, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateLevel(creature);

            // Assert
            Assert.That(result, Is.EqualTo(10));
        }

        #endregion

        #region Modifier Tests

        [Test]
        public void CalculateAccuracyModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.AccuracyModifier, 5);
            effects.SetStat(StatType.AccuracyModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAccuracyModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void CalculateRecastReductionModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.RecastReductionModifier, 3);
            effects.SetStat(StatType.RecastReductionModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateRecastReductionModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void CalculateDefenseBypassModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.DefenseBypassModifier, 7);
            effects.SetStat(StatType.DefenseBypassModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateDefenseBypassModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(7));
        }

        [Test]
        public void CalculateHealingModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.HealingModifier, 12);
            effects.SetStat(StatType.HealingModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateHealingModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(12));
        }

        [Test]
        public void CalculateFPRestoreOnHit_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.FPRestoreOnHit, 2);
            effects.SetStat(StatType.FPRestoreOnHit, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFPRestoreOnHit(creature);

            // Assert
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void CalculateDefenseModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.DefenseModifier, 8);
            effects.SetStat(StatType.DefenseModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateDefenseModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(8));
        }

        [Test]
        public void CalculateForceDefenseModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.ForceDefenseModifier, 6);
            effects.SetStat(StatType.ForceDefenseModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateForceDefenseModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void CalculateExtraAttackModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.ExtraAttackModifier, 4);
            effects.SetStat(StatType.ExtraAttackModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateExtraAttackModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public void CalculateAttackModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.AttackModifier, 9);
            effects.SetStat(StatType.AttackModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAttackModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(9));
        }

        [Test]
        public void CalculateForceAttackModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.ForceAttackModifier, 11);
            effects.SetStat(StatType.ForceAttackModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateForceAttackModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(11));
        }

        [Test]
        public void CalculateEvasionModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.EvasionModifier, 6);
            effects.SetStat(StatType.EvasionModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateEvasionModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void CalculateXPModifier_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.XPModifier, 15);
            effects.SetStat(StatType.XPModifier, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateXPModifier(creature);

            // Assert
            Assert.That(result, Is.EqualTo(15));
        }

        #endregion

        #region Edge Cases and Boundary Conditions

        [Test]
        public void CalculateMaxHP_WithNegativeStats_HandlesCorrectly()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxHP, -10);
            effects.SetStat(StatType.MaxHP, 5);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxHP(creature);

            // Assert
            // Base (70) + stats (-10) + effects (5) = 65
            Assert.That(result, Is.EqualTo(65));
        }

        [Test]
        public void CalculateMaxHP_WithZeroValues_ReturnsZero()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxHP, 0);
            effects.SetStat(StatType.MaxHP, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateMaxHP(creature);

            // Assert
            // Base (70) + stats (0) + effects (0) = 70
            Assert.That(result, Is.EqualTo(70));
        }

        [Test]
        public void CalculateRecastReduction_AtExactCapBoundary_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.RecastReduction, 50); // Exactly 50%
            effects.SetStat(StatType.RecastReduction, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateRecastReduction(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void CalculateRecastReduction_JustAboveCap_IsCappedAt50Percent()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.RecastReduction, 51); // Just above 50%
            effects.SetStat(StatType.RecastReduction, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateRecastReduction(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void CalculateRecastReduction_WithNegativeValues_HandlesCorrectly()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.RecastReduction, -10); // Negative recast reduction
            effects.SetStat(StatType.RecastReduction, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateRecastReduction(creature);

            // Assert
            Assert.That(result, Is.EqualTo(-0.1f).Within(0.0001f)); // -10% = -0.1
        }

        [Test]
        public void CalculateHPRegen_WithZeroVitality_ReturnsBaseRegen()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.HPRegen, 5);
            stats.SetStat(StatType.Vitality, 0); // Zero vitality
            effects.SetStat(StatType.HPRegen, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateHPRegen(creature);

            // Assert
            Assert.That(result, Is.EqualTo(5)); // Base regen only
        }

        [Test]
        public void CalculateFPRegen_WithOddVitality_HandlesDivisionCorrectly()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.FPRegen, 2);
            stats.SetStat(StatType.Vitality, 7); // Odd vitality (7 / 2 = 3)
            effects.SetStat(StatType.FPRegen, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFPRegen(creature);

            // Assert
            // Base 1 + FPRegen (2) + Vitality bonus (7 / 2 = 3) = 6
            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void CalculateSTMRegen_WithZeroVitality_ReturnsBaseRegenPlusOne()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.STMRegen, 3);
            stats.SetStat(StatType.Vitality, 0); // Zero vitality
            effects.SetStat(StatType.STMRegen, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSTMRegen(creature);

            // Assert
            // Base 1 + STMRegen (3) + Vitality bonus (0 / 2 = 0) = 4
            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public void CalculateDefense_WithConflictingStatusEffects_AppliesNetEffect()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Defense, 20);
            stats.SetStat(StatType.Vitality, 10); // Vitality ability
            effects.SetStat(StatType.Defense, -5); // Negative status effect
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Armor).Returns(5); // Armor skill

            // Act
            var result = _statService.CalculateDefense(creature);

            // Assert
            // Base (8) + ability (10 * 1.5 = 15) + skill (5) + bonus (20 + (-5) = 15) = 43
            Assert.That(result, Is.EqualTo(43));
        }

        [Test]
        public void CalculateDefense_WithStatusEffectExceedingBase_ReturnsNegative()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.Defense, 10);
            stats.SetStat(StatType.Vitality, 10); // Vitality ability
            effects.SetStat(StatType.Defense, -15); // Status effect exceeds base
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);
            _mockSkillService.GetSkillRank(creature, SkillType.Armor).Returns(5); // Armor skill

            // Act
            var result = _statService.CalculateDefense(creature);

            // Assert
            // Base (8) + ability (10 * 1.5 = 15) + skill (5) + bonus (10 + (-15) = -5) = 23
            Assert.That(result, Is.EqualTo(23));
        }

        #endregion

        #region Consistency and Performance Tests

        [Test]
        public void CalculateMaxHP_MultipleCalls_ReturnsConsistentResults()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.MaxHP, 100);
            effects.SetStat(StatType.MaxHP, 25);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result1 = _statService.CalculateMaxHP(creature);
            var result2 = _statService.CalculateMaxHP(creature);
            var result3 = _statService.CalculateMaxHP(creature);

            // Assert
            // Base (70) + stats (100) + effects (25) = 195
            Assert.That(result1, Is.EqualTo(195));
            Assert.That(result2, Is.EqualTo(195));
            Assert.That(result3, Is.EqualTo(195));
            Assert.That(result1, Is.EqualTo(result2));
            Assert.That(result2, Is.EqualTo(result3));
        }

        [Test]
        public void CalculateRecastReduction_WithVeryHighValues_IsProperlyCapped()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.RecastReduction, 1000); // Very high value
            effects.SetStat(StatType.RecastReduction, 500);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateRecastReduction(creature);

            // Assert
            Assert.That(result, Is.EqualTo(0.5f).Within(0.0001f)); // Should be capped at 50%
        }

        [Test]
        public void CalculateHPRegen_WithHighVitality_CalculatesCorrectly()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();
            
            stats.SetStat(StatType.HPRegen, 10);
            stats.SetStat(StatType.Vitality, 50); // High vitality
            effects.SetStat(StatType.HPRegen, 0);
            
            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateHPRegen(creature);

            // Assert
            // Base HPRegen (10) + Vitality bonus (50 * 4 = 200) = 210
            Assert.That(result, Is.EqualTo(210));
        }

        #endregion

        #region Control Stat Tests

        [Test]
        public void CalculateSmitheryControl_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlSmithery, 15);
            effects.SetStat(StatType.ControlSmithery, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSmitheryControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void CalculateSmitheryControl_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlSmithery, 15);
            effects.SetStat(StatType.ControlSmithery, 5);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSmitheryControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(20));
        }

        [Test]
        public void CalculateFabricationControl_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlFabrication, 12);
            effects.SetStat(StatType.ControlFabrication, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFabricationControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(12));
        }

        [Test]
        public void CalculateFabricationControl_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlFabrication, 12);
            effects.SetStat(StatType.ControlFabrication, 3);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFabricationControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void CalculateEngineeringControl_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlEngineering, 18);
            effects.SetStat(StatType.ControlEngineering, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateEngineeringControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(18));
        }

        [Test]
        public void CalculateEngineeringControl_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlEngineering, 18);
            effects.SetStat(StatType.ControlEngineering, 2);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateEngineeringControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(20));
        }

        [Test]
        public void CalculateAgricultureControl_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlAgriculture, 14);
            effects.SetStat(StatType.ControlAgriculture, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAgricultureControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(14));
        }

        [Test]
        public void CalculateAgricultureControl_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.ControlAgriculture, 14);
            effects.SetStat(StatType.ControlAgriculture, 4);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAgricultureControl(creature);

            // Assert
            Assert.That(result, Is.EqualTo(18));
        }

        #endregion

        #region Craftsmanship Stat Tests

        [Test]
        public void CalculateSmitheryCraftsmanship_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipSmithery, 16);
            effects.SetStat(StatType.CraftsmanshipSmithery, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSmitheryCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(16));
        }

        [Test]
        public void CalculateSmitheryCraftsmanship_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipSmithery, 16);
            effects.SetStat(StatType.CraftsmanshipSmithery, 1);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateSmitheryCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(17));
        }

        [Test]
        public void CalculateFabricationCraftsmanship_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipFabrication, 13);
            effects.SetStat(StatType.CraftsmanshipFabrication, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFabricationCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(13));
        }

        [Test]
        public void CalculateFabricationCraftsmanship_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipFabrication, 13);
            effects.SetStat(StatType.CraftsmanshipFabrication, 2);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateFabricationCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void CalculateEngineeringCraftsmanship_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipEngineering, 19);
            effects.SetStat(StatType.CraftsmanshipEngineering, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateEngineeringCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(19));
        }

        [Test]
        public void CalculateEngineeringCraftsmanship_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipEngineering, 19);
            effects.SetStat(StatType.CraftsmanshipEngineering, 1);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateEngineeringCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(20));
        }

        [Test]
        public void CalculateAgricultureCraftsmanship_WithBaseStats_ReturnsCorrectValue()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipAgriculture, 17);
            effects.SetStat(StatType.CraftsmanshipAgriculture, 0);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAgricultureCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(17));
        }

        [Test]
        public void CalculateAgricultureCraftsmanship_WithStatusEffects_IncludesStatusEffects()
        {
            // Arrange
            var creature = 1u;
            var stats = new StatGroup();
            var effects = new StatGroup();

            stats.SetStat(StatType.CraftsmanshipAgriculture, 17);
            effects.SetStat(StatType.CraftsmanshipAgriculture, 3);

            _mockStatGroupService.LoadStats(creature).Returns(stats);
            _mockStatusEffectService.GetCreatureStatGroup(creature).Returns(effects);

            // Act
            var result = _statService.CalculateAgricultureCraftsmanship(creature);

            // Assert
            Assert.That(result, Is.EqualTo(20));
        }

        #endregion
    }
}
