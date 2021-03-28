using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class DexterousImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            DexterousImplant1();
            DexterousImplant2();
            DexterousImplant3();
            DexterousImplant4();
            DexterousImplant5();

            return _builder.Build();
        }


        private void DexterousImplant1()
        {
            _builder.Create("h_imp_dex1")
                .Name("Dexterous")
                .Description("+1 DEX, -1 INT")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Legs)
                .ModifyAbilityScore(AbilityType.Dexterity, 1)
                .ModifyAbilityScore(AbilityType.Intelligence, -1);
        }

        private void DexterousImplant2()
        {
            _builder.Create("h_imp_dex2")
                .Name("Dexterous")
                .Description("+2 DEX, -1 WIS, -1 INT")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Legs)
                .ModifyAbilityScore(AbilityType.Dexterity, 2)
                .ModifyAbilityScore(AbilityType.Intelligence, -1)
                .ModifyAbilityScore(AbilityType.Wisdom, -1);
        }
        private void DexterousImplant3()
        {
            _builder.Create("h_imp_dex3")
                .Name("Dexterous")
                .Description("+3 DEX, -2 WIS, -2 INT")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Legs)
                .ModifyAbilityScore(AbilityType.Dexterity, 3)
                .ModifyAbilityScore(AbilityType.Intelligence, -2)
                .ModifyAbilityScore(AbilityType.Wisdom, -2);
        }
        private void DexterousImplant4()
        {
            _builder.Create("h_imp_dex4")
                .Name("Dexterous")
                .Description("+4 DEX, -3 WIS, -3 INT")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Legs)
                .ModifyAbilityScore(AbilityType.Dexterity, 4)
                .ModifyAbilityScore(AbilityType.Intelligence, -3)
                .ModifyAbilityScore(AbilityType.Wisdom, -3);
        }
        private void DexterousImplant5()
        {
            _builder.Create("h_imp_dex5")
                .Name("Dexterous")
                .Description("+5 DEX, -4 WIS, -4 INT")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Legs)
                .ModifyAbilityScore(AbilityType.Dexterity, 5)
                .ModifyAbilityScore(AbilityType.Intelligence, -4)
                .ModifyAbilityScore(AbilityType.Wisdom, -4);
        }
    }
}
