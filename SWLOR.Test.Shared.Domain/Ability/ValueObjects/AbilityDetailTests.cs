using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Perk.Enums;


namespace SWLOR.Test.Shared.Domain.Ability.ValueObjects
{
    [TestFixture]
    public class AbilityDetailTests
    {
        [Test]
        public void AbilityDetail_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var abilityDetail = new AbilityDetail();

            // Assert
            Assert.That(abilityDetail.Name, Is.Null);
            Assert.That(abilityDetail.ActivationAction, Is.Null);
            Assert.That(abilityDetail.ImpactAction, Is.Null);
            Assert.That(abilityDetail.ActivationDelay, Is.Null);
            Assert.That(abilityDetail.RecastDelay, Is.Null);
            Assert.That(abilityDetail.CustomValidation, Is.Null);
            Assert.That(abilityDetail.Requirements, Is.Not.Null);
            Assert.That(abilityDetail.ActivationVisualEffect, Is.EqualTo(VisualEffectType.None));
            Assert.That(abilityDetail.RecastGroup, Is.EqualTo(RecastGroupType.Invalid));
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Invalid));
            Assert.That(abilityDetail.EffectiveLevelPerkType, Is.EqualTo(PerkType.Invalid));
            Assert.That(abilityDetail.AnimationType, Is.EqualTo(AnimationType.Invalid));
            Assert.That(abilityDetail.CanBeUsedInSpace, Is.False);
            Assert.That(abilityDetail.IgnoreHeavyArmorPenalty, Is.False);
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(5.0f));
            Assert.That(abilityDetail.IsHostileAbility, Is.False);
            Assert.That(abilityDetail.DisplaysActivationMessage, Is.True);
            Assert.That(abilityDetail.BreaksStealth, Is.False);
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(1));
        }

        [Test]
        public void AbilityDetail_WithName_ShouldStoreNameCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.Name = "Test Ability";

            // Assert
            Assert.That(abilityDetail.Name, Is.EqualTo("Test Ability"));
        }

        [Test]
        public void AbilityDetail_WithActivationAction_ShouldStoreActivationActionCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            AbilityActivationAction activationAction = (activator, target, effectivePerkLevel, targetLocation) => true;

            // Act
            abilityDetail.ActivationAction = activationAction;

            // Assert
            Assert.That(abilityDetail.ActivationAction, Is.EqualTo(activationAction));
        }

        [Test]
        public void AbilityDetail_WithImpactAction_ShouldStoreImpactActionCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            AbilityImpactAction impactAction = (activator, target, effectivePerkLevel, targetLocation) => { };

            // Act
            abilityDetail.ImpactAction = impactAction;

            // Assert
            Assert.That(abilityDetail.ImpactAction, Is.EqualTo(impactAction));
        }

        [Test]
        public void AbilityDetail_WithActivationDelay_ShouldStoreActivationDelayCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            AbilityActivationDelayAction activationDelay = (activator, target, effectivePerkLevel) => 1.0f;

            // Act
            abilityDetail.ActivationDelay = activationDelay;

            // Assert
            Assert.That(abilityDetail.ActivationDelay, Is.EqualTo(activationDelay));
        }

        [Test]
        public void AbilityDetail_WithRecastDelay_ShouldStoreRecastDelayCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            AbilityRecastDelayAction recastDelay = (activator) => 5.0f;

            // Act
            abilityDetail.RecastDelay = recastDelay;

            // Assert
            Assert.That(abilityDetail.RecastDelay, Is.EqualTo(recastDelay));
        }

        [Test]
        public void AbilityDetail_WithCustomValidation_ShouldStoreCustomValidationCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            AbilityCustomValidationAction customValidation = (activator, target, effectivePerkLevel, targetLocation) => "Valid";

            // Act
            abilityDetail.CustomValidation = customValidation;

            // Assert
            Assert.That(abilityDetail.CustomValidation, Is.EqualTo(customValidation));
        }

        [Test]
        public void AbilityDetail_WithActivationVisualEffect_ShouldStoreActivationVisualEffectCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.ActivationVisualEffect = VisualEffectType.Fnf_Fireball;

            // Assert
            Assert.That(abilityDetail.ActivationVisualEffect, Is.EqualTo(VisualEffectType.Fnf_Fireball));
        }

        [Test]
        public void AbilityDetail_WithRecastGroup_ShouldStoreRecastGroupCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.RecastGroup = RecastGroupType.ForceHeal;

            // Assert
            Assert.That(abilityDetail.RecastGroup, Is.EqualTo(RecastGroupType.ForceHeal));
        }

        [Test]
        public void AbilityDetail_WithActivationType_ShouldStoreActivationTypeCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.ActivationType = AbilityActivationType.Casted;

            // Assert
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Casted));
        }

        [Test]
        public void AbilityDetail_WithAnimationType_ShouldStoreAnimationTypeCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.AnimationType = AnimationType.LoopingConjure1;

            // Assert
            Assert.That(abilityDetail.AnimationType, Is.EqualTo(AnimationType.LoopingConjure1));
        }


        [Test]
        public void AbilityDetail_WithCanBeUsedInSpace_ShouldStoreCanBeUsedInSpaceCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.CanBeUsedInSpace = true;

            // Assert
            Assert.That(abilityDetail.CanBeUsedInSpace, Is.True);
        }

        [Test]
        public void AbilityDetail_WithIgnoreHeavyArmorPenalty_ShouldStoreIgnoreHeavyArmorPenaltyCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.IgnoreHeavyArmorPenalty = true;

            // Assert
            Assert.That(abilityDetail.IgnoreHeavyArmorPenalty, Is.True);
        }

        [Test]
        public void AbilityDetail_WithMaxRange_ShouldStoreMaxRangeCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.MaxRange = 10.5f;

            // Assert
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(10.5f));
        }

        [Test]
        public void AbilityDetail_WithIsHostileAbility_ShouldStoreIsHostileAbilityCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.IsHostileAbility = true;

            // Assert
            Assert.That(abilityDetail.IsHostileAbility, Is.True);
        }

        [Test]
        public void AbilityDetail_WithDisplaysActivationMessage_ShouldStoreDisplaysActivationMessageCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.DisplaysActivationMessage = false;

            // Assert
            Assert.That(abilityDetail.DisplaysActivationMessage, Is.False);
        }

        [Test]
        public void AbilityDetail_WithBreaksStealth_ShouldStoreBreaksStealthCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.BreaksStealth = true;

            // Assert
            Assert.That(abilityDetail.BreaksStealth, Is.True);
        }

        [Test]
        public void AbilityDetail_WithAbilityLevel_ShouldStoreAbilityLevelCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.AbilityLevel = 5;

            // Assert
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(5));
        }

        [Test]
        public void AbilityDetail_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            AbilityActivationAction activationAction = (activator, target, effectivePerkLevel, targetLocation) => true;
            AbilityImpactAction impactAction = (activator, target, effectivePerkLevel, targetLocation) => { };
            AbilityActivationDelayAction activationDelay = (activator, target, effectivePerkLevel) => 1.0f;
            AbilityRecastDelayAction recastDelay = (activator) => 5.0f;
            AbilityCustomValidationAction customValidation = (activator, target, effectivePerkLevel, targetLocation) => "Valid";

            // Act
            abilityDetail.Name = "Test Ability";
            abilityDetail.ActivationAction = activationAction;
            abilityDetail.ImpactAction = impactAction;
            abilityDetail.ActivationDelay = activationDelay;
            abilityDetail.RecastDelay = recastDelay;
            abilityDetail.CustomValidation = customValidation;
            abilityDetail.ActivationVisualEffect = VisualEffectType.Fnf_Fireball;
            abilityDetail.RecastGroup = RecastGroupType.ForceHeal;
            abilityDetail.ActivationType = AbilityActivationType.Casted;
            abilityDetail.AnimationType = AnimationType.LoopingConjure1;
            abilityDetail.CanBeUsedInSpace = true;
            abilityDetail.IgnoreHeavyArmorPenalty = true;
            abilityDetail.MaxRange = 10.5f;
            abilityDetail.IsHostileAbility = true;
            abilityDetail.DisplaysActivationMessage = false;
            abilityDetail.BreaksStealth = true;
            abilityDetail.AbilityLevel = 5;

            // Assert
            Assert.That(abilityDetail.Name, Is.EqualTo("Test Ability"));
            Assert.That(abilityDetail.ActivationAction, Is.EqualTo(activationAction));
            Assert.That(abilityDetail.ImpactAction, Is.EqualTo(impactAction));
            Assert.That(abilityDetail.ActivationDelay, Is.EqualTo(activationDelay));
            Assert.That(abilityDetail.RecastDelay, Is.EqualTo(recastDelay));
            Assert.That(abilityDetail.CustomValidation, Is.EqualTo(customValidation));
            Assert.That(abilityDetail.ActivationVisualEffect, Is.EqualTo(VisualEffectType.Fnf_Fireball));
            Assert.That(abilityDetail.RecastGroup, Is.EqualTo(RecastGroupType.ForceHeal));
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Casted));
            Assert.That(abilityDetail.AnimationType, Is.EqualTo(AnimationType.LoopingConjure1));
            Assert.That(abilityDetail.CanBeUsedInSpace, Is.True);
            Assert.That(abilityDetail.IgnoreHeavyArmorPenalty, Is.True);
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(10.5f));
            Assert.That(abilityDetail.IsHostileAbility, Is.True);
            Assert.That(abilityDetail.DisplaysActivationMessage, Is.False);
            Assert.That(abilityDetail.BreaksStealth, Is.True);
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(5));
        }

        [Test]
        public void AbilityDetail_WithZeroValues_ShouldStoreZeroValues()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.MaxRange = 0.0f;
            abilityDetail.AbilityLevel = 0;

            // Assert
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(0.0f));
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(0));
        }

        [Test]
        public void AbilityDetail_WithNegativeValues_ShouldStoreNegativeValues()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.MaxRange = -5.0f;
            abilityDetail.AbilityLevel = -1;

            // Assert
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(-5.0f));
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(-1));
        }

        [Test]
        public void AbilityDetail_WithLargeValues_ShouldStoreLargeValues()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.MaxRange = 1000.0f;
            abilityDetail.AbilityLevel = 100;

            // Assert
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(1000.0f));
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(100));
        }

        [Test]
        public void AbilityDetail_WithMaxValues_ShouldStoreMaxValues()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.MaxRange = float.MaxValue;
            abilityDetail.AbilityLevel = int.MaxValue;

            // Assert
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(float.MaxValue));
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void AbilityDetail_WithMinValues_ShouldStoreMinValues()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.MaxRange = float.MinValue;
            abilityDetail.AbilityLevel = int.MinValue;

            // Assert
            Assert.That(abilityDetail.MaxRange, Is.EqualTo(float.MinValue));
            Assert.That(abilityDetail.AbilityLevel, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void AbilityDetail_WithEmptyString_ShouldStoreEmptyString()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.Name = "";

            // Assert
            Assert.That(abilityDetail.Name, Is.EqualTo(""));
        }

        [Test]
        public void AbilityDetail_WithNullString_ShouldStoreNullString()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            abilityDetail.Name = null;

            // Assert
            Assert.That(abilityDetail.Name, Is.Null);
        }

        [Test]
        public void AbilityDetail_WithSpecialCharacters_ShouldStoreSpecialCharacters()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            const string specialString = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            abilityDetail.Name = specialString;

            // Assert
            Assert.That(abilityDetail.Name, Is.EqualTo(specialString));
        }

        [Test]
        public void AbilityDetail_WithLongString_ShouldStoreLongString()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            var longString = new string('a', 1000);

            // Act
            abilityDetail.Name = longString;

            // Assert
            Assert.That(abilityDetail.Name, Is.EqualTo(longString));
        }

        [Test]
        public void AbilityDetail_WithAllActivationTypes_ShouldStoreAllActivationTypes()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act & Assert
            abilityDetail.ActivationType = AbilityActivationType.Invalid;
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Invalid));

            abilityDetail.ActivationType = AbilityActivationType.Casted;
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Casted));

            abilityDetail.ActivationType = AbilityActivationType.Weapon;
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Weapon));

            abilityDetail.ActivationType = AbilityActivationType.Stance;
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Stance));

            abilityDetail.ActivationType = AbilityActivationType.Concentration;
            Assert.That(abilityDetail.ActivationType, Is.EqualTo(AbilityActivationType.Concentration));
        }

        [Test]
        public void AbilityDetail_WithAllRecastGroups_ShouldStoreAllRecastGroups()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act & Assert
            abilityDetail.RecastGroup = RecastGroupType.Invalid;
            Assert.That(abilityDetail.RecastGroup, Is.EqualTo(RecastGroupType.Invalid));

            abilityDetail.RecastGroup = RecastGroupType.ForceHeal;
            Assert.That(abilityDetail.RecastGroup, Is.EqualTo(RecastGroupType.ForceHeal));

            abilityDetail.RecastGroup = RecastGroupType.ForcePush;
            Assert.That(abilityDetail.RecastGroup, Is.EqualTo(RecastGroupType.ForcePush));
        }

        [Test]
        public void AbilityDetail_WithAllVisualEffects_ShouldStoreAllVisualEffects()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act & Assert
            abilityDetail.ActivationVisualEffect = VisualEffectType.None;
            Assert.That(abilityDetail.ActivationVisualEffect, Is.EqualTo(VisualEffectType.None));

            abilityDetail.ActivationVisualEffect = VisualEffectType.Fnf_Fireball;
            Assert.That(abilityDetail.ActivationVisualEffect, Is.EqualTo(VisualEffectType.Fnf_Fireball));

            abilityDetail.ActivationVisualEffect = VisualEffectType.Dur_Entangle;
            Assert.That(abilityDetail.ActivationVisualEffect, Is.EqualTo(VisualEffectType.Dur_Entangle));
        }

        [Test]
        public void AbilityDetail_WithAllAnimationTypes_ShouldStoreAllAnimationTypes()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act & Assert
            abilityDetail.AnimationType = AnimationType.Invalid;
            Assert.That(abilityDetail.AnimationType, Is.EqualTo(AnimationType.Invalid));

            abilityDetail.AnimationType = AnimationType.LoopingConjure1;
            Assert.That(abilityDetail.AnimationType, Is.EqualTo(AnimationType.LoopingConjure1));

            abilityDetail.AnimationType = AnimationType.FireForgetHeadTurnLeft;
            Assert.That(abilityDetail.AnimationType, Is.EqualTo(AnimationType.FireForgetHeadTurnLeft));
        }

        [Test]
        public void AbilityDetail_WithAllPerkTypes_ShouldStoreAllPerkTypes()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act & Assert
            abilityDetail.EffectiveLevelPerkType = PerkType.Invalid;
            Assert.That(abilityDetail.EffectiveLevelPerkType, Is.EqualTo(PerkType.Invalid));

            abilityDetail.EffectiveLevelPerkType = PerkType.ForceLeap;
            Assert.That(abilityDetail.EffectiveLevelPerkType, Is.EqualTo(PerkType.ForceLeap));
        }

        [Test]
        public void AbilityDetail_WithSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            abilityDetail.Name = "Test Ability";
            abilityDetail.ActivationVisualEffect = VisualEffectType.Fnf_Fireball;
            abilityDetail.RecastGroup = RecastGroupType.ForceHeal;
            abilityDetail.ActivationType = AbilityActivationType.Casted;
            abilityDetail.AnimationType = AnimationType.LoopingConjure1;
            abilityDetail.CanBeUsedInSpace = true;
            abilityDetail.IgnoreHeavyArmorPenalty = true;
            abilityDetail.MaxRange = 10.5f;
            abilityDetail.IsHostileAbility = true;
            abilityDetail.DisplaysActivationMessage = false;
            abilityDetail.BreaksStealth = true;
            abilityDetail.AbilityLevel = 5;

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(abilityDetail);
            var deserializedDetail = System.Text.Json.JsonSerializer.Deserialize<AbilityDetail>(json);

            // Assert
            Assert.That(deserializedDetail, Is.Not.Null);
            Assert.That(deserializedDetail.Name, Is.EqualTo(abilityDetail.Name));
            Assert.That(deserializedDetail.ActivationVisualEffect, Is.EqualTo(abilityDetail.ActivationVisualEffect));
            Assert.That(deserializedDetail.RecastGroup, Is.EqualTo(abilityDetail.RecastGroup));
            Assert.That(deserializedDetail.ActivationType, Is.EqualTo(abilityDetail.ActivationType));
            Assert.That(deserializedDetail.EffectiveLevelPerkType, Is.EqualTo(abilityDetail.EffectiveLevelPerkType));
            Assert.That(deserializedDetail.AnimationType, Is.EqualTo(abilityDetail.AnimationType));
            Assert.That(deserializedDetail.CanBeUsedInSpace, Is.EqualTo(abilityDetail.CanBeUsedInSpace));
            Assert.That(deserializedDetail.IgnoreHeavyArmorPenalty, Is.EqualTo(abilityDetail.IgnoreHeavyArmorPenalty));
            Assert.That(deserializedDetail.MaxRange, Is.EqualTo(abilityDetail.MaxRange));
            Assert.That(deserializedDetail.IsHostileAbility, Is.EqualTo(abilityDetail.IsHostileAbility));
            Assert.That(deserializedDetail.DisplaysActivationMessage, Is.EqualTo(abilityDetail.DisplaysActivationMessage));
            Assert.That(deserializedDetail.BreaksStealth, Is.EqualTo(abilityDetail.BreaksStealth));
            Assert.That(deserializedDetail.AbilityLevel, Is.EqualTo(abilityDetail.AbilityLevel));
        }

        [Test]
        public void AbilityDetail_WithEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var abilityDetail1 = new AbilityDetail();
            var abilityDetail2 = new AbilityDetail();
            abilityDetail1.AbilityLevel = 5;
            abilityDetail2.AbilityLevel = 5;

            // Act & Assert
            Assert.That(abilityDetail1.AbilityLevel, Is.EqualTo(abilityDetail2.AbilityLevel));
        }

        [Test]
        public void AbilityDetail_WithInequality_ShouldCompareInequalityCorrectly()
        {
            // Arrange
            var abilityDetail1 = new AbilityDetail();
            var abilityDetail2 = new AbilityDetail();
            abilityDetail1.AbilityLevel = 5;
            abilityDetail2.AbilityLevel = 10;

            // Act & Assert
            Assert.That(abilityDetail1.AbilityLevel, Is.Not.EqualTo(abilityDetail2.AbilityLevel));
        }

        [Test]
        public void AbilityDetail_WithToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            abilityDetail.Name = "Test Ability";

            // Act
            var result = abilityDetail.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void AbilityDetail_WithGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();

            // Act
            var type = abilityDetail.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(AbilityDetail)));
        }

        [Test]
        public void AbilityDetail_WithHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            abilityDetail.Name = "Test Ability";

            // Act
            var hashCode = abilityDetail.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }

        [Test]
        public void AbilityDetail_WithDelegateInvocation_ShouldInvokeDelegatesCorrectly()
        {
            // Arrange
            var abilityDetail = new AbilityDetail();
            var activationCalled = false;
            var impactCalled = false;
            var activationDelayCalled = false;
            var recastDelayCalled = false;
            var customValidationCalled = false;

            abilityDetail.ActivationAction = (activator, target, effectivePerkLevel, targetLocation) =>
            {
                activationCalled = true;
                return true;
            };

            abilityDetail.ImpactAction = (activator, target, effectivePerkLevel, targetLocation) =>
            {
                impactCalled = true;
            };

            abilityDetail.ActivationDelay = (activator, target, effectivePerkLevel) =>
            {
                activationDelayCalled = true;
                return 1.0f;
            };

            abilityDetail.RecastDelay = (activator) =>
            {
                recastDelayCalled = true;
                return 5.0f;
            };

            abilityDetail.CustomValidation = (activator, target, effectivePerkLevel, targetLocation) =>
            {
                customValidationCalled = true;
                return "Valid";
            };

            // Act
            var activationResult = abilityDetail.ActivationAction(1, 2, 3, null);
            abilityDetail.ImpactAction(1, 2, 3, null);
            var activationDelayResult = abilityDetail.ActivationDelay(1, 2, 3);
            var recastDelayResult = abilityDetail.RecastDelay(1);
            var customValidationResult = abilityDetail.CustomValidation(1, 2, 3, null);

            // Assert
            Assert.That(activationCalled, Is.True);
            Assert.That(impactCalled, Is.True);
            Assert.That(activationDelayCalled, Is.True);
            Assert.That(recastDelayCalled, Is.True);
            Assert.That(customValidationCalled, Is.True);
            Assert.That(activationResult, Is.True);
            Assert.That(activationDelayResult, Is.EqualTo(1.0f));
            Assert.That(recastDelayResult, Is.EqualTo(5.0f));
            Assert.That(customValidationResult, Is.EqualTo("Valid"));
        }
    }
}
