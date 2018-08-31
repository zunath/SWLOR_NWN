using System;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service
{
    public class ColorTokenServiceTests
    {
        [Test]
        public void ColorTokenService_NullText_ShouldThrowArgumentException()
        {
            ColorTokenService service = new ColorTokenService();

            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Black(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Blue(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Gray(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Green(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.LightPurple(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Orange(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Pink(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Purple(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Red(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.White(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Yellow(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Cyan(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Combat(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Dialog(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.DialogAction(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.DialogCheck(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.DialogHighlight(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.DialogReply(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.DM(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.GameEngine(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.SavingThrow(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Script(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Server(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Shout(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.SkillCheck(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Talk(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Tell(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Whisper(null);
            });
        }
        
        [Test]
        public void ColorTokenService_TokenStart_ShouldReturnColorMap()
        {
            ColorTokenService service = new ColorTokenService();
            string result = service.TokenStart(20, 30, 40);

            Assert.AreEqual("<c%>", result);
        }
        
        [Test]
        public void ColorTokenService_Custom_InvalidArguments_ShouldThrowArgumentException()
        {
            ColorTokenService service = new ColorTokenService();
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Custom(null, 0, 0, 0);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Custom(string.Empty, 0, 0, 0);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.Custom("    ", 0, 0, 0);
            });
        }

        [Test]
        public void ColorTokenService_Custom_ShouldReturnColorCode()
        {
            ColorTokenService service = new ColorTokenService();
            string result = service.Custom("mytext", 20, 30, 40);

            Assert.AreEqual("<c%>mytext</c>", result);
        }

    }
}
