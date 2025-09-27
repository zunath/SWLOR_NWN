using NUnit.Framework;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiTextTextureTests
    {
        [Test]
        public void GuiFontName_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.GuiFontName, Is.EqualTo("fnt_es_gui"));
        }

        [Test]
        public void TextName_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.TextName, Is.EqualTo("fnt_es_text"));
        }

        [Test]
        public void WindowTopLeft_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowTopLeft, Is.EqualTo("a"));
        }

        [Test]
        public void WindowTopMiddle_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowTopMiddle, Is.EqualTo("b"));
        }

        [Test]
        public void WindowTopRight_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowTopRight, Is.EqualTo("c"));
        }

        [Test]
        public void WindowMiddleLeft_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowMiddleLeft, Is.EqualTo("d"));
        }

        [Test]
        public void WindowMiddleRight_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowMiddleRight, Is.EqualTo("f"));
        }

        [Test]
        public void WindowMiddleBlank_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowMiddleBlank, Is.EqualTo("i"));
        }

        [Test]
        public void WindowBottomLeft_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowBottomLeft, Is.EqualTo("h"));
        }

        [Test]
        public void WindowBottomRight_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowBottomRight, Is.EqualTo("g"));
        }

        [Test]
        public void WindowBottomMiddle_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.WindowBottomMiddle, Is.EqualTo("e"));
        }

        [Test]
        public void Arrow_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.Arrow, Is.EqualTo("j"));
        }

        [Test]
        public void BlankWhite_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(GuiTextTexture.BlankWhite, Is.EqualTo("k"));
        }
    }
}
