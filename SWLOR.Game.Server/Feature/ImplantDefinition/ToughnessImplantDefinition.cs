using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class ToughnessImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            ToughnessImplant1();
            ToughnessImplant2();
            ToughnessImplant3();
            ToughnessImplant4();
            ToughnessImplant5();

            return _builder.Build();
        }

        private void ToughnessImplant1()
        {
            _builder.Create("b_imp_tgh1")
                .Name("Toughness")
                .Description("+1 CON, -1 WIS")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Body)
                .ModifyAbilityScore(AbilityType.Constitution, 1)
                .ModifyAbilityScore(AbilityType.Wisdom, -1);
        }

        private void ToughnessImplant2()
        {
            _builder.Create("b_imp_tgh2")
                .Name("Toughness")
                .Description("+2 CON, -1 INT, -1 WIS")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Body)
                .ModifyAbilityScore(AbilityType.Constitution, 2)
                .ModifyAbilityScore(AbilityType.Intelligence, -1)
                .ModifyAbilityScore(AbilityType.Wisdom, -1);
        }
        private void ToughnessImplant3()
        {
            _builder.Create("b_imp_tgh3")
                .Name("Toughness")
                .Description("+3 CHA, -2 INT, -2 WIS")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Body)
                .ModifyAbilityScore(AbilityType.Constitution, 3)
                .ModifyAbilityScore(AbilityType.Intelligence, -2)
                .ModifyAbilityScore(AbilityType.Wisdom, -2);
        }
        private void ToughnessImplant4()
        {
            _builder.Create("b_imp_tgh4")
                .Name("Toughness")
                .Description("+4 CHA, -3 INT, -3 WIS")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Body)
                .ModifyAbilityScore(AbilityType.Constitution, 4)
                .ModifyAbilityScore(AbilityType.Intelligence, -3)
                .ModifyAbilityScore(AbilityType.Wisdom, -3);
        }
        private void ToughnessImplant5()
        {
            _builder.Create("b_imp_tgh5")
                .Name("Toughness")
                .Description("+5 CHA, -4 INT, -4 WIS")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Body)
                .ModifyAbilityScore(AbilityType.Constitution, 5)
                .ModifyAbilityScore(AbilityType.Intelligence, -4)
                .ModifyAbilityScore(AbilityType.Wisdom, -4);
        }
    }
}
