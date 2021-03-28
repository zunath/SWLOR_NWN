using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class QuicknessImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            QuicknessImplant1();
            QuicknessImplant2();
            QuicknessImplant3();
            QuicknessImplant4();
            QuicknessImplant5();

            return _builder.Build();
        }

        private void QuicknessImplant1()
        {
            _builder.Create("l_imp_qck1")
                .Name("Quickness")
                .Description("+2 Movement Speed")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Legs)
                .ModifyMovementRate(2);
        }

        private void QuicknessImplant2()
        {
            _builder.Create("l_imp_qck2")
                .Name("Quickness")
                .Description("+4 Movement Speed")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Legs)
                .ModifyMovementRate(4);
        }
        private void QuicknessImplant3()
        {
            _builder.Create("l_imp_qck3")
                .Name("Quickness")
                .Description("+6 Movement Speed")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Legs)
                .ModifyMovementRate(6);
        }
        private void QuicknessImplant4()
        {
            _builder.Create("l_imp_qck4")
                .Name("Quickness")
                .Description("+8 Movement Speed")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Legs)
                .ModifyMovementRate(8);
        }
        private void QuicknessImplant5()
        {
            _builder.Create("l_imp_qck5")
                .Name("Quickness")
                .Description("+10 Movement Speed")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Legs)
                .ModifyMovementRate(10);
        }
    }
}
