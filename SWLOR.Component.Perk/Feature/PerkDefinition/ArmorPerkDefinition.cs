using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;

namespace SWLOR.Component.Perk.Feature.PerkDefinition
{
    public class ArmorPerkDefinition : IPerkListDefinition
    {
                private readonly IPerkService _perkService;

        public ArmorPerkDefinition(IPerkService perkService)
        {
            _perkService = perkService;
        }

        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            Provoke(builder);
            CloakProficiency(builder);
            BeltProficiency(builder);
            RingProficiency(builder);
            NecklaceProficiency(builder);
            BreastplateProficiency(builder);
            HelmetProficiency(builder);
            BracerProficiency(builder);
            LeggingProficiency(builder);
            HeavyShieldProficiency(builder);
            TunicProficiency(builder);
            CapProficiency(builder);
            GloveProficiency(builder);
            BootProficiency(builder);

            return builder.Build();
        }

        private void Provoke(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Provoke)
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

        private void UnequipArmorIfRequirementsNotMet(uint player, PerkType perkType, InventorySlot slot)
        {
            var item = GetItemInSlot(slot, player);
            if (!GetIsObjectValid(item))
                return;

            var perkLevel = _perkService.GetPerkLevel(player, perkType);

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var ipType = GetItemPropertyType(ip);
                if (ipType != ItemPropertyType.UseLimitationPerk)
                    continue;

                var requiredPerkType = (PerkType)GetItemPropertySubType(ip);
                if (requiredPerkType != perkType)
                    continue;

                var requiredLevel = GetItemPropertyCostTableValue(ip);
                if (perkLevel < requiredLevel)
                {
                    AssignCommand(player, () =>
                    {
                        ActionUnequipItem(item);
                    });
                }
            }
        }

        private void CloakProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.CloakProficiency)
                .Name("Cloak Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.CloakProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.CloakProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.CloakProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.CloakProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.CloakProficiency5)
                
                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.CloakProficiency, InventorySlot.Cloak);
                });
        }

        private void BeltProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.BeltProficiency)
                .Name("Belt Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.BeltProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.BeltProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.BeltProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.BeltProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.BeltProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.BeltProficiency, InventorySlot.Belt);
                });
        }

        private void RingProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.RingProficiency)
                .Name("Ring Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.RingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.RingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.RingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.RingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.RingProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.RingProficiency, InventorySlot.LeftRing);
                    UnequipArmorIfRequirementsNotMet(player, PerkType.RingProficiency, InventorySlot.RightRing);
                });
        }

        private void NecklaceProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.NecklaceProficiency)
                .Name("Necklace Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.NecklaceProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.NecklaceProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.NecklaceProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.NecklaceProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.NecklaceProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.NecklaceProficiency, InventorySlot.Neck);
                });
        }

        private void BreastplateProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.BreastplateProficiency)
                .Name("Breastplate Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Breastplates")
                .Price(1)
                .GrantsFeat(FeatType.BreastplateProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BreastplateProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BreastplateProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BreastplateProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BreastplateProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.BreastplateProficiency, InventorySlot.Chest);
                });
        }

        private void HelmetProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.HelmetProficiency)
                .Name("Helmet Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Helmets")
                .Price(1)
                .GrantsFeat(FeatType.HelmetProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.HelmetProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.HelmetProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.HelmetProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.HelmetProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.HelmetProficiency, InventorySlot.Head);
                });
        }

        private void BracerProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.BracerProficiency)
                .Name("Bracer Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Bracers")
                .Price(1)
                .GrantsFeat(FeatType.BracerProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BracerProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BracerProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BracerProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BracerProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.BracerProficiency, InventorySlot.Arms);
                });
        }

        private void LeggingProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.LeggingProficiency)
                .Name("Legging Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Leggings")
                .Price(1)
                .GrantsFeat(FeatType.LeggingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.LeggingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.LeggingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.LeggingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.LeggingProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.LeggingProficiency, InventorySlot.Boots);
                });
        }

        private void HeavyShieldProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.ShieldProficiency)
                .Name("Shield Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Shields")
                .Price(1)
                .GrantsFeat(FeatType.ShieldProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.ShieldProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.ShieldProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.ShieldProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.ShieldProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.ShieldProficiency, InventorySlot.LeftHand);
                });
        }

        private void TunicProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.TunicProficiency)
                .Name("Tunic Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Tunics")
                .Price(1)
                .GrantsFeat(FeatType.TunicProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.TunicProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.TunicProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.TunicProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.TunicProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.TunicProficiency, InventorySlot.Chest);
                });
        }

        private void CapProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.CapProficiency)
                .Name("Cap Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Caps")
                .Price(1)
                .GrantsFeat(FeatType.CapProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.CapProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.CapProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.CapProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.CapProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.CapProficiency, InventorySlot.Head);
                });
        }

        private void GloveProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.GloveProficiency)
                .Name("Glove Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Gloves")
                .Price(1)
                .GrantsFeat(FeatType.GloveProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.GloveProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.GloveProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.GloveProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.GloveProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.GloveProficiency, InventorySlot.Arms);
                });
        }

        private void BootProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.BootProficiency)
                .Name("Boot Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Boots")
                .Price(1)
                .GrantsFeat(FeatType.BootProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BootProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BootProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BootProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BootProficiency5)

                .TriggerRefund(player =>
                {
                    UnequipArmorIfRequirementsNotMet(player, PerkType.BootProficiency, InventorySlot.Boots);
                });
        }
    }

}
