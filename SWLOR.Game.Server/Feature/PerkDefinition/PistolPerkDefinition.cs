using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class PistolPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DeadMansHand();
            DeadeyeReload();
            DisarmingShot();
            DuelistsDistance();
            EvasiveReload();
            FanTheHammer();
            GunfighterStance();
            GunslingerFocus();
            HighNoon();
            InterruptingShot();
            KitingInstinct();
            LastWord();
            LowShot();
            MobileFootwork();
            PointBlankBurst();
            ReloadTempo();
            RicochetShot();
            SkirmishersNerve();
            SkirmisherStance();
            SmokeRound();
            SnapRoll();

            return _builder.Build();
        }

        private void DeadMansHand()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.DeadMansHand)
                .Name("Dead Man's Hand")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DeadMansHand1)
                .Description("Fire six shots at your target and nearby enemies, each for weapon DMG + 10. Your target is prioritized and secondary targets cannot be hit more than twice.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 50);
        }

        private void DeadeyeReload()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.DeadeyeReload)
                .Name("Deadeye Reload")

                .AddPerkLevel()
                .Description("After using a pistol combat ability, your next auto-attack within 6 seconds deals +10 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 22);
        }

        private void DisarmingShot()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.DisarmingShot)
                .Name("Disarming Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisarmingShot1)
                .Description("Deals weapon DMG + 8 and has a Reflex DC12 check to inflict Weakened, reducing Attack by 10% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisarmingShot2)
                .Description("Deals weapon DMG + 18 and has a Reflex DC15 check to inflict Weakened, reducing Attack by 15% for 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisarmingShot3)
                .Description("Deals weapon DMG + 32 and has a Reflex DC18 check to inflict Weakened, reducing Attack by 20% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 42);
        }

        private void DuelistsDistance()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.DuelistsDistance)
                .Name("Duelist's Distance")

                .AddPerkLevel()
                .Description("Deal +12% pistol damage to enemies within 8 meters that are not targeting you.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 40);
        }

        private void EvasiveReload()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.EvasiveReload)
                .Name("Evasive Reload")

                .AddPerkLevel()
                .Description("Using Snap Roll or Ricochet Shot reduces Disarming Shot cooldowns by 10 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 32);
        }

        private void FanTheHammer()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.FanTheHammer)
                .Name("Fan the Hammer")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FanTheHammer1)
                .Description("Fires at up to 3 enemies in a cone for weapon DMG + 12 each.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FanTheHammer2)
                .Description("Fires at up to 5 enemies in a cone for weapon DMG + 20 each.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 35);
        }

        private void GunfighterStance()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.GunfighterStance)
                .Name("Gunfighter Stance")

                .AddPerkLevel()
                .Description("While active, grants +15% Attack and +10% Haste, but reduces Defense by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 15);
        }

        private void GunslingerFocus()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.GunslingerFocus)
                .Name("Gunslinger Focus")

                .AddPerkLevel()
                .Description("For 20 seconds, Quick Draw and Double Shot abilities cost 2 less STM and deal +10 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 45);
        }

        private void HighNoon()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.HighNoon)
                .Name("High Noon")

                .AddPerkLevel()
                .Description("Your first pistol attack after entering combat gains +30% critical chance and deals +20 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 38);
        }

        private void InterruptingShot()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.InterruptingShot)
                .Name("Interrupting Shot")

                .AddPerkLevel()
                .Description("Interrupts your target's ability activation and has a Will DC12 check to inflict Foggy Mind for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 18)

                .AddPerkLevel()
                .Description("Deals weapon DMG + 20, interrupts your target's ability activation, and has a Will DC16 check to inflict Foggy Mind for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 35);
        }

        private void KitingInstinct()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.KitingInstinct)
                .Name("Kiting Instinct")

                .AddPerkLevel()
                .Description("When attacked in melee, you have a 20% chance to restore 3 STM and gain +10% Evasion for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 20);
        }

        private void LastWord()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.LastWord)
                .Name("Last Word")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LastWord1)
                .Description("Interrupts all enemies in a cone, deals weapon DMG + 35, and has a Reflex DC18 check to inflict Dazed for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 50);
        }

        private void LowShot()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.LowShot)
                .Name("Low Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LowShot1)
                .Description("Deals weapon DMG + 20 and has a Reflex DC16 check to inflict Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 30);
        }

        private void MobileFootwork()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.MobileFootwork)
                .Name("Mobile Footwork")

                .AddPerkLevel()
                .Description("After using a pistol ability, gain +10% Evasion for 6 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 5);
        }

        private void PointBlankBurst()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.PointBlankBurst)
                .Name("Point Blank Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PointBlankBurst1)
                .Description("Deals weapon DMG + 18 to enemies in a cone. Reflex DC16 check to inflict Knockdown for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 38);
        }

        private void ReloadTempo()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.ReloadTempo)
                .Name("Reload Tempo")

                .AddPerkLevel()
                .Description("Defeating an enemy restores 10 STM and reduces Quick Draw cooldowns by 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40);
        }

        private void RicochetShot()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.RicochetShot)
                .Name("Ricochet Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RicochetShot1)
                .Description("A shot bounces to up to 3 enemies for weapon DMG + 12 each. Each target has a Reflex DC14 check to avoid Blind for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25);
        }

        private void SkirmishersNerve()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.SkirmishersNerve)
                .Name("Skirmisher's Nerve")

                .AddPerkLevel()
                .Description("When reduced below 40% HP, your next pistol ability costs 0 STM and grants +20% Evasion for 8 seconds. This can only trigger once every 2 minutes.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 48);
        }

        private void SkirmisherStance()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.SkirmisherStance)
                .Name("Skirmisher Stance")

                .AddPerkLevel()
                .Description("While active, grants +15% Evasion and reduces Enmity generation by 20%, but reduces Attack by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 15);
        }

        private void SmokeRound()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.SmokeRound)
                .Name("Smoke Round")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SmokeRound1)
                .Description("Enemies in the target area have a Fortitude DC16 check to avoid Blind for 12 seconds. You reduce enmity against affected enemies.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 45);
        }

        private void SnapRoll()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.SnapRoll)
                .Name("Snap Roll")

                .AddPerkLevel()
                .Description("Gain +25% Evasion for 6 seconds and reduce your current enmity by 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 12)

                .AddPerkLevel()
                .Description("Gain +35% Evasion for 8 seconds and your next pistol attack within 8 seconds deals +10 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 28);
        }
    }
}
