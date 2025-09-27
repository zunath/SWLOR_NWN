using SWLOR.Shared.Core.Service;
using SWLOR.Test.Shared.Core.TestHelpers;

namespace SWLOR.Test.Shared.Core.Service
{
    [TestFixture]
    public class RandomServiceTests
    {
        private RandomService _randomService;

        [SetUp]
        public void SetUp()
        {
            _randomService = new RandomService();
        }

        [Test]
        public void Next_ShouldReturnNonNegativeInteger()
        {
            // Act
            var result = _randomService.Next();

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Next_WithMaxValue_ShouldReturnValueInRange()
        {
            // Arrange
            const int maxValue = 10;

            // Act
            var result = _randomService.Next(maxValue);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
            Assert.That(result, Is.LessThan(maxValue));
        }

        [Test]
        public void Next_WithMaxValueZero_ShouldReturnZero()
        {
            // Act
            var result = _randomService.Next(0);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Next_WithMinAndMax_ShouldReturnValueInRange()
        {
            // Arrange
            const int min = 5;
            const int max = 15;

            // Act
            var result = _randomService.Next(min, max);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(min));
            Assert.That(result, Is.LessThan(max));
        }

        [Test]
        public void Next_WithSameMinAndMax_ShouldReturnMin()
        {
            // Arrange
            const int min = 10;
            const int max = 10;

            // Act
            var result = _randomService.Next(min, max);

            // Assert
            Assert.That(result, Is.EqualTo(min));
        }

        [Test]
        public void NextFloat_ShouldReturnValueInRange()
        {
            // Act
            var result = _randomService.NextFloat();

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0.0f));
            Assert.That(result, Is.LessThan(1.0f));
        }

        [Test]
        public void NextFloat_WithMinAndMax_ShouldReturnValueInRange()
        {
            // Arrange
            const float min = 5.5f;
            const float max = 10.5f;

            // Act
            var result = _randomService.NextFloat(min, max);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(min));
            Assert.That(result, Is.LessThanOrEqualTo(max));
        }

        [Test]
        public void NextFloat_WithSameMinAndMax_ShouldReturnMin()
        {
            // Arrange
            const float min = 7.5f;
            const float max = 7.5f;

            // Act
            var result = _randomService.NextFloat(min, max);

            // Assert
            Assert.That(result, Is.EqualTo(min));
        }

        [Test]
        public void GetRandomWeightedIndex_WithNullWeights_ShouldReturnNegativeOne()
        {
            // Act
            var result = _randomService.GetRandomWeightedIndex(null);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void GetRandomWeightedIndex_WithEmptyWeights_ShouldReturnNegativeOne()
        {
            // Arrange
            var weights = new int[0];

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void GetRandomWeightedIndex_WithAllZeroWeights_ShouldReturnNegativeOne()
        {
            // Arrange
            var weights = new int[] { 0, 0, 0 };

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void GetRandomWeightedIndex_WithValidWeights_ShouldReturnValidIndex()
        {
            // Arrange
            var weights = new int[] { 10, 20, 30, 40 };

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
            Assert.That(result, Is.LessThan(weights.Length));
        }

        [Test]
        public void GetRandomWeightedIndex_WithSinglePositiveWeight_ShouldReturnThatIndex()
        {
            // Arrange
            var weights = new int[] { 0, 0, 50, 0 };

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void GetRandomWeightedIndex_WithMixedWeights_ShouldReturnValidIndex()
        {
            // Arrange
            var weights = new int[] { 0, 25, 0, 75 };

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
            Assert.That(result, Is.LessThan(weights.Length));
        }

        [Test]
        public void D2_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 3;
            const int minimum = 1;

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 2));
        }

        [Test]
        public void D2_WithZeroDice_ShouldUseOneDie()
        {
            // Arrange
            const int numberOfDice = 0;
            const int minimum = 1;

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(result, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void D2_WithInvalidMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 0;

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(2));
            Assert.That(result, Is.LessThanOrEqualTo(4));
        }

        [Test]
        public void D3_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D3(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 3));
        }

        [Test]
        public void D4_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D4(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 4));
        }

        [Test]
        public void D6_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D6(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 6));
        }

        [Test]
        public void D8_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D8(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 8));
        }

        [Test]
        public void D10_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D10(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 10));
        }

        [Test]
        public void D12_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D12(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 12));
        }

        [Test]
        public void D20_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D20(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 20));
        }

        [Test]
        public void D100_WithValidParameters_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D100(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 100));
        }

        [Test]
        public void D2_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D2(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 2));
        }

        [Test]
        public void D3_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D3(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 3));
        }

        [Test]
        public void D4_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D4(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 4));
        }

        [Test]
        public void D6_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D6(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 6));
        }

        [Test]
        public void D8_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D8(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 8));
        }

        [Test]
        public void D10_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D10(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 10));
        }

        [Test]
        public void D12_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D12(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 12));
        }

        [Test]
        public void D20_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D20(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 20));
        }

        [Test]
        public void D100_WithDefaultMinimum_ShouldUseOne()
        {
            // Arrange
            const int numberOfDice = 2;

            // Act
            var result = _randomService.D100(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 100));
        }

        [Test]
        public void D2_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 5; // Greater than max (2)

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 2)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 2));
        }

        [Test]
        public void D3_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 5; // Greater than max (3)

            // Act
            var result = _randomService.D3(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 3)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 3));
        }

        [Test]
        public void D4_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 5; // Greater than max (4)

            // Act
            var result = _randomService.D4(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 4)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 4));
        }

        [Test]
        public void D6_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 8; // Greater than max (6)

            // Act
            var result = _randomService.D6(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 6)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 6));
        }

        [Test]
        public void D8_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 10; // Greater than max (8)

            // Act
            var result = _randomService.D8(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 8)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 8));
        }

        [Test]
        public void D10_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 12; // Greater than max (10)

            // Act
            var result = _randomService.D10(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 10)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 10));
        }

        [Test]
        public void D12_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 15; // Greater than max (12)

            // Act
            var result = _randomService.D12(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 12)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 12));
        }

        [Test]
        public void D20_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 25; // Greater than max (20)

            // Act
            var result = _randomService.D20(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 20)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 20));
        }

        [Test]
        public void D100_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 2;
            const int minimum = 150; // Greater than max (100)

            // Act
            var result = _randomService.D100(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * 100)); // Should use max as minimum
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 100));
        }
    }
}
