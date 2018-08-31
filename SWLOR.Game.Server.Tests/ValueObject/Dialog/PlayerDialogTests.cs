using NUnit.Framework;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Tests.ValueObject.Dialog
{
    public class PlayerDialogTests
    {
        [Test]
        public void PlayerDialog_Ctor_ShouldHaveDefaultValues()
        {
            PlayerDialog dialog = new PlayerDialog("MyPage");

            Assert.AreEqual(string.Empty, dialog.CurrentPageName);
            Assert.AreEqual(0, dialog.PageOffset);
            Assert.AreEqual(string.Empty, dialog.ActiveDialogName);
            Assert.AreEqual(null, dialog.DialogTarget);
            Assert.AreEqual(new CustomData(), dialog.CustomData);
            Assert.AreEqual(false, dialog.IsEnding);
            Assert.AreEqual("MyPage", dialog.DefaultPageName);
            Assert.AreEqual(0, dialog.DialogNumber);
        }

        [Test]
        public void PlayerDialog_AddPage_ShouldBeSetToCurrentPage()
        {
            PlayerDialog dialog = new PlayerDialog("TestPage");
            DialogPage page = new DialogPage();
            dialog.AddPage("TestPage", page);

            Assert.AreEqual("TestPage", dialog.CurrentPageName);
        }

        [Test]
        public void PlayerDialog_AddPage_SecondPageShouldNotBeSetToCurrentPage()
        {
            PlayerDialog dialog = new PlayerDialog("TestPage");
            DialogPage page = new DialogPage();
            dialog.AddPage("TestPage", page);

            DialogPage page2 = new DialogPage();
            dialog.AddPage("Page2", page2);

            Assert.AreNotEqual("Page2", dialog.CurrentPageName);
        }

        [Test]
        public void PlayerDialog_CurrentPage_ShouldReturnAddedPage()
        {
            PlayerDialog dialog = new PlayerDialog("TestPage");
            DialogPage page = new DialogPage();
            dialog.AddPage("TestPage", page);

            DialogPage result = dialog.CurrentPage;

            Assert.AreSame(result, page);
        }

        [Test]
        public void PlayerDialog_GetPageByName_ShouldReturnPage2()
        {
            PlayerDialog dialog = new PlayerDialog("TestPage");
            DialogPage page = new DialogPage();
            dialog.AddPage("TestPage", page);

            DialogPage page2 = new DialogPage();
            dialog.AddPage("Page2", page2);

            DialogPage result = dialog.GetPageByName("Page2");
            Assert.AreSame(result, page2);
        }

        [Test]
        public void PlayerDialog_ResetPage_ShouldSetToPage1()
        {
            PlayerDialog dialog = new PlayerDialog("TestPage");
            DialogPage page = new DialogPage();
            dialog.AddPage("TestPage", page);

            DialogPage page2 = new DialogPage();
            dialog.AddPage("Page2", page2);

            dialog.CurrentPageName = "Page2";
            dialog.ResetPage();

            Assert.AreSame(page, dialog.CurrentPage);
        }

    }
}
