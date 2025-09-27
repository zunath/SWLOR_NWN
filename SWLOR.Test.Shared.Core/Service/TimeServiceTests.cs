using SWLOR.Shared.Core.Service;

namespace SWLOR.Test.Shared.Core.Service
{
    [TestFixture]
    public class TimeServiceTests
    {
        private TimeService _timeService;

        [SetUp]
        public void SetUp()
        {
            _timeService = new TimeService();
        }

        [Test]
        public void GetTimeToWaitLongIntervals_WithFirstDateAfterSecondDate_ShouldReturnCorrectFormat()
        {
            // Arrange
            var firstDate = new DateTime(2023, 12, 25, 15, 30, 45);
            var secondDate = new DateTime(2023, 12, 23, 10, 15, 30);
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeToWaitLongIntervals(firstDate, secondDate, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2 days"));
            Assert.That(result, Does.Contain("5 hours"));
            Assert.That(result, Does.Contain("15 minutes"));
            Assert.That(result, Does.Contain("15 seconds"));
        }

        [Test]
        public void GetTimeToWaitLongIntervals_WithSecondDateAfterFirstDate_ShouldReturnCorrectFormat()
        {
            // Arrange
            var firstDate = new DateTime(2023, 12, 23, 10, 15, 30);
            var secondDate = new DateTime(2023, 12, 25, 15, 30, 45);
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeToWaitLongIntervals(firstDate, secondDate, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2 days"));
            Assert.That(result, Does.Contain("5 hours"));
            Assert.That(result, Does.Contain("15 minutes"));
            Assert.That(result, Does.Contain("15 seconds"));
        }

        [Test]
        public void GetTimeToWaitLongIntervals_WithSameDates_ShouldReturnZeroTime()
        {
            // Arrange
            var date = new DateTime(2023, 12, 25, 15, 30, 45);
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeToWaitLongIntervals(date, date, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("0 seconds"));
        }

        [Test]
        public void GetTimeToWaitShortIntervals_WithFirstDateAfterSecondDate_ShouldReturnCorrectFormat()
        {
            // Arrange
            var firstDate = new DateTime(2023, 12, 25, 15, 30, 45);
            var secondDate = new DateTime(2023, 12, 23, 10, 15, 30);
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeToWaitShortIntervals(firstDate, secondDate, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2D"));
            Assert.That(result, Does.Contain("5H"));
            Assert.That(result, Does.Contain("15M"));
            Assert.That(result, Does.Contain("15S"));
        }

        [Test]
        public void GetTimeToWaitShortIntervals_WithSecondDateAfterFirstDate_ShouldReturnCorrectFormat()
        {
            // Arrange
            var firstDate = new DateTime(2023, 12, 23, 10, 15, 30);
            var secondDate = new DateTime(2023, 12, 25, 15, 30, 45);
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeToWaitShortIntervals(firstDate, secondDate, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2D"));
            Assert.That(result, Does.Contain("5H"));
            Assert.That(result, Does.Contain("15M"));
            Assert.That(result, Does.Contain("15S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithIndividualValues_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 2;
            const int hours = 5;
            const int minutes = 15;
            const int seconds = 30;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2 days"));
            Assert.That(result, Does.Contain("5 hours"));
            Assert.That(result, Does.Contain("15 minutes"));
            Assert.That(result, Does.Contain("30 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithZeroValuesAndShowIfZeroFalse_ShouldNotShowZeroValues()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Not.Contain("0 days"));
            Assert.That(result, Does.Not.Contain("0 hours"));
            Assert.That(result, Does.Not.Contain("0 minutes"));
            Assert.That(result, Does.Contain("30 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithZeroValuesAndShowIfZeroTrue_ShouldShowZeroValues()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;
            const bool showIfZero = true;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("0 days"));
            Assert.That(result, Does.Contain("0 hours"));
            Assert.That(result, Does.Contain("0 minutes"));
            Assert.That(result, Does.Contain("30 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithSingularValues_ShouldUseSingularForm()
        {
            // Arrange
            const int days = 1;
            const int hours = 1;
            const int minutes = 1;
            const int seconds = 1;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("1 day"));
            Assert.That(result, Does.Contain("1 hour"));
            Assert.That(result, Does.Contain("1 minute"));
            Assert.That(result, Does.Contain("1 second"));
        }

        [Test]
        public void GetTimeLongIntervals_WithTimeSpan_ShouldReturnCorrectFormat()
        {
            // Arrange
            var timeSpan = new TimeSpan(2, 5, 15, 30);
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeLongIntervals(timeSpan, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2 days"));
            Assert.That(result, Does.Contain("5 hours"));
            Assert.That(result, Does.Contain("15 minutes"));
            Assert.That(result, Does.Contain("30 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithIndividualValues_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 2;
            const int hours = 5;
            const int minutes = 15;
            const int seconds = 30;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2D"));
            Assert.That(result, Does.Contain("5H"));
            Assert.That(result, Does.Contain("15M"));
            Assert.That(result, Does.Contain("30S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithZeroValuesAndShowIfZeroFalse_ShouldNotShowZeroValues()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Not.Contain("0D"));
            Assert.That(result, Does.Not.Contain("0H"));
            Assert.That(result, Does.Not.Contain("0M"));
            Assert.That(result, Does.Contain("30S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithZeroValuesAndShowIfZeroTrue_ShouldShowZeroValues()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;
            const bool showIfZero = true;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("0D"));
            Assert.That(result, Does.Contain("0H"));
            Assert.That(result, Does.Contain("0M"));
            Assert.That(result, Does.Contain("30S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithTimeSpan_ShouldReturnCorrectFormat()
        {
            // Arrange
            var timeSpan = new TimeSpan(2, 5, 15, 30);
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeShortIntervals(timeSpan, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("2D"));
            Assert.That(result, Does.Contain("5H"));
            Assert.That(result, Does.Contain("15M"));
            Assert.That(result, Does.Contain("30S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithLargeValues_ShouldHandleLargeValues()
        {
            // Arrange
            const int days = 365;
            const int hours = 23;
            const int minutes = 59;
            const int seconds = 59;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("365 days"));
            Assert.That(result, Does.Contain("23 hours"));
            Assert.That(result, Does.Contain("59 minutes"));
            Assert.That(result, Does.Contain("59 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithLargeValues_ShouldHandleLargeValues()
        {
            // Arrange
            const int days = 365;
            const int hours = 23;
            const int minutes = 59;
            const int seconds = 59;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("365D"));
            Assert.That(result, Does.Contain("23H"));
            Assert.That(result, Does.Contain("59M"));
            Assert.That(result, Does.Contain("59S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithNegativeValues_ShouldHandleNegativeValues()
        {
            // Arrange
            const int days = -1;
            const int hours = -1;
            const int minutes = -1;
            const int seconds = -1;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("-1 day"));
            Assert.That(result, Does.Contain("-1 hour"));
            Assert.That(result, Does.Contain("-1 minute"));
            Assert.That(result, Does.Contain("-1 second"));
        }

        [Test]
        public void GetTimeShortIntervals_WithNegativeValues_ShouldHandleNegativeValues()
        {
            // Arrange
            const int days = -1;
            const int hours = -1;
            const int minutes = -1;
            const int seconds = -1;
            const bool showIfZero = false;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, showIfZero);

            // Assert
            Assert.That(result, Does.Contain("-1D"));
            Assert.That(result, Does.Contain("-1H"));
            Assert.That(result, Does.Contain("-1M"));
            Assert.That(result, Does.Contain("-1S"));
        }
    }
}