using NUnit.Framework;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Test.Shared.Abstractions.Enums
{
    [TestFixture]
    public class ServerEnvironmentTypeTests
    {
        [Test]
        public void EnumValues_ShouldBeCorrect()
        {
            // Assert
            Assert.That((int)ServerEnvironmentType.Invalid, Is.EqualTo(0));
            Assert.That((int)ServerEnvironmentType.Development, Is.EqualTo(1));
            Assert.That((int)ServerEnvironmentType.Production, Is.EqualTo(2));
            Assert.That((int)ServerEnvironmentType.Test, Is.EqualTo(4));
            Assert.That((int)ServerEnvironmentType.All, Is.EqualTo(7)); // 1 | 2 | 4 = 7
        }

        [Test]
        public void EnumValues_ShouldBeUnique()
        {
            // Arrange
            var values = Enum.GetValues<ServerEnvironmentType>().Cast<int>().ToArray();

            // Assert
            Assert.That(values, Is.Unique);
        }

        [Test]
        public void EnumToString_ShouldReturnCorrectName()
        {
            // Assert
            Assert.That(ServerEnvironmentType.Invalid.ToString(), Is.EqualTo("Invalid"));
            Assert.That(ServerEnvironmentType.Development.ToString(), Is.EqualTo("Development"));
            Assert.That(ServerEnvironmentType.Production.ToString(), Is.EqualTo("Production"));
            Assert.That(ServerEnvironmentType.Test.ToString(), Is.EqualTo("Test"));
            Assert.That(ServerEnvironmentType.All.ToString(), Is.EqualTo("All"));
        }

        [Test]
        public void EnumParse_ShouldWorkCorrectly()
        {
            // Assert
            Assert.That(Enum.Parse<ServerEnvironmentType>("Invalid"), Is.EqualTo(ServerEnvironmentType.Invalid));
            Assert.That(Enum.Parse<ServerEnvironmentType>("Development"), Is.EqualTo(ServerEnvironmentType.Development));
            Assert.That(Enum.Parse<ServerEnvironmentType>("Production"), Is.EqualTo(ServerEnvironmentType.Production));
            Assert.That(Enum.Parse<ServerEnvironmentType>("Test"), Is.EqualTo(ServerEnvironmentType.Test));
            Assert.That(Enum.Parse<ServerEnvironmentType>("All"), Is.EqualTo(ServerEnvironmentType.All));
        }

        [Test]
        public void FlagsAttribute_ShouldAllowBitwiseOperations()
        {
            // Arrange
            var development = ServerEnvironmentType.Development;
            var production = ServerEnvironmentType.Production;
            var test = ServerEnvironmentType.Test;

            // Act
            var combined = development | production | test;

            // Assert
            Assert.That(combined, Is.EqualTo(ServerEnvironmentType.All));
        }

        [Test]
        public void FlagsAttribute_ShouldAllowBitwiseAndOperations()
        {
            // Arrange
            var all = ServerEnvironmentType.All;
            var development = ServerEnvironmentType.Development;

            // Act
            var result = all & development;

            // Assert
            Assert.That(result, Is.EqualTo(ServerEnvironmentType.Development));
        }

        [Test]
        public void FlagsAttribute_ShouldAllowHasFlagOperations()
        {
            // Arrange
            var all = ServerEnvironmentType.All;

            // Assert
            Assert.That(all.HasFlag(ServerEnvironmentType.Development), Is.True);
            Assert.That(all.HasFlag(ServerEnvironmentType.Production), Is.True);
            Assert.That(all.HasFlag(ServerEnvironmentType.Test), Is.True);
            Assert.That(all.HasFlag(ServerEnvironmentType.Invalid), Is.True); // 0 is included in All
        }

        [Test]
        public void FlagsAttribute_ShouldAllowCombinedFlags()
        {
            // Arrange
            var developmentAndProduction = ServerEnvironmentType.Development | ServerEnvironmentType.Production;

            // Assert
            Assert.That(developmentAndProduction.HasFlag(ServerEnvironmentType.Development), Is.True);
            Assert.That(developmentAndProduction.HasFlag(ServerEnvironmentType.Production), Is.True);
            Assert.That(developmentAndProduction.HasFlag(ServerEnvironmentType.Test), Is.False);
        }
    }
}
