using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class DevicesPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            FragGrenade();
            ConcussionGrenade();
            IonGrenade();
            AdhesiveGrenade();
            Flamethrower();
            WristRocket();
            DeflectorShield();

            return _builder.Build();
        }



        private void FragGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.FragGrenade)
                .Name("Frag Grenade")

                .AddPerkLevel()
                .Description("Deals fire DMG equal to your Perception Score to all creatures within range of explosion. Consumes explosives on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade1)

                .AddPerkLevel()
                .Description("Deals 20 fire DMG, scaling with your Perception Score, to all creatures within range of explosion. Also inflicts Bleeding. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade2)

                .AddPerkLevel()
                .Description("Deals 40 fire DMG, scaling with your Perception Score, to all creatures within range of explosion. Also inflicts Bleeding. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade3);
        }


        private void ConcussionGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade")

                .AddPerkLevel()
                .Description("Deals electrical DMG equal to your Perception Score to all creatures within range of explosion. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.ConcussionGrenade1)

                .AddPerkLevel()
                .Description("Deals 15 electrical DMG, scaling with your Perception Score, to all creatures within range of explosion. Also inflicts Knockdown for 3 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade2)

                .AddPerkLevel()
                .Description("Deals 30 electrical DMG, scaling with your Perception Score, to all creatures within range of explosion. Also inflicts Knockdown for 3 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade3);
        }



        private void IonGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.IonGrenade)
                .Name("Ion Grenade")

                .AddPerkLevel()
                .Description("Deals electrical DMG equal to your Perception Score to all enemies within range of explosion. Deals bonus damage to droids. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.IonGrenade1)

                .AddPerkLevel()
                .Description("Deals 8 electrical DMG, scaling with your Perception Score, to all enemies within range of explosion. Deals bonus damage to droids. Also inflicts Stun on droids for 6 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IonGrenade2);
        }



        private void AdhesiveGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade")

                .AddPerkLevel()
                .Description("Inflicts slow on all enemies within range of explosion for 4 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.AdhesiveGrenade1)

                .AddPerkLevel()
                .Description("Immobilizes all enemies within range of explosion for 6 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdhesiveGrenade2);
        }



        private void Flamethrower()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.Flamethrower)
                .Name("Flamethrower")

                .AddPerkLevel()
                .Description("Deals fire DMG equal to your Perception Score to all targets within a cone in front of the user.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Flamethrower1)

                .AddPerkLevel()
                .Description("Deals 20 fire DMG, scaling with your Perception Score, to all targets within a cone in front of the user. Also inflicts Burning.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower2)

                .AddPerkLevel()
                .Description("Deals 40 fire DMG, scaling with your Perception Score, to all targets within a cone in front of the user. Also inflicts Burning.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower3);
        }


        private void WristRocket()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.WristRocket)
                .Name("Wrist Rocket")

                .AddPerkLevel()
                .Description("Inflicts fire DMG equal to your Perception Score to a single target.")
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.WristRocket1)

                .AddPerkLevel()
                .Description("Inflicts 25 fire DMG, scaling with your Perception Score, to a single target. Also inflicts Knockdown for 3 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket2)

                .AddPerkLevel()
                .Description("Inflicts 50 fire DMG, scaling with your Perception Score, to a single target. Also inflicts Knockdown for 3 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket3);
        }


        private void DeflectorShield()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.DeflectorShield)
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
