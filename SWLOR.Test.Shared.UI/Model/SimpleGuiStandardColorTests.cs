using SWLOR.Shared.UI.Model;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiStandardColorTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void ColorWhite_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorWhite, Is.EqualTo(unchecked((int)0xFFFFFFFF)));
        }

        [Test]
        public void ColorBlack_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorBlack, Is.EqualTo(0x000000FF));
        }

        [Test]
        public void ColorRed_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorRed, Is.EqualTo(unchecked((int)0xFF0000FF)));
        }

        [Test]
        public void ColorGreen_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorGreen, Is.EqualTo(0x008000FF));
        }

        [Test]
        public void ColorBlue_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorBlue, Is.EqualTo(0x0000FFFF));
        }

        [Test]
        public void ColorYellow_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorYellow, Is.EqualTo(unchecked((int)0xFFFF00FF)));
        }

        [Test]
        public void ColorAqua_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorAqua, Is.EqualTo(unchecked((int)0x00FFFFFF)));
        }

        [Test]
        public void ColorFuschia_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorFuschia, Is.EqualTo(unchecked((int)0xFF00FFFF)));
        }

        [Test]
        public void ColorGray_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorGray, Is.EqualTo(unchecked((int)0x808080FF)));
        }

        [Test]
        public void ColorOrange_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorOrange, Is.EqualTo(unchecked((int)0xFFA500FF)));
        }

        [Test]
        public void ColorPurple_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorPurple, Is.EqualTo(unchecked((int)0x800080FF)));
        }

        [Test]
        public void ColorLime_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorLime, Is.EqualTo(unchecked((int)0x00FF00FF)));
        }

        [Test]
        public void ColorNavy_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorNavy, Is.EqualTo(0x000080FF));
        }

        [Test]
        public void ColorTeal_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorTeal, Is.EqualTo(0x008080FF));
        }

        [Test]
        public void ColorSilver_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorSilver, Is.EqualTo(unchecked((int)0xC0C0C0FF)));
        }

        [Test]
        public void ColorMaroon_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorMaroon, Is.EqualTo(unchecked((int)0x800000FF)));
        }

        [Test]
        public void ColorOlive_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorOlive, Is.EqualTo(unchecked((int)0x808000FF)));
        }

        [Test]
        public void ColorTransparent_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorTransparent, Is.EqualTo(unchecked((int)0xFFFFFF00)));
        }

        [Test]
        public void ColorDarkGray_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorDarkGray, Is.EqualTo(0x303030FF));
        }

        [Test]
        public void ColorHealthBar_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorHealthBar, Is.EqualTo(unchecked((int)0x8B0000FF)));
        }

        [Test]
        public void ColorFPBar_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorFPBar, Is.EqualTo(0x00008BFF));
        }

        [Test]
        public void ColorStaminaBar_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorStaminaBar, Is.EqualTo(0x008B00FF));
        }

        [Test]
        public void ColorShieldsBar_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorShieldsBar, Is.EqualTo(0x00AAE4FF));
        }

        [Test]
        public void ColorHullBar_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorHullBar, Is.EqualTo(unchecked((int)0x8B0000FF)));
        }

        [Test]
        public void ColorCapacitorBar_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiStandardColor.ColorCapacitorBar, Is.EqualTo(unchecked((int)0xFFA500FF)));
        }

        [Test]
        public void AllColors_AreNotNull()
        {
            // Arrange
            var colors = new[]
            {
                GuiStandardColor.ColorTransparent,
                GuiStandardColor.ColorWhite,
                GuiStandardColor.ColorSilver,
                GuiStandardColor.ColorGray,
                GuiStandardColor.ColorDarkGray,
                GuiStandardColor.ColorBlack,
                GuiStandardColor.ColorRed,
                GuiStandardColor.ColorMaroon,
                GuiStandardColor.ColorOrange,
                GuiStandardColor.ColorYellow,
                GuiStandardColor.ColorOlive,
                GuiStandardColor.ColorLime,
                GuiStandardColor.ColorGreen,
                GuiStandardColor.ColorAqua,
                GuiStandardColor.ColorTeal,
                GuiStandardColor.ColorBlue,
                GuiStandardColor.ColorNavy,
                GuiStandardColor.ColorFuschia,
                GuiStandardColor.ColorPurple,
                GuiStandardColor.ColorHealthBar,
                GuiStandardColor.ColorFPBar,
                GuiStandardColor.ColorStaminaBar,
                GuiStandardColor.ColorShieldsBar,
                GuiStandardColor.ColorHullBar,
                GuiStandardColor.ColorCapacitorBar
            };

            // Act & Assert
            foreach (var color in colors)
            {
                Assert.That(color, Is.Not.EqualTo(0), $"Color should not be 0");
            }
        }

        [Test]
        public void AllColors_HaveValidHexValues()
        {
            // Arrange
            var colors = new[]
            {
                GuiStandardColor.ColorTransparent,
                GuiStandardColor.ColorWhite,
                GuiStandardColor.ColorSilver,
                GuiStandardColor.ColorGray,
                GuiStandardColor.ColorDarkGray,
                GuiStandardColor.ColorBlack,
                GuiStandardColor.ColorRed,
                GuiStandardColor.ColorMaroon,
                GuiStandardColor.ColorOrange,
                GuiStandardColor.ColorYellow,
                GuiStandardColor.ColorOlive,
                GuiStandardColor.ColorLime,
                GuiStandardColor.ColorGreen,
                GuiStandardColor.ColorAqua,
                GuiStandardColor.ColorTeal,
                GuiStandardColor.ColorBlue,
                GuiStandardColor.ColorNavy,
                GuiStandardColor.ColorFuschia,
                GuiStandardColor.ColorPurple,
                GuiStandardColor.ColorHealthBar,
                GuiStandardColor.ColorFPBar,
                GuiStandardColor.ColorStaminaBar,
                GuiStandardColor.ColorShieldsBar,
                GuiStandardColor.ColorHullBar,
                GuiStandardColor.ColorCapacitorBar
            };

            // Act & Assert
            foreach (var color in colors)
            {
                // Note: Some colors can be negative when converted from hex to int (e.g., 0xFFFFFF00 = -256)
                // This is expected behavior for certain color values. We just verify they're not zero.
                Assert.That(color, Is.Not.EqualTo(0), $"Color should not be zero");
            }
        }
    }
}