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
            return _builder.Build();
        }

        private void TestImplant()
        {
            _builder.Create("test_implant")
                .Name("Test Implant")
                .Description("This is a test")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Chest)
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
