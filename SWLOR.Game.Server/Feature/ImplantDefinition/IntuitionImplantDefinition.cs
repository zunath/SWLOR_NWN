using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class IntuitionImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            IntuitionImplant1();
            IntuitionImplant2();
            IntuitionImplant3();
            IntuitionImplant4();
            IntuitionImplant5();

            return _builder.Build();
        }

        private void IntuitionImplant1()
        {
            _builder.Create("h_imp_inu1")
                .Name("Intuition")
                .Description("+1 WIS, -1 CON")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Wisdom, 1)
                .ModifyAbilityScore(AbilityType.Constitution, -1);
        }

        private void IntuitionImplant2()
        {
            _builder.Create("h_imp_inu2")
                .Name("Intuition")
                .Description("+2 WIS, -1 CON, -1 STR")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Wisdom, 2)
                .ModifyAbilityScore(AbilityType.Constitution, -1)
                .ModifyAbilityScore(AbilityType.Strength, -1);
        }
        private void IntuitionImplant3()
        {
            _builder.Create("h_imp_inu3")
                .Name("Intuition")
                .Description("+3 WIS, -2 CON, -2 STR")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Wisdom, 3)
                .ModifyAbilityScore(AbilityType.Constitution, -2)
                .ModifyAbilityScore(AbilityType.Strength, -2);
        }
        private void IntuitionImplant4()
        {
            _builder.Create("h_imp_inu4")
                .Name("Intuition")
                .Description("+4 WIS, -3 CON, -3 STR")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Wisdom, 4)
                .ModifyAbilityScore(AbilityType.Constitution, -3)
                .ModifyAbilityScore(AbilityType.Strength, -3);
        }
        private void IntuitionImplant5()
        {
            _builder.Create("h_imp_inu5")
                .Name("Intuition")
                .Description("+5 WIS, -4 CON, -4 STR")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Wisdom, 5)
                .ModifyAbilityScore(AbilityType.Constitution, -4)
                .ModifyAbilityScore(AbilityType.Strength, -4);
        }
    }
}
