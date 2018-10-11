using System;
using System.Collections.Generic;
using Autofac.Core.Registration;
using NSubstitute;
using NUnit.Framework;
using NWN;
using SWLOR.Game.Server.Conversation;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Tests.Service
{
    internal class ValidConversation: ConversationBase
    {
        public ValidConversation(INWScript script, IDialogService dialog) : base(script, dialog)
        {
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage page = new DialogPage("My header", "response 1");
            dialog.AddPage("MainPage", page);

            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }

    public class DialogServiceTests
    {
        [Test]
        public void DialogService_NumberOfResponsesPerPage_ShouldBe12()
        {
            DialogService service = new DialogService(
                Substitute.For<INWScript>(),
                new AppState()
            );
            Assert.AreEqual(12, service.NumberOfResponsesPerPage);
        }

        [Test]
        public void DialogService_LoadPlayerDialog_InvalidGlobalID_ShouldThrowArgumentNullException()
        {
            DialogService service = new DialogService(
                Substitute.For<INWScript>(),
                new AppState()
            );

            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.LoadPlayerDialog(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.LoadPlayerDialog(string.Empty);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.LoadPlayerDialog("    ");
            });
        }

        [Test]
        public void DialogService_LoadPlayerDialog_NoPlayerDialog_ShouldThrowException()
        {
            DialogService service = new DialogService(
                Substitute.For<INWScript>(),
                new AppState()
            );

            Assert.Throws(typeof(Exception), () =>
            {
                service.LoadPlayerDialog("noid");
            });
        }

        [Test]
        public void DialogService_LoadConversation_InvalidArguments_ShouldThrowExceptions()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );

            NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
            NWObject talkTo = Substitute.For<NWObject>(script);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.LoadConversation(null, talkTo, "MyClass", 1);
            });
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.LoadConversation(player, null, "MyClass", 1);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.LoadConversation(player, talkTo, null, 1);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.LoadConversation(player, talkTo, string.Empty, 1);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.LoadConversation(player, talkTo, "   ", 1);
            });
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                service.LoadConversation(player, talkTo, "MyClass", -50);
            });
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                service.LoadConversation(player, talkTo, "MyClass", 0);
            });
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                service.LoadConversation(player, talkTo, "MyClass", 100);
            });
        }

        [Test]
        public void DialogService_LoadConversation_InvalidConversationClassName_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );

            NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
            NWObject talkTo = Substitute.For<NWObject>(script);

            Assert.Throws(typeof(ComponentNotRegisteredException), () =>
            {
                service.LoadConversation(player, talkTo, "MyClass", 20);
            });
        }

        [Test]
        public void DialogService_LoadConversation_ShouldReturnPlayerDialog()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );

            NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
            player.GlobalID.Returns(x => "123");
            NWObject talkTo = Substitute.For<NWObject>(script);

            service.LoadConversation(player, talkTo, "ValidConversation", 1);
            
            PlayerDialog dialog = service.LoadPlayerDialog(player.GlobalID);
            Assert.IsNotNull(dialog);
            Assert.AreEqual("MainPage", dialog.CurrentPageName);
            Assert.AreEqual(1, dialog.DialogNumber);
            Assert.AreSame(talkTo, dialog.DialogTarget);
            Assert.AreEqual("ValidConversation", dialog.ActiveDialogName);
        }

        [Test]
        public void DialogService_RemovePlayerDialog_InvalidArguments_ShouldThrowException()
        {
            DialogService service = new DialogService(
                Substitute.For<INWScript>(),
                new AppState()
            );

            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.RemovePlayerDialog(null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.RemovePlayerDialog(string.Empty);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.RemovePlayerDialog("   ");
            });
        }

        [Test]
        public void DialogService_RemovePlayerDialog_NotRegistered_ShouldThrowException()
        {
            DialogService service = new DialogService(
                Substitute.For<INWScript>(),
                new AppState()
            );

            Assert.Throws(typeof(KeyNotFoundException), () =>
            {
                service.RemovePlayerDialog("nonexistentkey");
            });
        }

        [Test]
        public void DialogService_RemovePlayerDialog_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );

            NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
            player.GlobalID.Returns(x => "123");
            NWObject talkTo = Substitute.For<NWObject>(script);

            service.LoadConversation(player, talkTo, "ValidConversation", 1);
            service.RemovePlayerDialog("123");

            Assert.Throws(typeof(Exception), () =>
            {
                service.LoadPlayerDialog("123");
            });
        }

        [Test]
        public void DialogService_StartConversation_InvalidArguments_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );

            NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
            player.GlobalID.Returns(x => "123");
            NWObject talkTo = Substitute.For<NWObject>(script);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.StartConversation(null, talkTo, "ValidConversation");
            });
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                player.Object.Returns(x => null);
                service.StartConversation(player, talkTo, "ValidConversation");
            });

            player = Substitute.For<NWPlayer>(script, nwnxCreature);
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.StartConversation(player, null, "ValidConversation");
            });

            talkTo.Object.Returns(x => null);
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.StartConversation(player, talkTo, "ValidConversation");
            });

            talkTo = Substitute.For<NWObject>(script);
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.StartConversation(player, talkTo, null);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.StartConversation(player, talkTo, string.Empty);
            });
            Assert.Throws(typeof(ArgumentException), () =>
            {
                service.StartConversation(player, talkTo, "   ");
            });
        }

        [Test]
        public void DialogService_StartConversation_NoDialogsAvailable_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );

            for (int number = 1; number <= 99; number++)
            {
                NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
                string id = Convert.ToString(number);
                player.GlobalID.Returns(x => id);
                NWObject talkTo = Substitute.For<NWObject>(script);

                service.LoadConversation(player, talkTo, "ValidConversation", number);
            }

            NWPlayer refusedPlayer = Substitute.For<NWPlayer>(script, nwnxCreature);
            NWCreature refusedTalkTo = Substitute.For<NWCreature>(script, nwnxCreature);

            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                service.StartConversation(refusedPlayer, refusedTalkTo, "ValidConversation");
            });
        }

        [Test]
        public void DialogService_EndConversation_InvalidArguments_ShouldThrowException()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );
            NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
            player.GlobalID.Returns(x => "123");
            NWObject talkTo = Substitute.For<NWObject>(script);

            service.LoadConversation(player, talkTo, "ValidConversation", 1);

            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                service.EndConversation(null);
            });
        }
        [Test]
        public void DialogService_EndConversation_ShouldBeMarkedAsEnding()
        {
            INWScript script = Substitute.For<INWScript>();
            INWNXCreature nwnxCreature = Substitute.For<INWNXCreature>();

            DialogService service = new DialogService(
                script,
                new AppState()
            );
            NWPlayer player = Substitute.For<NWPlayer>(script, nwnxCreature);
            player.GlobalID.Returns(x => "123");
            NWObject talkTo = Substitute.For<NWObject>(script);
            service.LoadConversation(player, talkTo, "ValidConversation", 1);
            PlayerDialog dialog = service.LoadPlayerDialog("123");
            service.EndConversation(player);
            
            Assert.AreEqual(true, dialog.IsEnding);
        }
    }
}