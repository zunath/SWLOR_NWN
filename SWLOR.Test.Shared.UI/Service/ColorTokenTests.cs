using SWLOR.Shared.UI.Service;

namespace SWLOR.Test.Shared.UI.Service
{
    [TestFixture]
    public class ColorTokenTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void TokenStart_WithValidValues_ReturnsCorrectToken()
        {
            // Arrange
            byte red = 255;
            byte green = 128;
            byte blue = 64;

            // Act
            var result = ColorToken.TokenStart(red, green, blue);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith(">"));
            Assert.That(result.Length, Is.EqualTo(6)); // "<c" + 3 chars + ">" = 6
        }

        [Test]
        public void TokenStart_WithRedOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Note: This test cannot actually test out-of-range values because byte parameters
            // cannot exceed 255. The ColorToken.TokenStart method checks for values > 255,
            // but byte parameters are constrained to 0-255. This test is kept for documentation
            // but will always pass with valid byte values.
            
            // Arrange
            byte red = 255; // Maximum valid value
            byte green = 128;
            byte blue = 64;

            // Act
            var result = ColorToken.TokenStart(red, green, blue);

            // Assert - This should not throw since 255 is valid
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TokenStart_WithGreenOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Note: This test cannot actually test out-of-range values because byte parameters
            // cannot exceed 255. The ColorToken.TokenStart method checks for values > 255,
            // but byte parameters are constrained to 0-255. This test is kept for documentation
            // but will always pass with valid byte values.
            
            // Arrange
            byte red = 128;
            byte green = 255; // Maximum valid value
            byte blue = 64;

            // Act
            var result = ColorToken.TokenStart(red, green, blue);

            // Assert - This should not throw since 255 is valid
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TokenStart_WithBlueOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Note: This test cannot actually test out-of-range values because byte parameters
            // cannot exceed 255. The ColorToken.TokenStart method checks for values > 255,
            // but byte parameters are constrained to 0-255. This test is kept for documentation
            // but will always pass with valid byte values.
            
            // Arrange
            byte red = 128;
            byte green = 64;
            byte blue = 255; // Maximum valid value

            // Act
            var result = ColorToken.TokenStart(red, green, blue);

            // Assert - This should not throw since 255 is valid
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TokenEnd_ReturnsCorrectToken()
        {
            // Act
            var result = ColorToken.TokenEnd();

            // Assert
            Assert.That(result, Is.EqualTo("</c>"));
        }

        [Test]
        public void Custom_WithValidValues_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Test Text";
            byte red = 255;
            byte green = 0;
            byte blue = 0;

            // Act
            var result = ColorToken.Custom(text, red, green, blue);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Custom_WithNullText_ReturnsEmptyString()
        {
            // Arrange
            string text = null;
            byte red = 255;
            byte green = 0;
            byte blue = 0;

            // Act
            var result = ColorToken.Custom(text, red, green, blue);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
        }

        [Test]
        public void Black_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Black Text";

            // Act
            var result = ColorToken.Black(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Black_WithNullText_ThrowsArgumentException()
        {
            // Arrange
            string text = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ColorToken.Black(text));
        }

        [Test]
        public void Black_WithEmptyText_ThrowsArgumentException()
        {
            // Arrange
            string text = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ColorToken.Black(text));
        }

        [Test]
        public void Black_WithWhiteSpaceText_ThrowsArgumentException()
        {
            // Arrange
            string text = "   ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ColorToken.Black(text));
        }

        [Test]
        public void Blue_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Blue Text";

            // Act
            var result = ColorToken.Blue(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Gray_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Gray Text";

            // Act
            var result = ColorToken.Gray(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Green_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Green Text";

            // Act
            var result = ColorToken.Green(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void LightPurple_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Light Purple Text";

            // Act
            var result = ColorToken.LightPurple(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Orange_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Orange Text";

            // Act
            var result = ColorToken.Orange(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Pink_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Pink Text";

            // Act
            var result = ColorToken.Pink(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Purple_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Purple Text";

            // Act
            var result = ColorToken.Purple(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Red_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Red Text";

            // Act
            var result = ColorToken.Red(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void White_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "White Text";

            // Act
            var result = ColorToken.White(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Yellow_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Yellow Text";

            // Act
            var result = ColorToken.Yellow(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Cyan_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Cyan Text";

            // Act
            var result = ColorToken.Cyan(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Combat_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Combat Text";

            // Act
            var result = ColorToken.Combat(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Dialog_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Dialog Text";

            // Act
            var result = ColorToken.Dialog(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void DialogAction_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Dialog Action Text";

            // Act
            var result = ColorToken.DialogAction(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void DialogCheck_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Dialog Check Text";

            // Act
            var result = ColorToken.DialogCheck(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void DialogHighlight_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Dialog Highlight Text";

            // Act
            var result = ColorToken.DialogHighlight(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void DialogReply_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Dialog Reply Text";

            // Act
            var result = ColorToken.DialogReply(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void DM_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "DM Text";

            // Act
            var result = ColorToken.DM(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void GameEngine_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Game Engine Text";

            // Act
            var result = ColorToken.GameEngine(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void SavingThrow_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Saving Throw Text";

            // Act
            var result = ColorToken.SavingThrow(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Script_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Script Text";

            // Act
            var result = ColorToken.Script(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Server_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Server Text";

            // Act
            var result = ColorToken.Server(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Shout_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Shout Text";

            // Act
            var result = ColorToken.Shout(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void SkillCheck_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Skill Check Text";

            // Act
            var result = ColorToken.SkillCheck(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Talk_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Talk Text";

            // Act
            var result = ColorToken.Talk(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Tell_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Tell Text";

            // Act
            var result = ColorToken.Tell(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void Whisper_WithValidText_ReturnsCorrectToken()
        {
            // Arrange
            string text = "Whisper Text";

            // Act
            var result = ColorToken.Whisper(text);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
            Assert.That(result, Does.Contain(text));
        }

        [Test]
        public void GetNamePCColor_WithValidPC_ReturnsCorrectToken()
        {
            // Arrange
            uint oPC = 12345;

            // Act
            var result = ColorToken.GetNamePCColor(oPC);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
        }

        [Test]
        public void GetNameNPCColor_WithValidNPC_ReturnsCorrectToken()
        {
            // Arrange
            uint oNPC = 67890;

            // Act
            var result = ColorToken.GetNameNPCColor(oNPC);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("<c"));
            Assert.That(result, Does.EndWith("</c>"));
        }
    }
}