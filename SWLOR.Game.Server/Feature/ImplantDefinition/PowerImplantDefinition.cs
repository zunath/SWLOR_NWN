using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Feature.ImplantDefinition
{
    public class PowerImplantDefinition: IImplantListDefinition
    {
        private readonly ImplantBuilder _builder = new ImplantBuilder();

        public Dictionary<string, ImplantDetail> BuildImplants()
        {
            PowerImplant1();
            PowerImplant2();
            PowerImplant3();
            PowerImplant4();
            PowerImplant5();

            return _builder.Build();
        }

        private void PowerImplant1()
        {
            _builder.Create("a_imp_pow1")
                .Name("Power")
                .Description("+1 STR, -1 WIS")
                .RequiredLevel(1)
                .Slot(ImplantSlotType.Arms)
                .ModifyAbilityScore(AbilityType.Strength, 1)
                .ModifyAbilityScore(AbilityType.Wisdom, -1);
        }

        private void PowerImplant2()
        {
            _builder.Create("a_imp_pow2")
                .Name("Power")
                .Description("+2 STR, -1 INT, -1 WIS")
                .RequiredLevel(2)
                .Slot(ImplantSlotType.Arms)
                .ModifyAbilityScore(AbilityType.Strength, 2)
                .ModifyAbilityScore(AbilityType.Intelligence, -1)
                .ModifyAbilityScore(AbilityType.Wisdom, -1);
        }
        private void PowerImplant3()
        {
            _builder.Create("a_imp_pow3")
                .Name("Power")
                .Description("+3 STR, -2 INT, -2 WIS")
                .RequiredLevel(3)
                .Slot(ImplantSlotType.Arms)
                .ModifyAbilityScore(AbilityType.Strength, 3)
                .ModifyAbilityScore(AbilityType.Intelligence, -2)
                .ModifyAbilityScore(AbilityType.Wisdom, -2);
        }
        private void PowerImplant4()
        {
            _builder.Create("a_imp_pow4")
                .Name("Power")
                .Description("+4 STR, -3 INT, -3 WIS")
                .RequiredLevel(4)
                .Slot(ImplantSlotType.Arms)
                .ModifyAbilityScore(AbilityType.Strength, 4)
                .ModifyAbilityScore(AbilityType.Intelligence, -3)
                .ModifyAbilityScore(AbilityType.Wisdom, -3);
        }
        private void PowerImplant5()
        {
            _builder.Create("a_imp_pow5")
                .Name("Power")
                .Description("+5 STR, -4 INT, -4 WIS")
                .RequiredLevel(5)
                .Slot(ImplantSlotType.Arms)
                .ModifyAbilityScore(AbilityType.Strength, 5)
                .ModifyAbilityScore(AbilityType.Intelligence, -4)
                .ModifyAbilityScore(AbilityType.Wisdom, -4);
        }
    }
}
