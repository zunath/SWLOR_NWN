using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class CharismaticImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            CharismaticImplant1();
            CharismaticImplant2();
            CharismaticImplant3();
            CharismaticImplant4();
            CharismaticImplant5();

            return _builder.Build();
        }

        private void CharismaticImplant1()
        {
            _builder.Create("h_imp_cha1")
                .Name("Charismatic")
                .Description("+1 CHA, -1 INT")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Charisma, 1)
                .ModifyAbilityScore(AbilityType.Intelligence, -1);
        }

        private void CharismaticImplant2()
        {
            _builder.Create("h_imp_cha2")
                .Name("Charismatic")
                .Description("+2 CHA, -1 DEX, -1 INT")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Charisma, 2)
                .ModifyAbilityScore(AbilityType.Dexterity, -1)
                .ModifyAbilityScore(AbilityType.Intelligence, -1);
        }
        private void CharismaticImplant3()
        {
            _builder.Create("h_imp_cha3")
                .Name("Charismatic")
                .Description("+3 CHA, -2 DEX, -2 INT")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Charisma, 3)
                .ModifyAbilityScore(AbilityType.Dexterity, -2)
                .ModifyAbilityScore(AbilityType.Intelligence, -2);
        }
        private void CharismaticImplant4()
        {
            _builder.Create("h_imp_cha4")
                .Name("Charismatic")
                .Description("+4 CHA, -3 DEX, -3 INT")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Charisma, 4)
                .ModifyAbilityScore(AbilityType.Dexterity, -3)
                .ModifyAbilityScore(AbilityType.Intelligence, -3);
        }
        private void CharismaticImplant5()
        {
            _builder.Create("h_imp_cha5")
                .Name("Charismatic")
                .Description("+5 CHA, -4 DEX, -4 INT")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Head)
                .ModifyAbilityScore(AbilityType.Charisma, 5)
                .ModifyAbilityScore(AbilityType.Dexterity, -4)
                .ModifyAbilityScore(AbilityType.Intelligence, -4);
        }
    }
}
