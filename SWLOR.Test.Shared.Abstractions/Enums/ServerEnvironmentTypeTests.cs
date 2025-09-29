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
            Assert.That((int)ServerEnvironmentType.All, Is.EqualTo(7)); // 1 | 2 | 4
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
        public void EnumValues_ShouldBePowersOfTwo()
        {
            // Arrange
            var values = new[] { ServerEnvironmentType.Development, ServerEnvironmentType.Production, ServerEnvironmentType.Test };

            // Assert
            foreach (var value in values)
            {
                var intValue = (int)value;
                Assert.That(intValue, Is.GreaterThan(0));
                Assert.That((intValue & (intValue - 1)), Is.EqualTo(0), $"{value} should be a power of 2");
            }
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
        public void EnumParse_ShouldBeCaseInsensitive()
        {
            // Assert
            Assert.That(Enum.Parse<ServerEnvironmentType>("invalid", true), Is.EqualTo(ServerEnvironmentType.Invalid));
            Assert.That(Enum.Parse<ServerEnvironmentType>("DEVELOPMENT", true), Is.EqualTo(ServerEnvironmentType.Development));
            Assert.That(Enum.Parse<ServerEnvironmentType>("production", true), Is.EqualTo(ServerEnvironmentType.Production));
            Assert.That(Enum.Parse<ServerEnvironmentType>("Test", true), Is.EqualTo(ServerEnvironmentType.Test));
            Assert.That(Enum.Parse<ServerEnvironmentType>("ALL", true), Is.EqualTo(ServerEnvironmentType.All));
        }

        [Test]
        public void EnumTryParse_ShouldWorkCorrectly()
        {
            // Arrange
            ServerEnvironmentType result;

            // Act & Assert
            Assert.That(Enum.TryParse<ServerEnvironmentType>("Invalid", out result), Is.True);
            Assert.That(result, Is.EqualTo(ServerEnvironmentType.Invalid));

            Assert.That(Enum.TryParse<ServerEnvironmentType>("Development", out result), Is.True);
            Assert.That(result, Is.EqualTo(ServerEnvironmentType.Development));

            Assert.That(Enum.TryParse<ServerEnvironmentType>("InvalidValue", out result), Is.False);
            Assert.That(result, Is.EqualTo(default(ServerEnvironmentType)));
        }

        [Test]
        public void EnumHasFlag_ShouldWorkCorrectly()
        {
            // Arrange
            var all = ServerEnvironmentType.All;
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;
            var test = ServerEnvironmentType.Test;

            // Act & Assert
            Assert.That(all.HasFlag(ServerEnvironmentType.Development), Is.True);
            Assert.That(all.HasFlag(ServerEnvironmentType.Production), Is.True);
            Assert.That(all.HasFlag(ServerEnvironmentType.Test), Is.True);
            Assert.That(all.HasFlag(ServerEnvironmentType.Invalid), Is.True); // Invalid = 0, so HasFlag returns true

            Assert.That(dev.HasFlag(ServerEnvironmentType.Development), Is.True);
            Assert.That(dev.HasFlag(ServerEnvironmentType.Production), Is.False);
            Assert.That(dev.HasFlag(ServerEnvironmentType.Test), Is.False);

            Assert.That(prod.HasFlag(ServerEnvironmentType.Development), Is.False);
            Assert.That(prod.HasFlag(ServerEnvironmentType.Production), Is.True);
            Assert.That(prod.HasFlag(ServerEnvironmentType.Test), Is.False);

            Assert.That(test.HasFlag(ServerEnvironmentType.Development), Is.False);
            Assert.That(test.HasFlag(ServerEnvironmentType.Production), Is.False);
            Assert.That(test.HasFlag(ServerEnvironmentType.Test), Is.True);
        }

        [Test]
        public void EnumFlags_ShouldSupportBitwiseOperations()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;
            var test = ServerEnvironmentType.Test;

            // Act
            var devAndProd = dev | prod;
            var devAndTest = dev | test;
            var allThree = dev | prod | test;
            var devAndProdAndTest = dev | prod | test;

            // Assert
            Assert.That(devAndProd, Is.EqualTo(ServerEnvironmentType.Development | ServerEnvironmentType.Production));
            Assert.That(devAndTest, Is.EqualTo(ServerEnvironmentType.Development | ServerEnvironmentType.Test));
            Assert.That(allThree, Is.EqualTo(ServerEnvironmentType.All));
            Assert.That(devAndProdAndTest, Is.EqualTo(ServerEnvironmentType.All));
        }

        [Test]
        public void EnumFlags_ShouldSupportBitwiseAnd()
        {
            // Arrange
            var all = ServerEnvironmentType.All;
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;

            // Act
            var devFromAll = all & dev;
            var prodFromAll = all & prod;
            var devAndProd = dev & prod;

            // Assert
            Assert.That(devFromAll, Is.EqualTo(ServerEnvironmentType.Development));
            Assert.That(prodFromAll, Is.EqualTo(ServerEnvironmentType.Production));
            Assert.That(devAndProd, Is.EqualTo(ServerEnvironmentType.Invalid));
        }

        [Test]
        public void EnumFlags_ShouldSupportBitwiseXor()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;
            var all = ServerEnvironmentType.All;

            // Act
            var devXorProd = dev ^ prod;
            var allXorDev = all ^ dev;

            // Assert
            Assert.That(devXorProd, Is.EqualTo(ServerEnvironmentType.Development | ServerEnvironmentType.Production));
            Assert.That(allXorDev, Is.EqualTo(ServerEnvironmentType.Production | ServerEnvironmentType.Test));
        }

        [Test]
        public void EnumFlags_ShouldSupportBitwiseNot()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var all = ServerEnvironmentType.All;

            // Act
            var notDev = ~dev;
            var notAll = ~all;

            // Assert
            Assert.That(notDev, Is.EqualTo((ServerEnvironmentType)(-2))); // Direct bitwise NOT of 1 = -2
            Assert.That(notAll, Is.EqualTo((ServerEnvironmentType)(-8))); // Direct bitwise NOT of 7 = -8
        }

        [Test]
        public void EnumFlags_ShouldSupportToStringWithFlags()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;
            var test = ServerEnvironmentType.Test;
            var devAndProd = dev | prod;
            var all = ServerEnvironmentType.All;

            // Act
            var devString = dev.ToString("F");
            var devAndProdString = devAndProd.ToString("F");
            var allString = all.ToString("F");

            // Assert
            Assert.That(devString, Is.EqualTo("Development"));
            Assert.That(devAndProdString, Is.EqualTo("Development, Production"));
            Assert.That(allString, Is.EqualTo("All")); // All is explicitly defined
        }

        [Test]
        public void EnumFlags_ShouldSupportToStringWithG()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var devAndProd = ServerEnvironmentType.Development | ServerEnvironmentType.Production;

            // Act
            var devString = dev.ToString("G");
            var devAndProdString = devAndProd.ToString("G");

            // Assert
            Assert.That(devString, Is.EqualTo("Development"));
            Assert.That(devAndProdString, Is.EqualTo("Development, Production"));
        }

        [Test]
        public void EnumFlags_ShouldSupportToStringWithD()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;
            var test = ServerEnvironmentType.Test;
            var all = ServerEnvironmentType.All;

            // Act
            var devString = dev.ToString("D");
            var prodString = prod.ToString("D");
            var testString = test.ToString("D");
            var allString = all.ToString("D");

            // Assert
            Assert.That(devString, Is.EqualTo("1"));
            Assert.That(prodString, Is.EqualTo("2"));
            Assert.That(testString, Is.EqualTo("4"));
            Assert.That(allString, Is.EqualTo("7"));
        }

        [Test]
        public void EnumFlags_ShouldSupportToStringWithX()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;
            var test = ServerEnvironmentType.Test;
            var all = ServerEnvironmentType.All;

            // Act
            var devString = dev.ToString("X");
            var prodString = prod.ToString("X");
            var testString = test.ToString("X");
            var allString = all.ToString("X");

            // Assert
            Assert.That(devString, Is.EqualTo("00000001"));
            Assert.That(prodString, Is.EqualTo("00000002"));
            Assert.That(testString, Is.EqualTo("00000004"));
            Assert.That(allString, Is.EqualTo("00000007"));
        }

        [Test]
        public void EnumFlags_ShouldSupportGetValues()
        {
            // Act
            var values = Enum.GetValues<ServerEnvironmentType>();

            // Assert
            Assert.That(values, Is.Not.Null);
            Assert.That(values.Length, Is.EqualTo(5)); // Invalid, Development, Production, Test, All
            Assert.That(values, Does.Contain(ServerEnvironmentType.Invalid));
            Assert.That(values, Does.Contain(ServerEnvironmentType.Development));
            Assert.That(values, Does.Contain(ServerEnvironmentType.Production));
            Assert.That(values, Does.Contain(ServerEnvironmentType.Test));
            Assert.That(values, Does.Contain(ServerEnvironmentType.All));
        }

        [Test]
        public void EnumFlags_ShouldSupportGetNames()
        {
            // Act
            var names = Enum.GetNames<ServerEnvironmentType>();

            // Assert
            Assert.That(names, Is.Not.Null);
            Assert.That(names.Length, Is.EqualTo(5));
            Assert.That(names, Does.Contain("Invalid"));
            Assert.That(names, Does.Contain("Development"));
            Assert.That(names, Does.Contain("Production"));
            Assert.That(names, Does.Contain("Test"));
            Assert.That(names, Does.Contain("All"));
        }

        [Test]
        public void EnumFlags_ShouldSupportIsDefined()
        {
            // Assert
            Assert.That(Enum.IsDefined<ServerEnvironmentType>(ServerEnvironmentType.Invalid), Is.True);
            Assert.That(Enum.IsDefined<ServerEnvironmentType>(ServerEnvironmentType.Development), Is.True);
            Assert.That(Enum.IsDefined<ServerEnvironmentType>(ServerEnvironmentType.Production), Is.True);
            Assert.That(Enum.IsDefined<ServerEnvironmentType>(ServerEnvironmentType.Test), Is.True);
            Assert.That(Enum.IsDefined<ServerEnvironmentType>(ServerEnvironmentType.All), Is.True);
            Assert.That(Enum.IsDefined<ServerEnvironmentType>((ServerEnvironmentType)8), Is.False);
            Assert.That(Enum.IsDefined<ServerEnvironmentType>((ServerEnvironmentType)(-1)), Is.False);
        }

        [Test]
        public void EnumFlags_ShouldSupportEquals()
        {
            // Arrange
            var dev1 = ServerEnvironmentType.Development;
            var dev2 = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;

            // Act & Assert
            Assert.That(dev1.Equals(dev2), Is.True);
            Assert.That(dev1.Equals(prod), Is.False);
            Assert.That(dev1.Equals((object)dev2), Is.True);
            Assert.That(dev1.Equals((object)prod), Is.False);
        }

        [Test]
        public void EnumFlags_ShouldSupportGetHashCode()
        {
            // Arrange
            var dev1 = ServerEnvironmentType.Development;
            var dev2 = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;

            // Act & Assert
            Assert.That(dev1.GetHashCode(), Is.EqualTo(dev2.GetHashCode()));
            Assert.That(dev1.GetHashCode(), Is.Not.EqualTo(prod.GetHashCode()));
        }

        [Test]
        public void EnumFlags_ShouldSupportCompareTo()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;
            var prod = ServerEnvironmentType.Production;
            var test = ServerEnvironmentType.Test;

            // Act & Assert
            Assert.That(dev.CompareTo(prod), Is.LessThan(0));
            Assert.That(prod.CompareTo(dev), Is.GreaterThan(0));
            Assert.That(dev.CompareTo(dev), Is.EqualTo(0));
            Assert.That(dev.CompareTo(test), Is.LessThan(0));
        }

        [Test]
        public void EnumFlags_ShouldSupportGetType()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;

            // Act
            var type = dev.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(ServerEnvironmentType)));
        }

        [Test]
        public void EnumFlags_ShouldSupportGetTypeCode()
        {
            // Arrange
            var dev = ServerEnvironmentType.Development;

            // Act
            var typeCode = dev.GetTypeCode();

            // Assert
            Assert.That(typeCode, Is.EqualTo(TypeCode.Int32));
        }
    }
}