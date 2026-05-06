using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ArmorPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Provoke();
            CloakProficiency();
            BeltProficiency();
            RingProficiency();
            NecklaceProficiency();
            BreastplateProficiency();
            HelmetProficiency();
            BracerProficiency();
            LeggingProficiency();
            HeavyShieldProficiency();
            Endure();
            TunicProficiency();
            CapProficiency();
            GloveProficiency();
            BootProficiency();

            return _builder.Build();
        }


        private void UnequipItemIfRequirementsNotMet(uint player, InventorySlot slot)
        {
            var item = GetItemInSlot(slot, player);
            if (!GetIsObjectValid(item))
                return;

            if (!Item.CanCreatureUseItem(player, item))
            {
                AssignCommand(player, () =>
                {
                    ActionUnequipItem(item);
                });
            }
        }


        private void Provoke()
        {
            _builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Provoke)
                .Name("Provoke")

                .AddPerkLevel()
                .Description("Goads a single target into attacking you.")
                .Price(2)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.Provoke1)

                .AddPerkLevel()
                .Description("Goads all enemies within range into attacking you.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.Provoke2);
        }


        private void CloakProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorGeneral, PerkType.CloakProficiency)
                .Name("Cloak Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Cloak);
                });
        }


        private void BeltProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorGeneral, PerkType.BeltProficiency)
                .Name("Belt Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Belt);
                });
        }


        private void RingProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorGeneral, PerkType.RingProficiency)
                .Name("Ring Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.LeftRing);
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.RightRing);
                });
        }


        private void NecklaceProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorGeneral, PerkType.NecklaceProficiency)
                .Name("Necklace Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Neck);
                });
        }


        private void BreastplateProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorHeavy, PerkType.BreastplateProficiency)
                .Name("Breastplate Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Breastplates")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Chest);
                });
        }


        private void HelmetProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorHeavy, PerkType.HelmetProficiency)
                .Name("Helmet Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Helmets")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Head);
                });
        }


        private void BracerProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorHeavy, PerkType.BracerProficiency)
                .Name("Bracer Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Bracers")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Arms);
                });
        }


        private void LeggingProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorHeavy, PerkType.LeggingProficiency)
                .Name("Legging Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Leggings")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Boots);
                });
        }


        private void HeavyShieldProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorHeavy, PerkType.ShieldProficiency)
                .Name("Shield Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Shields")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.LeftHand);
                });
        }


        private void Endure()
        {
            _builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Endure)
                .Name("Endure")

                .AddPerkLevel()
                .Description("1% chance per MGT mod to prevent melee damage dealt from an enemy once per round. Must be wearing full heavy armor. (Max: 10%)")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 15)

                .AddPerkLevel()
                .Description("2% chance per MGT mod to prevent melee damage dealt from an enemy once per round. Must be wearing full heavy armor. (Max: 20%)")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("3% chance per MGT mod to prevent melee damage dealt from an enemy once per round. Must be wearing full heavy armor. (Max: 30%)")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 50);
        }


        private void TunicProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorLight, PerkType.TunicProficiency)
                .Name("Tunic Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Tunics")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Chest);
                });
        }


        private void CapProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorLight, PerkType.CapProficiency)
                .Name("Cap Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Caps")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Head);
                });
        }


        private void GloveProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorLight, PerkType.GloveProficiency)
                .Name("Glove Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Gloves")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Arms);
                });
        }


        private void BootProficiency()
        {
            _builder.Create(PerkCategoryType.ArmorLight, PerkType.BootProficiency)
                .Name("Boot Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Boots")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)

                .TriggerRefund(player =>
                {
                    UnequipItemIfRequirementsNotMet(player, InventorySlot.Boots);
                });
        }
    }
}
