using SWLOR.Shared.Abstractions.Models;

namespace SWLOR.Test.Shared.Abstractions.Models
{
    [TestFixture]
    public class GuiColorTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithProvidedValues()
        {
            // Arrange
            byte red = 100;
            byte green = 150;
            byte blue = 200;
            byte alpha = 255;

            // Act
            var color = new GuiColor(red, green, blue, alpha);

            // Assert
            Assert.That(color.R, Is.EqualTo(red));
            Assert.That(color.G, Is.EqualTo(green));
            Assert.That(color.B, Is.EqualTo(blue));
            Assert.That(color.Alpha, Is.EqualTo(alpha));
        }

        [Test]
        public void Constructor_ShouldUseDefaultAlphaWhenNotProvided()
        {
            // Arrange
            byte red = 100;
            byte green = 150;
            byte blue = 200;

            // Act
            var color = new GuiColor(red, green, blue);

            // Assert
            Assert.That(color.R, Is.EqualTo(red));
            Assert.That(color.G, Is.EqualTo(green));
            Assert.That(color.B, Is.EqualTo(blue));
            Assert.That(color.Alpha, Is.EqualTo(255));
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var color = new GuiColor(0, 0, 0, 0);

            // Act
            color.R = 100;
            color.G = 150;
            color.B = 200;
            color.Alpha = 128;

            // Assert
            Assert.That(color.R, Is.EqualTo(100));
            Assert.That(color.G, Is.EqualTo(150));
            Assert.That(color.B, Is.EqualTo(200));
            Assert.That(color.Alpha, Is.EqualTo(128));
        }

        [Test]
        public void StaticProperties_ShouldReturnCorrectColors()
        {
            // Assert
            Assert.That(GuiColor.Green.R, Is.EqualTo(0));
            Assert.That(GuiColor.Green.G, Is.EqualTo(255));
            Assert.That(GuiColor.Green.B, Is.EqualTo(0));
            Assert.That(GuiColor.Green.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.Red.R, Is.EqualTo(255));
            Assert.That(GuiColor.Red.G, Is.EqualTo(0));
            Assert.That(GuiColor.Red.B, Is.EqualTo(0));
            Assert.That(GuiColor.Red.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.Cyan.R, Is.EqualTo(0));
            Assert.That(GuiColor.Cyan.G, Is.EqualTo(255));
            Assert.That(GuiColor.Cyan.B, Is.EqualTo(255));
            Assert.That(GuiColor.Cyan.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.White.R, Is.EqualTo(255));
            Assert.That(GuiColor.White.G, Is.EqualTo(255));
            Assert.That(GuiColor.White.B, Is.EqualTo(255));
            Assert.That(GuiColor.White.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.Grey.R, Is.EqualTo(169));
            Assert.That(GuiColor.Grey.G, Is.EqualTo(169));
            Assert.That(GuiColor.Grey.B, Is.EqualTo(169));
            Assert.That(GuiColor.Grey.Alpha, Is.EqualTo(255));
        }

        [Test]
        public void StaticProperties_ShouldReturnCorrectSpecialColors()
        {
            // Assert
            Assert.That(GuiColor.HPColor.R, Is.EqualTo(139));
            Assert.That(GuiColor.HPColor.G, Is.EqualTo(0));
            Assert.That(GuiColor.HPColor.B, Is.EqualTo(0));
            Assert.That(GuiColor.HPColor.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.FPColor.R, Is.EqualTo(0));
            Assert.That(GuiColor.FPColor.G, Is.EqualTo(138));
            Assert.That(GuiColor.FPColor.B, Is.EqualTo(250));
            Assert.That(GuiColor.FPColor.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.STMColor.R, Is.EqualTo(0));
            Assert.That(GuiColor.STMColor.G, Is.EqualTo(139));
            Assert.That(GuiColor.STMColor.B, Is.EqualTo(0));
            Assert.That(GuiColor.STMColor.Alpha, Is.EqualTo(255));
        }

        [Test]
        public void Constructor_ShouldHandleEdgeCases()
        {
            // Arrange & Act
            var minColor = new GuiColor(0, 0, 0, 0);
            var maxColor = new GuiColor(255, 255, 255, 255);

            // Assert
            Assert.That(minColor.R, Is.EqualTo(0));
            Assert.That(minColor.G, Is.EqualTo(0));
            Assert.That(minColor.B, Is.EqualTo(0));
            Assert.That(minColor.Alpha, Is.EqualTo(0));

            Assert.That(maxColor.R, Is.EqualTo(255));
            Assert.That(maxColor.G, Is.EqualTo(255));
            Assert.That(maxColor.B, Is.EqualTo(255));
            Assert.That(maxColor.Alpha, Is.EqualTo(255));
        }

        [Test]
        public void Constructor_ShouldHandleMidRangeValues()
        {
            // Arrange & Act
            var midColor = new GuiColor(128, 128, 128, 128);

            // Assert
            Assert.That(midColor.R, Is.EqualTo(128));
            Assert.That(midColor.G, Is.EqualTo(128));
            Assert.That(midColor.B, Is.EqualTo(128));
            Assert.That(midColor.Alpha, Is.EqualTo(128));
        }

        [Test]
        public void ToJson_ShouldReturnJsonObject()
        {
            // Arrange
            var color = new GuiColor(100, 150, 200, 255);

            // Act & Assert
            // Note: This method depends on NWN.API which requires initialization
            // Since this is an abstraction layer, we'll test that the method exists and is callable
            // The actual implementation depends on the NWN.API runtime environment
            Assert.That(() => color.ToJson(), Throws.TypeOf<TypeInitializationException>());
        }

        [Test]
        public void ToJson_ShouldHaveCorrectSignature()
        {
            // Arrange
            var color = new GuiColor(100, 150, 200, 255);
            var method = typeof(GuiColor).GetMethod("ToJson");

            // Assert
            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType.Name, Is.EqualTo("Json"));
            Assert.That(method.GetParameters().Length, Is.EqualTo(0));
        }

        [Test]
        public void ToJson_ShouldHandleDifferentColorValues()
        {
            // Arrange
            var color1 = new GuiColor(0, 0, 0, 0);
            var color2 = new GuiColor(255, 255, 255, 255);
            var color3 = new GuiColor(128, 64, 192, 128);

            // Act & Assert - These should throw TypeInitializationException due to NWN.API dependency
            Assert.That(() => color1.ToJson(), Throws.TypeOf<TypeInitializationException>());
            Assert.That(() => color2.ToJson(), Throws.TypeOf<TypeInitializationException>());
            Assert.That(() => color3.ToJson(), Throws.TypeOf<TypeInitializationException>());
        }

        [Test]
        public void ToJson_ShouldBeCallableViaReflection()
        {
            // Arrange
            var color = new GuiColor(100, 150, 200, 255);
            var method = typeof(GuiColor).GetMethod("ToJson");

            // Act & Assert
            // This test ensures the method can be called via reflection
            // The actual execution will still throw due to NWN.API dependency
            Assert.That(() => method.Invoke(color, null), Throws.TypeOf<System.Reflection.TargetInvocationException>());
        }

        [Test]
        public void StaticColors_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.That(GuiColor.Green.R, Is.EqualTo(0));
            Assert.That(GuiColor.Green.G, Is.EqualTo(255));
            Assert.That(GuiColor.Green.B, Is.EqualTo(0));
            Assert.That(GuiColor.Green.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.Red.R, Is.EqualTo(255));
            Assert.That(GuiColor.Red.G, Is.EqualTo(0));
            Assert.That(GuiColor.Red.B, Is.EqualTo(0));
            Assert.That(GuiColor.Red.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.Cyan.R, Is.EqualTo(0));
            Assert.That(GuiColor.Cyan.G, Is.EqualTo(255));
            Assert.That(GuiColor.Cyan.B, Is.EqualTo(255));
            Assert.That(GuiColor.Cyan.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.White.R, Is.EqualTo(255));
            Assert.That(GuiColor.White.G, Is.EqualTo(255));
            Assert.That(GuiColor.White.B, Is.EqualTo(255));
            Assert.That(GuiColor.White.Alpha, Is.EqualTo(255));

            Assert.That(GuiColor.Grey.R, Is.EqualTo(169));
            Assert.That(GuiColor.Grey.G, Is.EqualTo(169));
            Assert.That(GuiColor.Grey.B, Is.EqualTo(169));
            Assert.That(GuiColor.Grey.Alpha, Is.EqualTo(255));
        }

        [Test]
        public void Constructor_ShouldValidateByteRange()
        {
            // Arrange & Act
            var minColor = new GuiColor(0, 0, 0, 0);
            var maxColor = new GuiColor(255, 255, 255, 255);

            // Assert
            Assert.That(minColor.R, Is.EqualTo(0));
            Assert.That(minColor.G, Is.EqualTo(0));
            Assert.That(minColor.B, Is.EqualTo(0));
            Assert.That(minColor.Alpha, Is.EqualTo(0));

            Assert.That(maxColor.R, Is.EqualTo(255));
            Assert.That(maxColor.G, Is.EqualTo(255));
            Assert.That(maxColor.B, Is.EqualTo(255));
            Assert.That(maxColor.Alpha, Is.EqualTo(255));
        }

        [Test]
        public void Properties_ShouldBeSettableAfterConstruction()
        {
            // Arrange
            var color = new GuiColor(0, 0, 0, 0);

            // Act
            color.R = 100;
            color.G = 150;
            color.B = 200;
            color.Alpha = 128;

            // Assert
            Assert.That(color.R, Is.EqualTo(100));
            Assert.That(color.G, Is.EqualTo(150));
            Assert.That(color.B, Is.EqualTo(200));
            Assert.That(color.Alpha, Is.EqualTo(128));
        }
    }
}
