using SWLOR.Shared.Core.Bioware;

namespace SWLOR.Test.Shared.Core.Bioware
{
    [TestFixture]
    public class BiowarePositionTests
    {
        [Test]
        public void GetChangeInX_WithValidInputs_ShouldReturnCorrectValue()
        {
            // Arrange
            const float distance = 10.0f;
            const float angle = MathF.PI / 4; // 45 degrees

            // Act
            var result = BiowarePosition.GetChangeInX(distance, angle);

            // Assert
            var expected = distance * MathF.Cos(angle);
            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void GetChangeInY_WithValidInputs_ShouldReturnCorrectValue()
        {
            // Arrange
            const float distance = 10.0f;
            const float angle = MathF.PI / 4; // 45 degrees

            // Act
            var result = BiowarePosition.GetChangeInY(distance, angle);

            // Assert
            var expected = distance * MathF.Sin(angle);
            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void GetChangeInX_WithZeroAngle_ShouldReturnDistance()
        {
            // Arrange
            const float distance = 5.0f;
            const float angle = 0.0f;

            // Act
            var result = BiowarePosition.GetChangeInX(distance, angle);

            // Assert
            Assert.That(result, Is.EqualTo(distance).Within(0.0001f));
        }

        [Test]
        public void GetChangeInY_WithZeroAngle_ShouldReturnZero()
        {
            // Arrange
            const float distance = 5.0f;
            const float angle = 0.0f;

            // Act
            var result = BiowarePosition.GetChangeInY(distance, angle);

            // Assert
            Assert.That(result, Is.EqualTo(0.0f).Within(0.0001f));
        }

        [Test]
        public void GetChangeInX_WithNinetyDegreeAngle_ShouldReturnZero()
        {
            // Arrange
            const float distance = 5.0f;
            const float angle = MathF.PI / 2; // 90 degrees

            // Act
            var result = BiowarePosition.GetChangeInX(distance, angle);

            // Assert
            Assert.That(result, Is.EqualTo(0.0f).Within(0.0001f));
        }

        [Test]
        public void GetChangeInY_WithNinetyDegreeAngle_ShouldReturnDistance()
        {
            // Arrange
            const float distance = 5.0f;
            const float angle = MathF.PI / 2; // 90 degrees

            // Act
            var result = BiowarePosition.GetChangeInY(distance, angle);

            // Assert
            Assert.That(result, Is.EqualTo(distance).Within(0.0001f));
        }

        // Note: Tests for GetChangedPosition and GetRelativeFacing methods have been removed
        // because they depend on NWN.Core initialization which is not appropriate for unit tests.
        // These methods should be tested in integration tests instead.
    }
}
