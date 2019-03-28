using NUnit.Framework;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Tests.ValueObject.Dialog
{
    public class DialogResponseTests
    {
        [Test]
        public void DialogResponseTests_Ctor_ShouldHaveDefaultValues()
        {
            DialogResponse response = new DialogResponse("MyResponse");

            Assert.AreEqual("MyResponse", response.Text);
            Assert.AreEqual(true, response.IsActive);
            Assert.AreEqual(null, response.CustomData);
        }

        [Test]
        public void DialogResponseTests_Ctor_ShouldHaveSpecifiedValues()
        {
            DialogResponse response = new DialogResponse(
                "MyResponse", 
                false, 
                852);

            Assert.AreEqual("MyResponse", response.Text);
            Assert.AreEqual(false, response.IsActive);

            Assert.AreEqual(852, response.CustomData);
        }

        [Test]
        public void DialogResponseTests_CtorNullData_ShouldHaveEmptyObject()
        {
            DialogResponse response = new DialogResponse(
                "MyResponse",
                false, 
                null);

            Assert.AreEqual("MyResponse", response.Text);
            Assert.AreEqual(false, response.IsActive);
            Assert.AreEqual(null, response.CustomData);
        }
    }
}
