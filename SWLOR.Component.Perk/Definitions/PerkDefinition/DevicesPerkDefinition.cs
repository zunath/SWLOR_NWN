using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Definitions.PerkDefinition
{
    public class DevicesPerkDefinition: IPerkListDefinition
    {
                public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            DemolitionExpert(builder);
            FragGrenade(builder);
            ConcussionGrenade(builder);
            FlashbangGrenade(builder);
            IonGrenade(builder);
            KoltoGrenade(builder);
            AdhesiveGrenade(builder);
            SmokeBomb(builder);
            KoltoBomb(builder);
            IncendiaryBomb(builder);
            GasBomb(builder);
            StealthGenerator(builder);
            Flamethrower(builder);
            WristRocket(builder);
            DeflectorShield(builder);

            return builder.Build();
        }

        private void DemolitionExpert(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.DemolitionExpert)
                .Name("Demolition Expert")

                .AddPerkLevel()
                .Description("10% chance to use a Devices ability without consuming explosives.")
                .Price(1)
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DemolitionExpert1)

                .AddPerkLevel()
                .Description("20% chance to use a Devices ability without consuming explosives.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DemolitionExpert2)

                .AddPerkLevel()
                .Description("30% chance to use a Devices ability without consuming explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DemolitionExpert3);
        }

        private void FragGrenade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.FragGrenade)
                .Name("Frag Grenade")

                .AddPerkLevel()
                .Description("Deals fire DMG equal to your Perception Score to all creatures within range of explosion. Consumes explosives on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade1)

                .AddPerkLevel()
                .Description("Deals 20 fire DMG, scaling with your Perception Score, to all creatures within range of explosion. Also has an 8DC reflex check to inflict Bleeding. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade2)

                .AddPerkLevel()
                .Description("Deals 40 fire DMG, scaling with your Perception Score, to all creatures within range of explosion. Also has a 12DC reflex check to inflict Bleeding. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade3);
        }

        private void ConcussionGrenade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade")

                .AddPerkLevel()
                .Description("Deals electrical DMG equal to your Perception Score to all creatures within range of explosion. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.ConcussionGrenade1)

                .AddPerkLevel()
                .Description("Deals 15 electrical DMG, scaling with your Perception Score, to all creatures within range of explosion. Also has an 8DC reflex check to inflict Knockdown for 3 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade2)

                .AddPerkLevel()
                .Description("Deals 30 electrical DMG, scaling with your Perception Score, to all creatures within range of explosion. Also has a 12DC reflex check to inflict Knockdown for 3 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade3);
        }

        private void FlashbangGrenade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade")

                .AddPerkLevel()
                .Description("Reduces Accuracy by 10 on all enemies within range of explosion for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.FlashbangGrenade1)

                .AddPerkLevel()
                .Description("Reduces Accuracy by 20 on all enemies within range of explosion for 20 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FlashbangGrenade2)

                .AddPerkLevel()
                .Description("Reduces Accuracy by 30 on all enemies within range of explosion for 20 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FlashbangGrenade3);
        }

        private void IonGrenade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.IonGrenade)
                .Name("Ion Grenade")

                .AddPerkLevel()
                .Description("Deals electrical DMG equal to your Perception Score to all enemies within range of explosion. Deals bonus damage to droids. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.IonGrenade1)

                .AddPerkLevel()
                .Description("Deals 8 electrical DMG, scaling with your Perception Score, to all enemies within range of explosion. Deals bonus damage to droids. Also has a 10DC fortitude check to inflict stun to droids for 6 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IonGrenade2)

                .AddPerkLevel()
                .Description("Deals 14 electrical DMG, scaling with your Perception Score, to all enemies within range of explosion. Deals bonus damage to droids. Also has a 14DC fortitude check to inflict stun to droids for 6 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IonGrenade3);
        }

        private void KoltoGrenade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.KoltoGrenade)
                .Name("Kolto Grenade")

                .AddPerkLevel()
                .Description("Grants 6 HP regeneration to all party members within range of explosion for 45 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 4)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.KoltoGrenade1)

                .AddPerkLevel()
                .Description("Grants 14 HP regeneration to all party members within range of explosion for 45 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoGrenade2)

                .AddPerkLevel()
                .Description("Grants 24 HP regeneration to all party members within range of explosion for 45 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoGrenade3);
        }

        private void AdhesiveGrenade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade")

                .AddPerkLevel()
                .Description("Inflicts slow on all enemies within range of explosion for 4 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.AdhesiveGrenade1)

                .AddPerkLevel()
                .Description("Inflicts slow on all enemies within range of explosion for 6 seconds. Also has an 8DC fortitude check to inflict immobilize instead. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdhesiveGrenade2)

                .AddPerkLevel()
                .Description("Inflicts slow on all enemies within range of explosion for 8 seconds. Also has a 12DC fortitude check to inflict immobilize instead. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdhesiveGrenade3);
        }

        private void SmokeBomb(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.SmokeBomb)
                .Name("Smoke Bomb")

                .AddPerkLevel()
                .Description("Creates a smokescreen at the explosion site, granting invisibility to all creatures who enter the area of effect for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.SmokeBomb1)

                .AddPerkLevel()
                .Description("Creates a smokescreen at the explosion site, granting invisibility to all creatures who enter the area of effect for 40 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SmokeBomb2)

                .AddPerkLevel()
                .Description("Creates a smokescreen at the explosion site, granting invisibility to all creatures who enter the area of effect for 60 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SmokeBomb3);
        }

        private void KoltoBomb(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.KoltoBomb)
                .Name("Kolto Bomb")

                .AddPerkLevel()
                .Description("Creates a Kolto field at the explosion site, granting 4 HP regeneration to all creatures who enter the area of effect for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.KoltoBomb1)

                .AddPerkLevel()
                .Description("Creates a Kolto field at the explosion site, granting 12 HP regeneration to all creatures who enter the area of effect for 40 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 24)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoBomb2)

                .AddPerkLevel()
                .Description("Creates a Kolto field at the explosion site, granting 20 HP regeneration to all creatures who enter the area of effect for 60 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 44)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoBomb3);
        }

        private void IncendiaryBomb(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb")

                .AddPerkLevel()
                .Description("Creates a fire field at the explosion site, dealing 4 fire DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 13)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.IncendiaryBomb1)

                .AddPerkLevel()
                .Description("Creates a fire field at the explosion site, dealing 10 fire DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 40 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 33)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IncendiaryBomb2)

                .AddPerkLevel()
                .Description("Creates a fire field at the explosion site, dealing 16 fire DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 60 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 43)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IncendiaryBomb3);
        }

        private void GasBomb(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.GasBomb)
                .Name("Gas Bomb")

                .AddPerkLevel()
                .Description("Creates a poison field at the explosion site, dealing 4 poison DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 18 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 16)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.GasBomb1)

                .AddPerkLevel()
                .Description("Creates a poison field at the explosion site, dealing 12 poison DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 30 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 34)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.GasBomb2)

                .AddPerkLevel()
                .Description("Creates a poison field at the explosion site, dealing 16 poison DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 48 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 46)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.GasBomb3);
        }

        private void StealthGenerator(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.StealthGenerator)
                .Name("Stealth Generator")

                .AddPerkLevel()
                .Description("Grants invisibility to the user for 30 seconds.")
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.StealthGenerator1)

                .AddPerkLevel()
                .Description("Grants invisibility to the user for 60 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StealthGenerator2)

                .AddPerkLevel()
                .Description("Grants invisibility to the user for 2 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StealthGenerator3);
        }

        private void Flamethrower(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.Flamethrower)
                .Name("Flamethrower")

                .AddPerkLevel()
                .Description("Deals fire DMG equal to your Perception Score to all targets within a cone in front of the user.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Flamethrower1)

                .AddPerkLevel()
                .Description("Deals 20 fire DMG, scaling with your Perception Score, to all targets within a cone in front of the user. Also has an 8DC reflex check to inflict Burning.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower2)

                .AddPerkLevel()
                .Description("Deals 40 fire DMG, scaling with your Perception Score, to all targets within a cone in front of the user. Also has a 12DC reflex check to inflict Burning.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower3);
        }

        private void WristRocket(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.WristRocket)
                .Name("Wrist Rocket")

                .AddPerkLevel()
                .Description("Inflicts fire DMG equal to your Perception Score to a single target.")
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.WristRocket1)

                .AddPerkLevel()
                .Description("Inflicts 25 fire DMG, scaling with your Perception Score, to a single target. Also has an 8DC fortitude check to inflict Knockdown for 3 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket2)

                .AddPerkLevel()
                .Description("Inflicts 50 fire DMG, scaling with your Perception Score, to a single target. Also has a 12DC fortitude check to inflict Knockdown for 3 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket3);
        }

        private void DeflectorShield(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Devices, PerkType.DeflectorShield)
                .Name("Deflector Shield")

                .AddPerkLevel()
                .Description("Grants temporary hit points to the user for a short period of time.")
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.DeflectorShield1)

                .AddPerkLevel()
                .Description("Grants temporary hit points to the user for a short period of time.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DeflectorShield2)

                .AddPerkLevel()
                .Description("Grants temporary hit points to the user and all nearby party members for a short period of time.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DeflectorShield3);
        }
    }
}
