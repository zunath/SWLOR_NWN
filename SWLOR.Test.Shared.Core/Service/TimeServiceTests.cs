using SWLOR.Shared.Core.Service;
using SWLOR.Test.Shared.Core.TestHelpers;

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
            var firstDate = new DateTime(2024, 1, 15, 14, 30, 45);
            var secondDate = new DateTime(2024, 1, 13, 2, 25, 0);

            // Act
            var result = _timeService.GetTimeToWaitLongIntervals(firstDate, secondDate, false);

            // Assert
            Assert.That(result, Is.EqualTo("2 days, 12 hours, 5 minutes, 45 seconds"));
        }

        [Test]
        public void GetTimeToWaitLongIntervals_WithSecondDateAfterFirstDate_ShouldReturnCorrectFormat()
        {
            // Arrange
            var firstDate = new DateTime(2024, 1, 13, 2, 25, 0);
            var secondDate = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var result = _timeService.GetTimeToWaitLongIntervals(firstDate, secondDate, false);

            // Assert
            Assert.That(result, Is.EqualTo("2 days, 12 hours, 5 minutes, 45 seconds"));
        }

        [Test]
        public void GetTimeToWaitLongIntervals_WithSameDates_ShouldReturnZeroTime()
        {
            // Arrange
            var firstDate = new DateTime(2024, 1, 15, 14, 30, 45);
            var secondDate = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var result = _timeService.GetTimeToWaitLongIntervals(firstDate, secondDate, false);

            // Assert
            Assert.That(result, Is.EqualTo("0 seconds"));
        }

        [Test]
        public void GetTimeToWaitLongIntervals_WithShowIfZeroTrue_ShouldShowZeroValues()
        {
            // Arrange
            var firstDate = new DateTime(2024, 1, 15, 14, 30, 45);
            var secondDate = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var result = _timeService.GetTimeToWaitLongIntervals(firstDate, secondDate, true);

            // Assert
            Assert.That(result, Is.EqualTo("0 days, 0 hours, 0 minutes, 0 seconds"));
        }

        [Test]
        public void GetTimeToWaitShortIntervals_WithFirstDateAfterSecondDate_ShouldReturnCorrectFormat()
        {
            // Arrange
            var firstDate = new DateTime(2024, 1, 15, 14, 30, 45);
            var secondDate = new DateTime(2024, 1, 13, 2, 25, 0);

            // Act
            var result = _timeService.GetTimeToWaitShortIntervals(firstDate, secondDate, false);

            // Assert
            Assert.That(result, Is.EqualTo("2D, 12H, 5M, 45S"));
        }

        [Test]
        public void GetTimeToWaitShortIntervals_WithSecondDateAfterFirstDate_ShouldReturnCorrectFormat()
        {
            // Arrange
            var firstDate = new DateTime(2024, 1, 13, 2, 25, 0);
            var secondDate = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var result = _timeService.GetTimeToWaitShortIntervals(firstDate, secondDate, false);

            // Assert
            Assert.That(result, Is.EqualTo("2D, 12H, 5M, 45S"));
        }

        [Test]
        public void GetTimeToWaitShortIntervals_WithSameDates_ShouldReturnZeroTime()
        {
            // Arrange
            var firstDate = new DateTime(2024, 1, 15, 14, 30, 45);
            var secondDate = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var result = _timeService.GetTimeToWaitShortIntervals(firstDate, secondDate, false);

            // Assert
            Assert.That(result, Is.EqualTo("0S"));
        }

        [Test]
        public void GetTimeToWaitShortIntervals_WithShowIfZeroTrue_ShouldShowZeroValues()
        {
            // Arrange
            var firstDate = new DateTime(2024, 1, 15, 14, 30, 45);
            var secondDate = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var result = _timeService.GetTimeToWaitShortIntervals(firstDate, secondDate, true);

            // Assert
            Assert.That(result, Is.EqualTo("0D, 0H, 0M, 0S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithValidValues_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 2;
            const int hours = 12;
            const int minutes = 5;
            const int seconds = 45;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("2 days, 12 hours, 5 minutes, 45 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithZeroValuesAndShowIfZeroFalse_ShouldOnlyShowSeconds()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("30 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithZeroValuesAndShowIfZeroTrue_ShouldShowAllValues()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, true);

            // Assert
            Assert.That(result, Is.EqualTo("0 days, 0 hours, 0 minutes, 30 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithSingleDay_ShouldUseSingularForm()
        {
            // Arrange
            const int days = 1;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("1 day, 0 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithSingleHour_ShouldUseSingularForm()
        {
            // Arrange
            const int days = 0;
            const int hours = 1;
            const int minutes = 0;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("1 hour, 0 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithSingleMinute_ShouldUseSingularForm()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 1;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("1 minute, 0 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithSingleSecond_ShouldUseSingularForm()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 1;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("1 second"));
        }

        [Test]
        public void GetTimeLongIntervals_WithTimeSpan_ShouldReturnCorrectFormat()
        {
            // Arrange
            var timeSpan = new TimeSpan(2, 12, 5, 45);

            // Act
            var result = _timeService.GetTimeLongIntervals(timeSpan, false);

            // Assert
            Assert.That(result, Is.EqualTo("2 days, 12 hours, 5 minutes, 45 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithValidValues_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 2;
            const int hours = 12;
            const int minutes = 5;
            const int seconds = 45;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("2D, 12H, 5M, 45S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithZeroValuesAndShowIfZeroFalse_ShouldOnlyShowSeconds()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("30S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithZeroValuesAndShowIfZeroTrue_ShouldShowAllValues()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 30;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, true);

            // Assert
            Assert.That(result, Is.EqualTo("0D, 0H, 0M, 30S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithTimeSpan_ShouldReturnCorrectFormat()
        {
            // Arrange
            var timeSpan = new TimeSpan(2, 12, 5, 45);

            // Act
            var result = _timeService.GetTimeShortIntervals(timeSpan, false);

            // Assert
            Assert.That(result, Is.EqualTo("2D, 12H, 5M, 45S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithSingleValues_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 1;
            const int hours = 1;
            const int minutes = 1;
            const int seconds = 1;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("1D, 1H, 1M, 1S"));
        }

        [Test]
        public void GetTimeShortIntervals_WithLargeValues_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 365;
            const int hours = 23;
            const int minutes = 59;
            const int seconds = 59;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("365D, 23H, 59M, 59S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithLargeValues_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 365;
            const int hours = 23;
            const int minutes = 59;
            const int seconds = 59;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("365 days, 23 hours, 59 minutes, 59 seconds"));
        }

        [Test]
        public void GetTimeLongIntervals_WithNegativeValues_ShouldHandleCorrectly()
        {
            // Arrange
            const int days = -1;
            const int hours = -1;
            const int minutes = -1;
            const int seconds = -1;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("-1 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithNegativeValues_ShouldHandleCorrectly()
        {
            // Arrange
            const int days = -1;
            const int hours = -1;
            const int minutes = -1;
            const int seconds = -1;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("-1S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithOnlyDays_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 7;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("7 days, 0 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithOnlyDays_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 7;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("7D, 0S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithOnlyHours_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 0;
            const int hours = 12;
            const int minutes = 0;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("12 hours, 0 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithOnlyHours_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 0;
            const int hours = 12;
            const int minutes = 0;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("12H, 0S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithOnlyMinutes_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 30;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("30 minutes, 0 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithOnlyMinutes_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 30;
            const int seconds = 0;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("30M, 0S"));
        }

        [Test]
        public void GetTimeLongIntervals_WithOnlySeconds_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 45;

            // Act
            var result = _timeService.GetTimeLongIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("45 seconds"));
        }

        [Test]
        public void GetTimeShortIntervals_WithOnlySeconds_ShouldReturnCorrectFormat()
        {
            // Arrange
            const int days = 0;
            const int hours = 0;
            const int minutes = 0;
            const int seconds = 45;

            // Act
            var result = _timeService.GetTimeShortIntervals(days, hours, minutes, seconds, false);

            // Assert
            Assert.That(result, Is.EqualTo("45S"));
        }
    }
}
