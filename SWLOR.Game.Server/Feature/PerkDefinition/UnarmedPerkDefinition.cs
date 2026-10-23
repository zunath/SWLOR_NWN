using System.Collections.Generic;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class UnarmedPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Jab();
            FlowingForm();
            PalmStrike();
            PracticedForm();
            MeasuredTechnique();
            FinishingCombination();
            FlowStance();
            ControlledBreathing();
            RootedTechnique();
            PerfectForm();
            DirtyJab();
            DirtyFighting();
            Uppercut();
            HeavyBlows();
            HardKnocks();
            LowKick();
            BruiserStance();
            CatchYourBreath();
            Payback();
            Haymaker();

            return _builder.Build();
        }

        private void Jab()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.Jab)
                .Name("Jab")
                .Icon("ife_jab")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Jab1)
                .DroidAISlots(1)
                .Description("Deals weapon DMG + 8. On hit, grants Opening against this enemy for 4 seconds. Opening gives your weapon auto-attacks +3 Accuracy against this enemy with any weapon and sets up either follow-up attack.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueWeavePower, 4)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Jab2)
                .DroidAISlots(2)
                .Description("Deals weapon DMG + 18. On hit, grants Opening against this enemy for 4 seconds. Opening gives your weapon auto-attacks +3 Accuracy against this enemy with any weapon and sets up either follow-up attack.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueWeavePower, 8)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Jab3)
                .DroidAISlots(3)
                .Description("Deals weapon DMG + 28. On hit, grants Opening against this enemy for 4 seconds. Opening gives your weapon auto-attacks +3 Accuracy against this enemy with any weapon and sets up either follow-up attack.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueWeavePower, 12)
                .Price(3)
                .RequirementSkill(SkillType.Unarmed, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Jab4)
                .DroidAISlots(4)
                .Description("Deals weapon DMG + 38. On hit, grants Opening against this enemy for 4 seconds. Opening gives your weapon auto-attacks +3 Accuracy against this enemy with any weapon and sets up either follow-up attack.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueWeavePower, 16)
                .Price(5)
                .RequirementSkill(SkillType.Unarmed, 40);
        }

        private void FlowingForm()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.FlowingForm)
                .Name("Flowing Form")
                .Icon("ife_flowform")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlowingFormTrait)
                .Description("Extends Opening and Finish from 4 to 6 seconds. During Opening, your next weapon auto-attack against that enemy gains +4/8/12/16 DMG by Jab rank, with any weapon. One bonus per opener, consumed even on a miss.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueWindowExtensionSeconds, 2)
                .IncreasesStat(StatType.TechniqueWeaveEnabled, 1)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 5)
                .RequirementMustHavePerk(PerkType.Jab);
        }

        private void PalmStrike()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.PalmStrike)
                .Name("Palm Strike")
                .Icon("ife_palmstr")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PalmStrike1)
                .DroidAISlots(1)
                .Description("Deals weapon DMG + 12. Spends Opening at activation. If the sequenced strike lands, grants Finish for 4 seconds; Finish gives your weapon auto-attacks +3 Accuracy against this enemy with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 8)
                .RequirementMustHavePerk(PerkType.Jab)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PalmStrike2)
                .DroidAISlots(2)
                .Description("Deals weapon DMG + 24. Spends Opening at activation. If the sequenced strike lands, grants Finish for 4 seconds; Finish gives your weapon auto-attacks +3 Accuracy against this enemy with any weapon.")
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 22)
                .RequirementMustHavePerk(PerkType.Jab)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PalmStrike3)
                .DroidAISlots(3)
                .Description("Deals weapon DMG + 36. Spends Opening at activation. If the sequenced strike lands, grants Finish for 4 seconds; Finish gives your weapon auto-attacks +3 Accuracy against this enemy with any weapon.")
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 45)
                .RequirementMustHavePerk(PerkType.Jab);
        }

        private void PracticedForm()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.PracticedForm)
                .Name("Practiced Form")
                .Icon("ife_pracform")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PracticedFormTrait)
                .Description("Adds +4 total DMG to Palm Strike spending Opening, or a finisher spending Opening/Finish. Applied once per cast; split evenly between Finishing Combination's two impacts.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueConditionalPower, 4)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 10)
                .RequirementMustHavePerk(PerkType.PalmStrike)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PracticedForm2Trait)
                .Description("Adds +8 total DMG to Palm Strike spending Opening, or a finisher spending Opening/Finish. Applied once per cast; split evenly between Finishing Combination's two impacts.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueConditionalPower, 8)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 30)
                .RequirementMustHavePerk(PerkType.PalmStrike)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PracticedForm3Trait)
                .Description("Adds +12 total DMG to Palm Strike spending Opening, or a finisher spending Opening/Finish. Applied once per cast; split evenly between Finishing Combination's two impacts.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueConditionalPower, 12)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 38)
                .RequirementMustHavePerk(PerkType.PalmStrike);
        }

        private void MeasuredTechnique()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.MeasuredTechnique)
                .Name("Measured Technique")
                .Icon("ife_meastech")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MeasuredTechniqueTrait)
                .Description("While your Opening or Finish is active, gain +10 ability Accuracy against its enemy with any skill. This also applies to the ability that spends the setup.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueSetupAbilityAccuracy, 10)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 15)
                .RequirementMustHavePerk(PerkType.PalmStrike);
        }

        private void FinishingCombination()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.FinishingCombination)
                .Name("Finishing Combination")
                .Icon("ife_fincombo")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FinishingCombination1)
                .DroidAISlots(1)
                .Description("Two strikes at one enemy, each 50% of weapon DMG + 8. Spend Opening for +3 DMG per strike and redirect the second strike to the nearest other enemy within 5m of you, if available. Spend Finish for +6 DMG per strike, both against the selected enemy.")
                .Price(3)
                .RequirementSkill(SkillType.Unarmed, 18)
                .RequirementMustHavePerk(PerkType.Jab)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FinishingCombination2)
                .DroidAISlots(2)
                .Description("Two strikes at one enemy, each 50% of weapon DMG + 14. Spend Opening for +4 DMG per strike and redirect the second strike to the nearest other enemy within 5m of you, if available. Spend Finish for +8 DMG per strike, both against the selected enemy.")
                .Price(3)
                .RequirementSkill(SkillType.Unarmed, 32)
                .RequirementMustHavePerk(PerkType.Jab);
        }

        private void FlowStance()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.FlowStance)
                .Name("Flow Stance")
                .Icon("ife_flowst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlowStance1)
                .DroidAISlots(1)
                .Description("While active, a landed Finishing Combination spending Opening grants Slip Away: +20% Evasion and +15% movement speed for 8 seconds. A landed Finishing Combination or Perfect Form spending Finish instead grants Press Forward: +15% Haste for 8 seconds. These windows are mutually exclusive and share a 12-second trigger cooldown. Weapon swaps and unrelated hostile skills against the same enemy preserve the window. Stance, combat or enemy changes clear it. Reduces Defense by 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 20);
        }

        private void ControlledBreathing()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.ControlledBreathing)
                .Name("Controlled Breathing")
                .Icon("ife_ctrlbreath")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ControlledBreathingTrait)
                .Description("Finishing Combination spending Opening/Finish, or Perfect Form spending Finish, restores 2 STM if any hit lands. Once per cast, no more than STM paid; shares a 12-second refund cooldown with Catch Your Breath.")
                .IncreasesStat(StatType.TechniqueLine, 1)
                .IncreasesStat(StatType.TechniqueRefundStamina, 2)
                .IncreasesStat(StatType.TechniqueRefundCooldownSeconds, 12)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 25)
                .RequirementMustHavePerk(PerkType.FinishingCombination);
        }

        private void RootedTechnique()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.RootedTechnique)
                .Name("Rooted Technique")
                .Icon("ife_roottech")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RootedTechniqueTrait)
                .Description("Gain +15 Mobility Resistance.")
                .IncreasesStat(StatType.MobilityResistance, 15)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 35);
        }

        private void PerfectForm()
        {
            _builder.Create(PerkCategoryType.UnarmedMartialArtist, PerkType.PerfectForm)
                .Name("Perfect Form")
                .Icon("ife_perf_form")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PerfectForm1)
                .Description("Spends your Finish against one enemy to deal weapon DMG + 75. Cannot be used without Finish, and Finish cannot expire during the cast. Practiced Form applies.")
                .Price(6)
                .RequirementSkill(SkillType.Unarmed, 50)
                .RequirementQuest(UnarmedMasteryQuestDefinition.PerfectFormMasteryQuestId)
                .RequirementMustHavePerk(PerkType.PalmStrike);
        }

        private void DirtyJab()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.DirtyJab)
                .Name("Dirty Jab")
                .Icon("ife_dirtyjab")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DirtyJab1)
                .DroidAISlots(1)
                .Description("Deals weapon DMG + 8. On hit, applies your Rattled for 6 seconds, reducing this enemy's weapon auto-attack Accuracy by 10.")
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DirtyJab2)
                .DroidAISlots(2)
                .Description("Deals weapon DMG + 18. On hit, applies your Rattled for 6 seconds, reducing this enemy's weapon auto-attack Accuracy by 10.")
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DirtyJab3)
                .DroidAISlots(3)
                .Description("Deals weapon DMG + 28. On hit, applies your Rattled for 6 seconds, reducing this enemy's weapon auto-attack Accuracy by 10.")
                .Price(3)
                .RequirementSkill(SkillType.Unarmed, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DirtyJab4)
                .DroidAISlots(4)
                .Description("Deals weapon DMG + 38. On hit, applies your Rattled for 6 seconds, reducing this enemy's weapon auto-attack Accuracy by 10.")
                .Price(5)
                .RequirementSkill(SkillType.Unarmed, 40);
        }

        private void DirtyFighting()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.DirtyFighting)
                .Name("Dirty Fighting")
                .Icon("ife_dirtyfight")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DirtyFightingTrait)
                .Description("Extends your Rattled from 6 to 8 seconds. Your weapon auto-attacks gain +3 Accuracy against the enemy affected by your Rattled, with any weapon. Uppercut and Haymaker do not consume Rattled.")
                .IncreasesStat(StatType.TechniqueLine, 2)
                .IncreasesStat(StatType.TechniqueWindowExtensionSeconds, 2)
                .IncreasesStat(StatType.TechniqueDebuffTargetOrdinaryAccuracy, 3)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 5)
                .RequirementMustHavePerk(PerkType.DirtyJab);
        }

        private void Uppercut()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.Uppercut)
                .Name("Uppercut")
                .Icon("ife_uppercut")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Uppercut1)
                .DroidAISlots(1)
                .Description("Deals weapon DMG + 12. No Rattled requirement. Can spend optional Retaliation against its enemy.")
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 8)
                .RequirementMustHavePerk(PerkType.DirtyJab)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Uppercut2)
                .DroidAISlots(2)
                .Description("Deals weapon DMG + 24. No Rattled requirement. Can spend optional Retaliation against its enemy.")
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 22)
                .RequirementMustHavePerk(PerkType.DirtyJab)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Uppercut3)
                .DroidAISlots(3)
                .Description("Deals weapon DMG + 36. No Rattled requirement. Can spend optional Retaliation against its enemy.")
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 45)
                .RequirementMustHavePerk(PerkType.DirtyJab);
        }

        private void HeavyBlows()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.HeavyBlows)
                .Name("Heavy Blows")
                .Icon("ife_heavyblows")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HeavyBlowsTrait)
                .Description("Uppercut and Haymaker gain +4 total DMG per activation, without requiring or consuming Rattled.")
                .IncreasesStat(StatType.TechniqueLine, 2)
                .IncreasesStat(StatType.TechniqueAbilityPower, 4)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 10)
                .RequirementMustHavePerk(PerkType.Uppercut)

                .AddPerkLevel()
                .GrantsFeat(FeatType.HeavyBlows2Trait)
                .Description("Uppercut and Haymaker gain +8 total DMG per activation, without requiring or consuming Rattled.")
                .IncreasesStat(StatType.TechniqueLine, 2)
                .IncreasesStat(StatType.TechniqueAbilityPower, 8)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 30)
                .RequirementMustHavePerk(PerkType.Uppercut)

                .AddPerkLevel()
                .GrantsFeat(FeatType.HeavyBlows3Trait)
                .Description("Uppercut and Haymaker gain +12 total DMG per activation, without requiring or consuming Rattled.")
                .IncreasesStat(StatType.TechniqueLine, 2)
                .IncreasesStat(StatType.TechniqueAbilityPower, 12)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 38)
                .RequirementMustHavePerk(PerkType.Uppercut);
        }

        private void HardKnocks()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.HardKnocks)
                .Name("Hard Knocks")
                .Icon("ife_hardknocks")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HardKnocksTrait)
                .Description("Gain +15 Trauma Resistance.")
                .IncreasesStat(StatType.TraumaResistance, 15)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 15);
        }

        private void LowKick()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.LowKick)
                .Name("Low Kick")
                .Icon("ife_lowkick")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LowKick1)
                .DroidAISlots(1)
                .Description("Deals weapon DMG + 16. On hit, this enemy's weapon auto-attacks deal 10% less physical damage for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Unarmed, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LowKick2)
                .DroidAISlots(2)
                .Description("Deals weapon DMG + 30. On hit, this enemy's weapon auto-attacks deal 10% less physical damage for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Unarmed, 32);
        }

        private void BruiserStance()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.BruiserStance)
                .Name("Bruiser Stance")
                .Icon("ife_bruisst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BruiserStance1)
                .DroidAISlots(1)
                .Description("Increases Physical Defense by 10%, with any equipment. Direct hostile damage to real HP arms Retaliation against the attacker: +12 total DMG on your next Uppercut or Haymaker against that enemy within 8 seconds. One charge, with an 18-second grant cooldown. No Dirty Jab or Rattled requirement. The bonus is spent on activation, including a miss. Weapon swaps and unrelated hostile skills against the same enemy preserve it. Stance, combat or enemy changes clear it. Reduces Attack by 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 20);
        }

        private void CatchYourBreath()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.CatchYourBreath)
                .Name("Catch Your Breath")
                .Icon("ife_catchbreath")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CatchYourBreathTrait)
                .Description("A landed Uppercut or Haymaker restores 2 STM once per paid cast, no more than STM paid. No Rattled requirement. Shares a 12-second refund cooldown with Controlled Breathing.")
                .IncreasesStat(StatType.TechniqueLine, 2)
                .IncreasesStat(StatType.TechniqueRefundStamina, 2)
                .IncreasesStat(StatType.TechniqueRefundCooldownSeconds, 12)
                .Price(4)
                .RequirementSkill(SkillType.Unarmed, 25)
                .RequirementMustHavePerk(PerkType.Uppercut);
        }

        private void Payback()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.Payback)
                .Name("Payback")
                .Icon("ife_payback")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PaybackTrait)
                .Description("Direct hostile damage to real HP makes your next ability with a positive STM cost cost 2 less STM, from any skill and with any equipment. One charge, lasts 8 seconds, triggers at most once per 18 seconds.")
                .IncreasesStat(StatType.DirectHostileDamageNextAbilityStaminaDiscount, 2)
                .IncreasesStat(StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds, 8)
                .IncreasesStat(StatType.DirectHostileDamageNextAbilityDiscountCooldownSeconds, 18)
                .IncreasesStat(StatType.TechniquePresentationSkillType, (int)SkillType.Unarmed)
                .Price(2)
                .RequirementSkill(SkillType.Unarmed, 35);
        }

        private void Haymaker()
        {
            _builder.Create(PerkCategoryType.UnarmedBrawler, PerkType.Haymaker)
                .Name("Haymaker")
                .Icon("ife_haymaker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Haymaker1)
                .Description("Deals weapon DMG + 50 to one enemy. No Rattled requirement. Heavy Blows applies, and the strike can spend optional Retaliation against its enemy.")
                .Price(6)
                .RequirementSkill(SkillType.Unarmed, 50)
                .RequirementQuest(UnarmedMasteryQuestDefinition.HaymakerMasteryQuestId)
                .RequirementMustHavePerk(PerkType.DirtyJab);
        }
    }
}
