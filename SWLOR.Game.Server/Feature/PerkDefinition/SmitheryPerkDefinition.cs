using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class SmitheryPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Synthesis();
            Touch();
            Abilities();
            WeaponBlueprints();
            ArmorBlueprints();
            AccessoryBlueprints();
            SmitheryEquipment();

            return _builder.Build();
        }


        private void Synthesis()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.RapidSynthesisSmithery)
                .Name("Rapid Synthesis (Smithery)")

                .AddPerkLevel()
                .Description("Increases progress by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10);


            _builder.Create(PerkCategoryType.Smithery, PerkType.CarefulSynthesisSmithery)
                .Name("Careful Synthesis (Smithery)")

                .AddPerkLevel()
                .Description("Increases progress by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 30);
        }


        private void Touch()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.BasicTouchSmithery)
                .Name("Basic Touch (Smithery)")

                .AddPerkLevel()
                .Description("Increases quality by 10. (90% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 5);

            _builder.Create(PerkCategoryType.Smithery, PerkType.StandardTouchSmithery)
                .Name("Standard Touch (Smithery)")

                .AddPerkLevel()
                .Description("Increases quality by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 15);

            _builder.Create(PerkCategoryType.Smithery, PerkType.PreciseTouchSmithery)
                .Name("Precise Touch (Smithery)")

                .AddPerkLevel()
                .Description("Increases quality by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 35);
        }


        private void Abilities()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.MastersMendSmithery)
                .Name("Master's Mend (Smithery)")

                .AddPerkLevel()
                .Description("Restores item durability by 30.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10);

            _builder.Create(PerkCategoryType.Smithery, PerkType.SteadyHandSmithery)
                .Name("Steady Hand (Smithery)")

                .AddPerkLevel()
                .Description("Increases success rate of next synthesis ability to 100%. Passively grants +21 synthesis progress per successful synthesis while crafting.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 20);

            _builder.Create(PerkCategoryType.Smithery, PerkType.MuscleMemorySmithery)
                .Name("Muscle Memory (Smithery)")

                .AddPerkLevel()
                .Description("Increases success rate of next touch ability to 100%. Passively grants +115 quality per successful touch while crafting.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 40);

            _builder.Create(PerkCategoryType.Smithery, PerkType.VenerationSmithery)
                .Name("Veneration (Smithery)")

                .AddPerkLevel()
                .Description("Reduces CP cost of synthesis abilities by 50% for the next four actions. Passively grants +31 maximum CP while crafting.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 25);

            _builder.Create(PerkCategoryType.Smithery, PerkType.WasteNotSmithery)
                .Name("Waste Not (Smithery)")

                .AddPerkLevel()
                .Description("Reduces loss of durability by 50% for the next four actions.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 8);
        }

        private void WeaponBlueprints()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.WeaponBlueprints)
                .Name("Weapon Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 weapon blueprints.")
                .Price(2)
                .GrantsFeat(FeatType.WeaponBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 weapon blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.WeaponBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 weapon blueprints.")
                .Price(4)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.WeaponBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 weapon blueprints.")
                .Price(5)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.WeaponBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 weapon blueprints.")
                .Price(6)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.WeaponBlueprints5);
        }


        private void ArmorBlueprints()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.ArmorBlueprints)
                .Name("Armor Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Armor blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.ArmorBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Armor blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.ArmorBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Armor blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.ArmorBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.ArmorBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.ArmorBlueprints5);
        }


        private void AccessoryBlueprints()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.AccessoryBlueprints)
                .Name("Accessory Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Accessory blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.AccessoryBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Accessory blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.AccessoryBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Accessory blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.AccessoryBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Accessory blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.AccessoryBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Accessory blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.AccessoryBlueprints5);
        }


        private void SmitheryEquipment()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryEquipment)
                .Name("Smithery Equipment")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Smithery equipment.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 5)
                .GrantsFeat(FeatType.SmitheryEquipment1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Smithery equipment.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 15)
                .GrantsFeat(FeatType.SmitheryEquipment2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Smithery equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Smithery, 25)
                .GrantsFeat(FeatType.SmitheryEquipment3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Smithery equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Smithery, 35)
                .GrantsFeat(FeatType.SmitheryEquipment4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Smithery equipment.")
                .Price(5)
                .RequirementSkill(SkillType.Smithery, 45)
                .GrantsFeat(FeatType.SmitheryEquipment5);
        }
    }
}

