using System;
using NUnit.Framework;
using SWLOR.Game.Server.ValueObject;
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
            Assert.AreEqual(new CustomData(), response.CustomData);
        }

        [Test]
        public void DialogResponseTests_Ctor_ShouldHaveSpecifiedValues()
        {
            DialogResponse response = new DialogResponse(
                "MyResponse", 
                false, 
                new Tuple<string, dynamic>("val1", 852),
                new Tuple<string, dynamic>("val2", false),
                new Tuple<string, dynamic>("val3", "myval3"));

            Assert.AreEqual("MyResponse", response.Text);
            Assert.AreEqual(false, response.IsActive);

            Assert.AreEqual(852, response.CustomData["val1"]);
            Assert.AreEqual(false, response.CustomData["val2"]);
            Assert.AreEqual("myval3", response.CustomData["val3"]);
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
            Assert.AreEqual(new CustomData(), response.CustomData);
        }
    }
}
