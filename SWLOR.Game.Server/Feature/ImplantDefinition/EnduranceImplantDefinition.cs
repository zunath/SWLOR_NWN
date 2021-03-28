using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class EnduranceImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            EnduranceImplant1();
            EnduranceImplant2();
            EnduranceImplant3();
            EnduranceImplant4();
            EnduranceImplant5();

            return _builder.Build();
        }

        private void EnduranceImplant1()
        {
            // Head
            _builder.Create("h_imp_end1")
                .Name("Endurance")
                .Description("+2 HP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(2);

            // Body
            _builder.Create("b_imp_end1")
                .Name("Endurance")
                .Description("+3 HP, -1 FP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(3)
                .ModifyFP(-1);

            // Legs
            _builder.Create("l_imp_end1")
                .Name("Endurance")
                .Description("+2 HP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(2);

            // Arms
            _builder.Create("a_imp_end1")
                .Name("Endurance")
                .Description("+2 HP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(2);
        }

        private void EnduranceImplant2()
        {
            // Head
            _builder.Create("h_imp_end2")
                .Name("Endurance")
                .Description("+3 HP, -1 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(3)
                .ModifyFP(-1);

            // Body
            _builder.Create("b_imp_end2")
                .Name("Endurance")
                .Description("+4 HP, -2 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(4)
                .ModifyFP(-2);

            // Legs
            _builder.Create("l_imp_end2")
                .Name("Endurance")
                .Description("+3 HP, -1 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(3)
                .ModifyFP(-1);

            // Arms
            _builder.Create("a_imp_end2")
                .Name("Endurance")
                .Description("+3 HP, -1 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(3)
                .ModifyFP(-1);
        }

        private void EnduranceImplant3()
        {
            // Head
            _builder.Create("h_imp_end3")
                .Name("Endurance")
                .Description("+4 HP, -2 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(4)
                .ModifyFP(-2);

            // Body
            _builder.Create("b_imp_end3")
                .Name("Endurance")
                .Description("+5 HP, -3 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(5)
                .ModifyFP(-3);
            
            // Legs
            _builder.Create("l_imp_end3")
                .Name("Endurance")
                .Description("+4 HP, -2 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(4)
                .ModifyFP(-2);
            
            // Arms
            _builder.Create("a_imp_end3")
                .Name("Endurance")
                .Description("+4 HP, -2 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(4)
                .ModifyFP(-2);
        }
        private void EnduranceImplant4()
        {
            // Head
            _builder.Create("h_imp_end4")
                .Name("Endurance")
                .Description("+5 HP, -3 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(5)
                .ModifyFP(-3);

            // Body
            _builder.Create("b_imp_end4")
                .Name("Endurance")
                .Description("+6 HP, -4 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(6)
                .ModifyFP(-4);

            // Legs
            _builder.Create("l_imp_end4")
                .Name("Endurance")
                .Description("+5 HP, -3 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(5)
                .ModifyFP(-3);

            // Arms
            _builder.Create("a_imp_end4")
                .Name("Endurance")
                .Description("+5 HP, -3 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(5)
                .ModifyFP(-3);
        }
        private void EnduranceImplant5()
        {
            // Head
            _builder.Create("h_imp_end5")
                .Name("Endurance")
                .Description("+6 HP, -4 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(6)
                .ModifyFP(-4);

            // Body
            _builder.Create("b_imp_end5")
                .Name("Endurance")
                .Description("+7 HP, -5 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(7)
                .ModifyFP(-5);

            // Legs
            _builder.Create("l_imp_end5")
                .Name("Endurance")
                .Description("+6 HP, -4 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(6)
                .ModifyFP(-4);

            // Arms
            _builder.Create("a_imp_end5")
                .Name("Endurance")
                .Description("+6 HP, -4 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(6)
                .ModifyFP(-4);
        }
    }
}
