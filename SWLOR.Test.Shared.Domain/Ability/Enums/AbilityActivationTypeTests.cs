using SWLOR.Shared.Domain.Ability.Enums;

namespace SWLOR.Test.Shared.Domain.Ability.Enums
{
    [TestFixture]
    public class AbilityActivationTypeTests
    {
        [Test]
        public void AbilityActivationType_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.That((int)AbilityActivationType.Invalid, Is.EqualTo(0));
            Assert.That((int)AbilityActivationType.Casted, Is.EqualTo(1));
            Assert.That((int)AbilityActivationType.Weapon, Is.EqualTo(2));
            Assert.That((int)AbilityActivationType.Stance, Is.EqualTo(3));
            Assert.That((int)AbilityActivationType.Concentration, Is.EqualTo(4));
        }

        [Test]
        public void AbilityActivationType_ShouldBeComparable()
        {
            // Act & Assert
            Assert.That(AbilityActivationType.Casted, Is.GreaterThan(AbilityActivationType.Invalid));
            Assert.That(AbilityActivationType.Weapon, Is.GreaterThan(AbilityActivationType.Casted));
            Assert.That(AbilityActivationType.Stance, Is.GreaterThan(AbilityActivationType.Weapon));
            Assert.That(AbilityActivationType.Concentration, Is.GreaterThan(AbilityActivationType.Stance));
        }

        [Test]
        public void AbilityActivationType_ShouldBeEquatable()
        {
            // Act & Assert
            Assert.That(AbilityActivationType.Invalid, Is.EqualTo(AbilityActivationType.Invalid));
            Assert.That(AbilityActivationType.Casted, Is.EqualTo(AbilityActivationType.Casted));
            Assert.That(AbilityActivationType.Weapon, Is.EqualTo(AbilityActivationType.Weapon));
            Assert.That(AbilityActivationType.Stance, Is.EqualTo(AbilityActivationType.Stance));
            Assert.That(AbilityActivationType.Concentration, Is.EqualTo(AbilityActivationType.Concentration));
        }

        [Test]
        public void AbilityActivationType_ShouldBeInequatable()
        {
            // Act & Assert
            Assert.That(AbilityActivationType.Invalid, Is.Not.EqualTo(AbilityActivationType.Casted));
            Assert.That(AbilityActivationType.Casted, Is.Not.EqualTo(AbilityActivationType.Weapon));
            Assert.That(AbilityActivationType.Weapon, Is.Not.EqualTo(AbilityActivationType.Stance));
            Assert.That(AbilityActivationType.Stance, Is.Not.EqualTo(AbilityActivationType.Concentration));
        }

        [Test]
        public void AbilityActivationType_ShouldHaveToString()
        {
            // Act & Assert
            Assert.That(AbilityActivationType.Invalid.ToString(), Is.EqualTo("Invalid"));
            Assert.That(AbilityActivationType.Casted.ToString(), Is.EqualTo("Casted"));
            Assert.That(AbilityActivationType.Weapon.ToString(), Is.EqualTo("Weapon"));
            Assert.That(AbilityActivationType.Stance.ToString(), Is.EqualTo("Stance"));
            Assert.That(AbilityActivationType.Concentration.ToString(), Is.EqualTo("Concentration"));
        }

        [Test]
        public void AbilityActivationType_ShouldHaveGetHashCode()
        {
            // Act & Assert
            Assert.That(AbilityActivationType.Invalid.GetHashCode(), Is.EqualTo(0));
            Assert.That(AbilityActivationType.Casted.GetHashCode(), Is.EqualTo(1));
            Assert.That(AbilityActivationType.Weapon.GetHashCode(), Is.EqualTo(2));
            Assert.That(AbilityActivationType.Stance.GetHashCode(), Is.EqualTo(3));
            Assert.That(AbilityActivationType.Concentration.GetHashCode(), Is.EqualTo(4));
        }

        [Test]
        public void AbilityActivationType_ShouldBeSerializable()
        {
            // Arrange
            var activationType = AbilityActivationType.Casted;

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(activationType);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<AbilityActivationType>(json);

            // Assert
            Assert.That(deserialized, Is.EqualTo(activationType));
        }

        [Test]
        public void AbilityActivationType_ShouldBeParsable()
        {
            // Act & Assert
            Assert.That(Enum.Parse<AbilityActivationType>("Invalid"), Is.EqualTo(AbilityActivationType.Invalid));
            Assert.That(Enum.Parse<AbilityActivationType>("Casted"), Is.EqualTo(AbilityActivationType.Casted));
            Assert.That(Enum.Parse<AbilityActivationType>("Weapon"), Is.EqualTo(AbilityActivationType.Weapon));
            Assert.That(Enum.Parse<AbilityActivationType>("Stance"), Is.EqualTo(AbilityActivationType.Stance));
            Assert.That(Enum.Parse<AbilityActivationType>("Concentration"), Is.EqualTo(AbilityActivationType.Concentration));
        }

        [Test]
        public void AbilityActivationType_ShouldBeEnumerable()
        {
            // Act
            var values = Enum.GetValues<AbilityActivationType>();

            // Assert
            Assert.That(values, Is.Not.Null);
            Assert.That(values.Length, Is.EqualTo(5));
            Assert.That(values, Contains.Item(AbilityActivationType.Invalid));
            Assert.That(values, Contains.Item(AbilityActivationType.Casted));
            Assert.That(values, Contains.Item(AbilityActivationType.Weapon));
            Assert.That(values, Contains.Item(AbilityActivationType.Stance));
            Assert.That(values, Contains.Item(AbilityActivationType.Concentration));
        }

        [Test]
        public void AbilityActivationType_ShouldBeConvertibleToInt()
        {
            // Act & Assert
            Assert.That((int)AbilityActivationType.Invalid, Is.EqualTo(0));
            Assert.That((int)AbilityActivationType.Casted, Is.EqualTo(1));
            Assert.That((int)AbilityActivationType.Weapon, Is.EqualTo(2));
            Assert.That((int)AbilityActivationType.Stance, Is.EqualTo(3));
            Assert.That((int)AbilityActivationType.Concentration, Is.EqualTo(4));
        }

        [Test]
        public void AbilityActivationType_ShouldBeConvertibleFromInt()
        {
            // Act & Assert
            Assert.That((AbilityActivationType)0, Is.EqualTo(AbilityActivationType.Invalid));
            Assert.That((AbilityActivationType)1, Is.EqualTo(AbilityActivationType.Casted));
            Assert.That((AbilityActivationType)2, Is.EqualTo(AbilityActivationType.Weapon));
            Assert.That((AbilityActivationType)3, Is.EqualTo(AbilityActivationType.Stance));
            Assert.That((AbilityActivationType)4, Is.EqualTo(AbilityActivationType.Concentration));
        }
    }
}
