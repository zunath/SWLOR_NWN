using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class TestImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            TestImplant();
            TestImplant2();
            return _builder.Build();
        }

        private void TestImplant()
        {
            _builder.Create("test_implant")
                .Name("Test Implant")
                .Description("This is a test")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Body)
                .ValidationAction(creature =>
                {
                    return string.Empty;
                })
                .InstalledAction(creature =>
                {
                    Console.WriteLine("Installed test implant");
                });
        }
        private void TestImplant2()
        {
            _builder.Create("test_implant2")
                .Name("Test Implant 2")
                .Description("This is a test 2")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Body)
                .ValidationAction(creature =>
                {
                    return string.Empty;
                })
                .InstalledAction(creature =>
                {
                    Console.WriteLine("Installed test implant");
                });
        }
    }
}
