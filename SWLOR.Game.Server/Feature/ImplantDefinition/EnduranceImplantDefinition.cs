using System.Collections.Generic;
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
                .Description("+4 HP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(4);

            // Body
            _builder.Create("b_imp_end1")
                .Name("Endurance")
                .Description("+5 HP, -2 FP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(5)
                .ModifyFP(-2);

            // Legs
            _builder.Create("l_imp_end1")
                .Name("Endurance")
                .Description("+4 HP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(4);

            // Arms
            _builder.Create("a_imp_end1")
                .Name("Endurance")
                .Description("+4 HP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(4);
        }

        private void EnduranceImplant2()
        {
            // Head
            _builder.Create("h_imp_end2")
                .Name("Endurance")
                .Description("+6 HP, -2 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(6)
                .ModifyFP(-2);

            // Body
            _builder.Create("b_imp_end2")
                .Name("Endurance")
                .Description("+7 HP, +1 HP Regen, -2 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(7)
                .ModifyHPRegen(1)
                .ModifyFP(-4);

            // Legs
            _builder.Create("l_imp_end2")
                .Name("Endurance")
                .Description("+6 HP, -2 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(6)
                .ModifyFP(-2);

            // Arms
            _builder.Create("a_imp_end2")
                .Name("Endurance")
                .Description("+6 HP, -2 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(6)
                .ModifyFP(-2);
        }

        private void EnduranceImplant3()
        {
            // Head
            _builder.Create("h_imp_end3")
                .Name("Endurance")
                .Description("+8 HP, -4 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(8)
                .ModifyFP(-4);

            // Body
            _builder.Create("b_imp_end3")
                .Name("Endurance")
                .Description("+9 HP, +1 HP Regen, -6 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(9)
                .ModifyHPRegen(1)
                .ModifyFP(-6);
            
            // Legs
            _builder.Create("l_imp_end3")
                .Name("Endurance")
                .Description("+8 HP, -4 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(8)
                .ModifyFP(-4);
            
            // Arms
            _builder.Create("a_imp_end3")
                .Name("Endurance")
                .Description("+8 HP, -4 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(8)
                .ModifyFP(-4);
        }
        private void EnduranceImplant4()
        {
            // Head
            _builder.Create("h_imp_end4")
                .Name("Endurance")
                .Description("+10 HP, -6 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(10)
                .ModifyFP(-6);

            // Body
            _builder.Create("b_imp_end4")
                .Name("Endurance")
                .Description("+11 HP, +1 HP Regen, -8 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(11)
                .ModifyHPRegen(1)
                .ModifyFP(-8);

            // Legs
            _builder.Create("l_imp_end4")
                .Name("Endurance")
                .Description("+10 HP, -6 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(10)
                .ModifyFP(-6);

            // Arms
            _builder.Create("a_imp_end4")
                .Name("Endurance")
                .Description("+10 HP, -6 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(10)
                .ModifyFP(-6);
        }
        private void EnduranceImplant5()
        {
            // Head
            _builder.Create("h_imp_end5")
                .Name("Endurance")
                .Description("+12 HP, -8 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Head)
                .ModifyHP(12)
                .ModifyFP(-8);

            // Body
            _builder.Create("b_imp_end5")
                .Name("Endurance")
                .Description("+13 HP, +1 HP Regen, -10 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Body)
                .ModifyHP(13)
                .ModifyHPRegen(1)
                .ModifyFP(-10);

            // Legs
            _builder.Create("l_imp_end5")
                .Name("Endurance")
                .Description("+12 HP, -8 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Legs)
                .ModifyHP(12)
                .ModifyFP(-8);

            // Arms
            _builder.Create("a_imp_end5")
                .Name("Endurance")
                .Description("+12 HP, -8 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Arms)
                .ModifyHP(12)
                .ModifyFP(-8);
        }
    }
}
