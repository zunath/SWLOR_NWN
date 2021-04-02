using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class StaminaImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            StaminaImplant1();
            StaminaImplant2();
            StaminaImplant3();
            StaminaImplant4();
            StaminaImplant5();

            return _builder.Build();
        }

        private void StaminaImplant1()
        {
            // Body
            _builder.Create("b_imp_stm1")
                .Name("Stamina")
                .Description("+2 STM")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Body)
                .ModifySTM(2);

            // Legs
            _builder.Create("l_imp_stm1")
                .Name("Stamina")
                .Description("+3 STM, -1 FP")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Legs)
                .ModifySTM(3)
                .ModifyFP(-1);

            // Arms
            _builder.Create("a_imp_stm1")
                .Name("Stamina")
                .Description("+1 STM")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Arms)
                .ModifySTM(1);
        }

        private void StaminaImplant2()
        {
            // Body
            _builder.Create("b_imp_stm2")
                .Name("Stamina")
                .Description("+3 STM, -1 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Body)
                .ModifySTM(3)
                .ModifyFP(-1);

            // Legs
            _builder.Create("l_imp_stm2")
                .Name("Stamina")
                .Description("+4 STM, +1 STM Regen, -2 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Legs)
                .ModifySTM(4)
                .ModifySTMRegen(1)
                .ModifyFP(-2);

            // Arms
            _builder.Create("a_imp_stm2")
                .Name("Stamina")
                .Description("+2 STM, -1 FP")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Arms)
                .ModifySTM(2)
                .ModifyFP(-1);
        }

        private void StaminaImplant3()
        {
            // Body
            _builder.Create("b_imp_stm3")
                .Name("Stamina")
                .Description("+4 STM, -2 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Body)
                .ModifySTM(4)
                .ModifyFP(-2);
            
            // Legs
            _builder.Create("l_imp_stm3")
                .Name("Stamina")
                .Description("+5 STM, +1 STM Regen, -3 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Legs)
                .ModifySTM(5)
                .ModifySTMRegen(1)
                .ModifyFP(-3);
            
            // Arms
            _builder.Create("a_imp_stm3")
                .Name("Stamina")
                .Description("+3 STM, -2 FP")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Arms)
                .ModifySTM(3)
                .ModifyFP(-2);
        }
        private void StaminaImplant4()
        {
            // Body
            _builder.Create("b_imp_stm4")
                .Name("Stamina")
                .Description("+5 STM, -3 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Body)
                .ModifySTM(5)
                .ModifyFP(-3);

            // Legs
            _builder.Create("l_imp_stm4")
                .Name("Stamina")
                .Description("+6 STM, +1 STM Regen, -4 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Legs)
                .ModifySTM(6)
                .ModifySTMRegen(1)
                .ModifyFP(-4);

            // Arms
            _builder.Create("a_imp_stm4")
                .Name("Stamina")
                .Description("+4 STM, -3 FP")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Arms)
                .ModifySTM(4)
                .ModifyFP(-3);
        }
        private void StaminaImplant5()
        {
            // Body
            _builder.Create("b_imp_stm5")
                .Name("Stamina")
                .Description("+6 STM, -4 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Body)
                .ModifySTM(6)
                .ModifyFP(-4);

            // Legs
            _builder.Create("l_imp_stm5")
                .Name("Stamina")
                .Description("+7 STM, +1 STM Regen, -5 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Legs)
                .ModifySTM(7)
                .ModifySTMRegen(1)
                .ModifyFP(-5);

            // Arms
            _builder.Create("a_imp_stm5")
                .Name("Stamina")
                .Description("+5 STM, -4 FP")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Arms)
                .ModifySTM(5)
                .ModifyFP(-4);
        }
    }
}
