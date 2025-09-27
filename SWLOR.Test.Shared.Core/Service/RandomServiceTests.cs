using SWLOR.Shared.Core.Service;

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
            // Arrange
            const int maxValue = 0;

            // Act
            var result = _randomService.Next(maxValue);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Next_WithMinAndMaxValues_ShouldReturnValueInRange()
        {
            // Arrange
            const int minValue = 5;
            const int maxValue = 15;

            // Act
            var result = _randomService.Next(minValue, maxValue);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(minValue));
            Assert.That(result, Is.LessThan(maxValue));
        }

        [Test]
        public void Next_WithSameMinAndMaxValues_ShouldReturnMinValue()
        {
            // Arrange
            const int minValue = 10;
            const int maxValue = 10;

            // Act
            var result = _randomService.Next(minValue, maxValue);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void NextFloat_ShouldReturnFloatInRange()
        {
            // Act
            var result = _randomService.NextFloat();

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0.0f));
            Assert.That(result, Is.LessThan(1.0f));
        }

        [Test]
        public void NextFloat_WithMinAndMaxValues_ShouldReturnValueInRange()
        {
            // Arrange
            const float minValue = 1.5f;
            const float maxValue = 5.5f;

            // Act
            var result = _randomService.NextFloat(minValue, maxValue);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(minValue));
            Assert.That(result, Is.LessThan(maxValue));
        }

        [Test]
        public void NextFloat_WithSameMinAndMaxValues_ShouldReturnMinValue()
        {
            // Arrange
            const float minValue = 3.14f;
            const float maxValue = 3.14f;

            // Act
            var result = _randomService.NextFloat(minValue, maxValue);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void GetRandomWeightedIndex_WithValidWeights_ShouldReturnValidIndex()
        {
            // Arrange
            var weights = new int[] { 1, 2, 3, 4 };

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
            Assert.That(result, Is.LessThan(weights.Length));
        }

        [Test]
        public void GetRandomWeightedIndex_WithNullWeights_ShouldReturnNegativeOne()
        {
            // Arrange
            int[] weights = null;

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

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
        public void GetRandomWeightedIndex_WithZeroWeights_ShouldReturnNegativeOne()
        {
            // Arrange
            var weights = new int[] { 0, 0, 0 };

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void GetRandomWeightedIndex_WithSingleWeight_ShouldReturnZero()
        {
            // Arrange
            var weights = new int[] { 5 };

            // Act
            var result = _randomService.GetRandomWeightedIndex(weights);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void GetRandomWeightedIndex_WithMixedWeights_ShouldReturnValidIndex()
        {
            // Arrange
            var weights = new int[] { 0, 5, 0, 3, 0 };

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
            const int numberOfDice = 2;
            const int minimum = 1;

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(numberOfDice * minimum));
            Assert.That(result, Is.LessThanOrEqualTo(numberOfDice * 2));
        }

        [Test]
        public void D2_WithDefaultMinimum_ShouldReturnValidValue()
        {
            // Arrange
            const int numberOfDice = 1;

            // Act
            var result = _randomService.D2(numberOfDice);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(1));
            Assert.That(result, Is.LessThanOrEqualTo(2));
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
        public void D2_WithInvalidNumberOfDice_ShouldUseMinimumOne()
        {
            // Arrange
            const int numberOfDice = 0;
            const int minimum = 1;

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(1));
            Assert.That(result, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void D2_WithInvalidMinimum_ShouldUseMinimumOne()
        {
            // Arrange
            const int numberOfDice = 1;
            const int minimum = 0;

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(1));
            Assert.That(result, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void D2_WithMinimumGreaterThanMax_ShouldAdjustMinimum()
        {
            // Arrange
            const int numberOfDice = 1;
            const int minimum = 5; // Greater than max of 2

            // Act
            var result = _randomService.D2(numberOfDice, minimum);

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(2));
            Assert.That(result, Is.LessThanOrEqualTo(2));
        }
    }
}