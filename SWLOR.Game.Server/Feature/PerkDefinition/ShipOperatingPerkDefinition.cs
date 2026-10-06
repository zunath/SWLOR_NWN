using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ShipOperatingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BuildOperatingPerks();
            return _builder.Build();
        }
        private void BuildOperatingPerks()
        {
            var builder = _builder;
            builder.Create(PerkCategoryType.ShipCombatPilot, PerkType.ShipIntercept).Name("Intercept").Icon("ife_sintercept1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 2).Description("15 percent base speed for 8s. Costs 8 extra capacitor; 24s cooldown. Self; pursuing a hostile ship within 40m; no teleport.")
                .GrantsFeat(FeatType.ShipIntercept1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 15).Description("20 percent base speed for 8s. Costs 8 extra capacitor; 24s cooldown. Self; pursuing a hostile ship within 40m; no teleport.")
                .GrantsFeat(FeatType.ShipIntercept2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 30).Description("25 percent base speed for 8s. Costs 8 extra capacitor; 24s cooldown. Self; pursuing a hostile ship within 40m; no teleport.")
                .GrantsFeat(FeatType.ShipIntercept3);
            builder.Create(PerkCategoryType.ShipCombatPilot, PerkType.ShipBreakAway).Name("Break Away").Icon("ife_sbreakaway1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 10).Description("3 seconds ignoring soft movement slows. Costs 10 extra capacitor; 36s cooldown. Self; disables weapon banks for 3s; does not erase hard control.")
                .GrantsFeat(FeatType.ShipBreakAway1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 25).Description("4 seconds ignoring soft movement slows. Costs 10 extra capacitor; 36s cooldown. Self; disables weapon banks for 3s; does not erase hard control.")
                .GrantsFeat(FeatType.ShipBreakAway2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 40).Description("5 seconds ignoring soft movement slows. Costs 10 extra capacitor; 36s cooldown. Self; disables weapon banks for 3s; does not erase hard control.")
                .GrantsFeat(FeatType.ShipBreakAway3);
            builder.Create(PerkCategoryType.ShipCombatPilot, PerkType.ShipEvasiveManeuver).Name("Evasive Maneuver").Icon("ife_sevade1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 20).Description("8 pre-tracking evasion percentage points for 5s. Costs 12 extra capacitor; 30s cooldown. Self; -15% base weapon output during the window.")
                .GrantsFeat(FeatType.ShipEvasiveManeuver1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 35).Description("10 pre-tracking evasion percentage points for 5s. Costs 12 extra capacitor; 30s cooldown. Self; -15% base weapon output during the window.")
                .GrantsFeat(FeatType.ShipEvasiveManeuver2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 45).Description("12 pre-tracking evasion percentage points for 5s. Costs 12 extra capacitor; 30s cooldown. Self; -15% base weapon output during the window.")
                .GrantsFeat(FeatType.ShipEvasiveManeuver3);
            builder.Create(PerkCategoryType.ShipCombatPilot, PerkType.ShipManeuverHandling).Name("Maneuver Handling").Icon("ife_smaneuver")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 5).Description("2 percent base speed; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipManeuverHandlingTrait)
                .IncreasesStat(StatType.ShipSpeed, 200)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 20).Description("4 percent base speed; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipSpeed, 400)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 35).Description("6 percent base speed; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipSpeed, 600);
            builder.Create(PerkCategoryType.ShipCombatPilot, PerkType.ShipPursuitEfficiency).Name("Pursuit Efficiency").Icon("ife_spursuitec")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 15).Description("3 percent capacitor discount on propulsion techniques; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipPursuitEfficiencyTrait)
                .IncreasesStat(StatType.ShipPropulsionCapacitorDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 30).Description("6 percent capacitor discount on propulsion techniques; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipPropulsionCapacitorDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 45).Description("9 percent capacitor discount on propulsion techniques; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipPropulsionCapacitorDiscount, 900);
            builder.Create(PerkCategoryType.ShipCombatPilot, PerkType.ShipPursuit).Name("Pursuit").Icon("ife_spursuit1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 25).Description("+5% base speed and +10% weapon tracking; -10 shield resistance rating")
                .GrantsFeat(FeatType.ShipPursuit1);
            builder.Create(PerkCategoryType.ShipCombatPilot, PerkType.ShipAceManeuver).Name("Ace Maneuver").Icon("ife_sace1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.Piloting, 50).Description("+35% base speed and +12 pre-tracking evasion points for 6s; weapon banks disabled for first 2s. Costs 20 extra capacitor; 120s cooldown. Self; temporary caps and highest-effect rules.")
                .GrantsFeat(FeatType.ShipAceManeuver1);
            builder.Create(PerkCategoryType.ShipExpeditionPilot, PerkType.ShipEfficientTransit).Name("Efficient Transit").Icon("ife_stransit1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 2).Description("10 percent lower departure service charge on one transit leg. Costs 6 extra capacitor; 30s cooldown. Self; committed noncombat transit objective; consume discount once.")
                .GrantsFeat(FeatType.ShipEfficientTransit1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 15).Description("15 percent lower departure service charge on one transit leg. Costs 6 extra capacitor; 30s cooldown. Self; committed noncombat transit objective; consume discount once.")
                .GrantsFeat(FeatType.ShipEfficientTransit2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 30).Description("20 percent lower departure service charge on one transit leg. Costs 6 extra capacitor; 30s cooldown. Self; committed noncombat transit objective; consume discount once.")
                .GrantsFeat(FeatType.ShipEfficientTransit3);
            builder.Create(PerkCategoryType.ShipExpeditionPilot, PerkType.ShipHazardRun).Name("Hazard Run").Icon("ife_shazardrun1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 10).Description("15 percent less environmental hull damage for 8s. Costs 10 extra capacitor; 36s cooldown. Self; environmental hazards only, not hostile weapon damage.")
                .GrantsFeat(FeatType.ShipHazardRun1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 25).Description("20 percent less environmental hull damage for 8s. Costs 10 extra capacitor; 36s cooldown. Self; environmental hazards only, not hostile weapon damage.")
                .GrantsFeat(FeatType.ShipHazardRun2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 40).Description("25 percent less environmental hull damage for 8s. Costs 10 extra capacitor; 36s cooldown. Self; environmental hazards only, not hostile weapon damage.")
                .GrantsFeat(FeatType.ShipHazardRun3);
            builder.Create(PerkCategoryType.ShipExpeditionPilot, PerkType.ShipEmergencyEscape).Name("Emergency Escape").Icon("ife_sescape1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 20).Description("15 percent base speed for 10s. Costs 14 extra capacitor; 60s cooldown. Self; weapon banks disabled throughout; no encounter completion bypass.")
                .GrantsFeat(FeatType.ShipEmergencyEscape1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 35).Description("20 percent base speed for 10s. Costs 14 extra capacitor; 60s cooldown. Self; weapon banks disabled throughout; no encounter completion bypass.")
                .GrantsFeat(FeatType.ShipEmergencyEscape2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 45).Description("25 percent base speed for 10s. Costs 14 extra capacitor; 60s cooldown. Self; weapon banks disabled throughout; no encounter completion bypass.")
                .GrantsFeat(FeatType.ShipEmergencyEscape3);
            builder.Create(PerkCategoryType.ShipExpeditionPilot, PerkType.ShipNavigationEconomy).Name("Navigation Economy").Icon("ife_snavecon")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 5).Description("3 percent lower departure service charge; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipNavigationEconomyTrait)
                .IncreasesStat(StatType.ShipServiceDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 20).Description("6 percent lower departure service charge; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipServiceDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 35).Description("9 percent lower departure service charge; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipServiceDiscount, 900);
            builder.Create(PerkCategoryType.ShipExpeditionPilot, PerkType.ShipHazardHandling).Name("Hazard Handling").Icon("ife_shazard")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Piloting, 15).Description("5 percent less environmental hull damage; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipHazardHandlingTrait)
                .IncreasesStat(StatType.ShipEnvironmentalMitigation, 500)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Piloting, 30).Description("10 percent less environmental hull damage; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipEnvironmentalMitigation, 1000)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 45).Description("15 percent less environmental hull damage; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipEnvironmentalMitigation, 1500);
            builder.Create(PerkCategoryType.ShipExpeditionPilot, PerkType.ShipCruise).Name("Cruise").Icon("ife_scruise1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Piloting, 25).Description("+10% base speed; +15% incoming damage, weapon output -20%")
                .GrantsFeat(FeatType.ShipCruise1);
            builder.Create(PerkCategoryType.ShipExpeditionPilot, PerkType.ShipSafePassage).Name("Safe Passage").Icon("ife_spassage1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.Piloting, 50).Description("Environmental damage -50% for 12s; +25% base speed; weapon banks disabled. Costs 25 extra capacitor; 180s cooldown. Self; one paid hazardous leg; no immunity to enemies.")
                .GrantsFeat(FeatType.ShipSafePassage1);
            builder.Create(PerkCategoryType.ShipPrecisionGunnery, PerkType.ShipControlledBurst).Name("Controlled Burst").Icon("ife_sburst1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 2).Description("10 percent base weapon output for 8s. Costs 10 extra capacitor; 30s cooldown. One fitted bank; weapon capacitor costs +20% during the window.")
                .GrantsFeat(FeatType.ShipControlledBurst1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 15).Description("15 percent base weapon output for 8s. Costs 10 extra capacitor; 30s cooldown. One fitted bank; weapon capacitor costs +20% during the window.")
                .GrantsFeat(FeatType.ShipControlledBurst2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 30).Description("20 percent base weapon output for 8s. Costs 10 extra capacitor; 30s cooldown. One fitted bank; weapon capacitor costs +20% during the window.")
                .GrantsFeat(FeatType.ShipControlledBurst3);
            builder.Create(PerkCategoryType.ShipPrecisionGunnery, PerkType.ShipTrackingSolution).Name("Tracking Solution").Icon("ife_stracksol1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 10).Description("15 percent weapon tracking for 10s. Costs 8 extra capacitor; 24s cooldown. One fitted weapon bank against selected hostile ship within 40m.")
                .GrantsFeat(FeatType.ShipTrackingSolution1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 25).Description("20 percent weapon tracking for 10s. Costs 8 extra capacitor; 24s cooldown. One fitted weapon bank against selected hostile ship within 40m.")
                .GrantsFeat(FeatType.ShipTrackingSolution2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 40).Description("25 percent weapon tracking for 10s. Costs 8 extra capacitor; 24s cooldown. One fitted weapon bank against selected hostile ship within 40m.")
                .GrantsFeat(FeatType.ShipTrackingSolution3);
            builder.Create(PerkCategoryType.ShipPrecisionGunnery, PerkType.ShipExploitOpening).Name("Exploit Opening").Icon("ife_sopening1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 20).Description("12 percent base weapon output for 8s. Costs 10 extra capacitor; 36s cooldown. Target has a declared exposed-system status; applies to compatible fitted weapons.")
                .GrantsFeat(FeatType.ShipExploitOpening1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 35).Description("16 percent base weapon output for 8s. Costs 10 extra capacitor; 36s cooldown. Target has a declared exposed-system status; applies to compatible fitted weapons.")
                .GrantsFeat(FeatType.ShipExploitOpening2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 45).Description("20 percent base weapon output for 8s. Costs 10 extra capacitor; 36s cooldown. Target has a declared exposed-system status; applies to compatible fitted weapons.")
                .GrantsFeat(FeatType.ShipExploitOpening3);
            builder.Create(PerkCategoryType.ShipPrecisionGunnery, PerkType.ShipWeaponHandling).Name("Weapon Handling").Icon("ife_sgunhandle")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 5).Description("4 percent tracking on compatible weapons; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipWeaponHandlingTrait)
                .IncreasesStat(StatType.ShipTracking, 400)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 20).Description("8 percent tracking on compatible weapons; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipTracking, 800)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 35).Description("12 percent tracking on compatible weapons; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipTracking, 1200);
            builder.Create(PerkCategoryType.ShipPrecisionGunnery, PerkType.ShipFiringEfficiency).Name("Firing Efficiency").Icon("ife_sfiringeco")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 15).Description("3 percent weapon capacitor discount; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipFiringEfficiencyTrait)
                .IncreasesStat(StatType.ShipWeaponCapacitorDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 30).Description("6 percent weapon capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipWeaponCapacitorDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 45).Description("9 percent weapon capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipWeaponCapacitorDiscount, 900);
            builder.Create(PerkCategoryType.ShipPrecisionGunnery, PerkType.ShipPrecision).Name("Precision").Icon("ife_sprecision1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 25).Description("+15% tracking; -10% base weapon output")
                .GrantsFeat(FeatType.ShipPrecision1);
            builder.Create(PerkCategoryType.ShipPrecisionGunnery, PerkType.ShipPerfectSolution).Name("Perfect Solution").Icon("ife_sperfectai1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.Gunnery, 50).Description("+8 pre-tracking accuracy points and +30% tracking for 10s. Costs 20 extra capacitor; 120s cooldown. One fitted weapon bank; hit ceiling remains 95%.")
                .GrantsFeat(FeatType.ShipPerfectSolution1);
            builder.Create(PerkCategoryType.ShipHeavyOrdnance, PerkType.ShipPreparedVolley).Name("Prepared Volley").Icon("ife_svolley1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 2).Description("10 percent base ordnance output for next paid volley within 10s. Costs 10 extra capacitor; 30s cooldown. Fitted ordnance bank; every participating launcher consumes ammunition.")
                .GrantsFeat(FeatType.ShipPreparedVolley1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 15).Description("15 percent base ordnance output for next paid volley within 10s. Costs 10 extra capacitor; 30s cooldown. Fitted ordnance bank; every participating launcher consumes ammunition.")
                .GrantsFeat(FeatType.ShipPreparedVolley2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 30).Description("20 percent base ordnance output for next paid volley within 10s. Costs 10 extra capacitor; 30s cooldown. Fitted ordnance bank; every participating launcher consumes ammunition.")
                .GrantsFeat(FeatType.ShipPreparedVolley3);
            builder.Create(PerkCategoryType.ShipHeavyOrdnance, PerkType.ShipTorpedoRun).Name("Torpedo Run").Icon("ife_storprun1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 10).Description("15 percent base torpedo output for next shot within 8s. Costs 12 extra capacitor; 36s cooldown. Torpedo launcher; target signature >=100; speed -20% during preparation.")
                .GrantsFeat(FeatType.ShipTorpedoRun1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 25).Description("20 percent base torpedo output for next shot within 8s. Costs 12 extra capacitor; 36s cooldown. Torpedo launcher; target signature >=100; speed -20% during preparation.")
                .GrantsFeat(FeatType.ShipTorpedoRun2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 40).Description("25 percent base torpedo output for next shot within 8s. Costs 12 extra capacitor; 36s cooldown. Torpedo launcher; target signature >=100; speed -20% during preparation.")
                .GrantsFeat(FeatType.ShipTorpedoRun3);
            builder.Create(PerkCategoryType.ShipHeavyOrdnance, PerkType.ShipBombardment).Name("Bombardment").Icon("ife_sbombard1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 20).Description("15 percent base heavy-ordnance output for next shot within 10s. Costs 16 extra capacitor; 45s cooldown. Heavy launcher; selected target signature >=200 or authored heavy objective.")
                .GrantsFeat(FeatType.ShipBombardment1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 35).Description("20 percent base heavy-ordnance output for next shot within 10s. Costs 16 extra capacitor; 45s cooldown. Heavy launcher; selected target signature >=200 or authored heavy objective.")
                .GrantsFeat(FeatType.ShipBombardment2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 45).Description("25 percent base heavy-ordnance output for next shot within 10s. Costs 16 extra capacitor; 45s cooldown. Heavy launcher; selected target signature >=200 or authored heavy objective.")
                .GrantsFeat(FeatType.ShipBombardment3);
            builder.Create(PerkCategoryType.ShipHeavyOrdnance, PerkType.ShipOrdnanceHandling).Name("Ordnance Handling").Icon("ife_sordhandle")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 5).Description("4 percent ordnance tracking; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipOrdnanceHandlingTrait)
                .IncreasesStat(StatType.ShipOrdnanceTracking, 400)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 20).Description("8 percent ordnance tracking; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipOrdnanceTracking, 800)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 35).Description("12 percent ordnance tracking; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipOrdnanceTracking, 1200);
            builder.Create(PerkCategoryType.ShipHeavyOrdnance, PerkType.ShipAmmunitionEconomy).Name("Ammunition Economy").Icon("ife_sammoecon")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Gunnery, 15).Description("3 percent docking ammunition purchase discount; no ammo-free firing chance; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipAmmunitionEconomyTrait)
                .IncreasesStat(StatType.ShipAmmunitionDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Gunnery, 30).Description("6 percent docking ammunition purchase discount; no ammo-free firing chance; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipAmmunitionDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 45).Description("9 percent docking ammunition purchase discount; no ammo-free firing chance; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipAmmunitionDiscount, 900);
            builder.Create(PerkCategoryType.ShipHeavyOrdnance, PerkType.ShipSiege).Name("Siege").Icon("ife_ssiege1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Gunnery, 25).Description("+10% base ordnance output; -20% base speed and -6 pre-tracking evasion points")
                .GrantsFeat(FeatType.ShipSiege1);
            builder.Create(PerkCategoryType.ShipHeavyOrdnance, PerkType.ShipCoordinatedSalvo).Name("Coordinated Salvo").Icon("ife_ssalvo1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.Gunnery, 50).Description("+30% base ordnance output on next bank volley within 10s; 3s telegraph; launchers spend 150% normal ammunition rounded up. Costs 25 extra capacitor; 120s cooldown. Fitted ordnance bank; target signature >=100; ordinary capacitor costs still paid.")
                .GrantsFeat(FeatType.ShipCoordinatedSalvo1);
            builder.Create(PerkCategoryType.ShipDefensiveSystems, PerkType.ShipEmergencyRepair).Name("Emergency Repair").Icon("ife_semergrepa1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 2).Description("10 percent base self-recovery output for 8s. Costs 10 extra capacitor; 36s cooldown. Fitted self hull/shield repairer; each recovery still pays normal module cost.")
                .GrantsFeat(FeatType.ShipEmergencyRepair1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 15).Description("15 percent base self-recovery output for 8s. Costs 10 extra capacitor; 36s cooldown. Fitted self hull/shield repairer; each recovery still pays normal module cost.")
                .GrantsFeat(FeatType.ShipEmergencyRepair2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 30).Description("20 percent base self-recovery output for 8s. Costs 10 extra capacitor; 36s cooldown. Fitted self hull/shield repairer; each recovery still pays normal module cost.")
                .GrantsFeat(FeatType.ShipEmergencyRepair3);
            builder.Create(PerkCategoryType.ShipDefensiveSystems, PerkType.ShipPowerRouting).Name("Power Routing").Icon("ife_spwrroute1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 10).Description("10 percent base weapon output sacrificed for same percent self-recovery output for 10s. Costs 8 extra capacitor; 30s cooldown. Fitted repairer; no free shield or capacitor generation.")
                .GrantsFeat(FeatType.ShipPowerRouting1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 25).Description("15 percent base weapon output sacrificed for same percent self-recovery output for 10s. Costs 8 extra capacitor; 30s cooldown. Fitted repairer; no free shield or capacitor generation.")
                .GrantsFeat(FeatType.ShipPowerRouting2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 40).Description("20 percent base weapon output sacrificed for same percent self-recovery output for 10s. Costs 8 extra capacitor; 30s cooldown. Fitted repairer; no free shield or capacitor generation.")
                .GrantsFeat(FeatType.ShipPowerRouting3);
            builder.Create(PerkCategoryType.ShipDefensiveSystems, PerkType.ShipShieldRecovery).Name("Shield Recovery").Icon("ife_sshieldrec1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 20).Description("10 percent base shield recovery output for next paid cycle within 8s. Costs 10 extra capacitor; 30s cooldown. Fitted shield booster/repairer; target self.")
                .GrantsFeat(FeatType.ShipShieldRecovery1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 35).Description("15 percent base shield recovery output for next paid cycle within 8s. Costs 10 extra capacitor; 30s cooldown. Fitted shield booster/repairer; target self.")
                .GrantsFeat(FeatType.ShipShieldRecovery2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 45).Description("20 percent base shield recovery output for next paid cycle within 8s. Costs 10 extra capacitor; 30s cooldown. Fitted shield booster/repairer; target self.")
                .GrantsFeat(FeatType.ShipShieldRecovery3);
            builder.Create(PerkCategoryType.ShipDefensiveSystems, PerkType.ShipRecoveryEfficiency).Name("Recovery Efficiency").Icon("ife_srepaireco")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 5).Description("3 percent capacitor discount on recovery modules; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipRecoveryEfficiencyTrait)
                .IncreasesStat(StatType.ShipRepairCapacitorDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 20).Description("6 percent capacitor discount on recovery modules; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipRepairCapacitorDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 35).Description("9 percent capacitor discount on recovery modules; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipRepairCapacitorDiscount, 900);
            builder.Create(PerkCategoryType.ShipDefensiveSystems, PerkType.ShipPowerDiscipline).Name("Power Discipline").Icon("ife_spwrdisc")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 15).Description("2 percent lower incoming capacitor destruction; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipPowerDisciplineTrait)
                .IncreasesStat(StatType.ShipCapacitorDamageMitigation, 200)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 30).Description("4 percent lower incoming capacitor destruction; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipCapacitorDamageMitigation, 400)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 45).Description("6 percent lower incoming capacitor destruction; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipCapacitorDamageMitigation, 600);
            builder.Create(PerkCategoryType.ShipDefensiveSystems, PerkType.ShipDefensiveRouting).Name("Defensive Routing").Icon("ife_sdefroute1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 25).Description("+10% base self-recovery output; -15% base weapon output")
                .GrantsFeat(FeatType.ShipDefensiveRouting1);
            builder.Create(PerkCategoryType.ShipDefensiveSystems, PerkType.ShipDamageControl).Name("Damage Control").Icon("ife_sdmgcontro1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.ShipSystems, 50).Description("+30% base self-recovery output and +25 hull/shield resistance rating for 8s. Costs 25 extra capacitor; 150s cooldown. Fitted recovery hardware; highest temporary bonuses; not a free heal.")
                .GrantsFeat(FeatType.ShipDamageControl1);
            builder.Create(PerkCategoryType.ShipFleetSupport, PerkType.ShipRepairLink).Name("Repair Link").Icon("ife_srepairlin1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 2).Description("10 percent base allied recovery output for 10s. Costs 10 extra capacitor; 30s cooldown. Fitted allied projector; target within normal module range; reception cap applies.")
                .GrantsFeat(FeatType.ShipRepairLink1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 15).Description("15 percent base allied recovery output for 10s. Costs 10 extra capacitor; 30s cooldown. Fitted allied projector; target within normal module range; reception cap applies.")
                .GrantsFeat(FeatType.ShipRepairLink2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 30).Description("20 percent base allied recovery output for 10s. Costs 10 extra capacitor; 30s cooldown. Fitted allied projector; target within normal module range; reception cap applies.")
                .GrantsFeat(FeatType.ShipRepairLink3);
            builder.Create(PerkCategoryType.ShipFleetSupport, PerkType.ShipCapacitorTransfer).Name("Capacitor Transfer").Icon("ife_scaptransf1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 10).Description("82 percent transfer efficiency for next paid transfer within 8s. Costs 4 extra capacitor; 24s cooldown. Fitted transfer projector; sender pays normal transfer plus technique cost.")
                .GrantsFeat(FeatType.ShipCapacitorTransfer1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 25).Description("86 percent transfer efficiency for next paid transfer within 8s. Costs 4 extra capacitor; 24s cooldown. Fitted transfer projector; sender pays normal transfer plus technique cost.")
                .GrantsFeat(FeatType.ShipCapacitorTransfer2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 40).Description("90 percent transfer efficiency for next paid transfer within 8s. Costs 4 extra capacitor; 24s cooldown. Fitted transfer projector; sender pays normal transfer plus technique cost.")
                .GrantsFeat(FeatType.ShipCapacitorTransfer3);
            builder.Create(PerkCategoryType.ShipFleetSupport, PerkType.ShipSupportSurge).Name("Support Surge").Icon("ife_ssupportsu1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 20).Description("10 percent base external recovery output for 8s. Costs 14 extra capacitor; 36s cooldown. Fitted support bank; +20% capacitor expenditure during window.")
                .GrantsFeat(FeatType.ShipSupportSurge1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 35).Description("15 percent base external recovery output for 8s. Costs 14 extra capacitor; 36s cooldown. Fitted support bank; +20% capacitor expenditure during window.")
                .GrantsFeat(FeatType.ShipSupportSurge2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 45).Description("20 percent base external recovery output for 8s. Costs 14 extra capacitor; 36s cooldown. Fitted support bank; +20% capacitor expenditure during window.")
                .GrantsFeat(FeatType.ShipSupportSurge3);
            builder.Create(PerkCategoryType.ShipFleetSupport, PerkType.ShipProjectorHandling).Name("Projector Handling").Icon("ife_sprojector")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 5).Description("3 percent projector range; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipProjectorHandlingTrait)
                .IncreasesStat(StatType.ShipRepairRange, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 20).Description("6 percent projector range; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipRepairRange, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 35).Description("9 percent projector range; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipRepairRange, 900);
            builder.Create(PerkCategoryType.ShipFleetSupport, PerkType.ShipTransferEfficiency).Name("Transfer Efficiency").Icon("ife_stransfere")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.ShipSystems, 15).Description("2 transfer efficiency percentage points; total <=90%; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipTransferEfficiencyTrait)
                .IncreasesStat(StatType.ShipTransferEfficiency, 200)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.ShipSystems, 30).Description("4 transfer efficiency percentage points; total <=90%; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipTransferEfficiency, 400)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 45).Description("6 transfer efficiency percentage points; total <=90%; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipTransferEfficiency, 600);
            builder.Create(PerkCategoryType.ShipFleetSupport, PerkType.ShipSupportRouting).Name("Support Routing").Icon("ife_ssupportro1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.ShipSystems, 25).Description("+10% base external recovery output; -20% base weapon output")
                .GrantsFeat(FeatType.ShipSupportRouting1);
            builder.Create(PerkCategoryType.ShipFleetSupport, PerkType.ShipFleetStabilization).Name("Fleet Stabilization").Icon("ife_sfleetstab1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.ShipSystems, 50).Description("+30% base external recovery output for 10s; up to 3 recipients within 35m; all fitted support pools remain bounded. Costs 30 extra capacitor; 150s cooldown. Fitted allied recovery bank; per-target reception caps still apply.")
                .GrantsFeat(FeatType.ShipFleetStabilization1);
            builder.Create(PerkCategoryType.ShipSurveying, PerkType.ShipDepositAnalysis).Name("Deposit Analysis").Icon("ife_sdeposcan1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 2).Description("5 scanner resolution for next scan within 10s. Costs 6 extra capacitor; 24s cooldown. Fitted deposit scanner; target within scanner range.")
                .GrantsFeat(FeatType.ShipDepositAnalysis1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 15).Description("10 scanner resolution for next scan within 10s. Costs 6 extra capacitor; 24s cooldown. Fitted deposit scanner; target within scanner range.")
                .GrantsFeat(FeatType.ShipDepositAnalysis2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 30).Description("15 scanner resolution for next scan within 10s. Costs 6 extra capacitor; 24s cooldown. Fitted deposit scanner; target within scanner range.")
                .GrantsFeat(FeatType.ShipDepositAnalysis3);
            builder.Create(PerkCategoryType.ShipSurveying, PerkType.ShipAnomalyScan).Name("Anomaly Scan").Icon("ife_sanomscan1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 10).Description("5 scanner resolution for next anomaly scan within 10s. Costs 8 extra capacitor; 30s cooldown. Fitted scanner; authored anomaly; first discovery only rewards XP.")
                .GrantsFeat(FeatType.ShipAnomalyScan1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 25).Description("10 scanner resolution for next anomaly scan within 10s. Costs 8 extra capacitor; 30s cooldown. Fitted scanner; authored anomaly; first discovery only rewards XP.")
                .GrantsFeat(FeatType.ShipAnomalyScan2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 40).Description("15 scanner resolution for next anomaly scan within 10s. Costs 8 extra capacitor; 30s cooldown. Fitted scanner; authored anomaly; first discovery only rewards XP.")
                .GrantsFeat(FeatType.ShipAnomalyScan3);
            builder.Create(PerkCategoryType.ShipSurveying, PerkType.ShipRouteSurvey).Name("Route Survey").Icon("ife_sroutesurv1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 20).Description("5 percent lower environmental damage for next committed leg within 60s. Costs 10 extra capacitor; 45s cooldown. Fitted scanner; scan completes in 8s; shared hazard mitigation cap 60%.")
                .GrantsFeat(FeatType.ShipRouteSurvey1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 35).Description("10 percent lower environmental damage for next committed leg within 60s. Costs 10 extra capacitor; 45s cooldown. Fitted scanner; scan completes in 8s; shared hazard mitigation cap 60%.")
                .GrantsFeat(FeatType.ShipRouteSurvey2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 45).Description("15 percent lower environmental damage for next committed leg within 60s. Costs 10 extra capacitor; 45s cooldown. Fitted scanner; scan completes in 8s; shared hazard mitigation cap 60%.")
                .GrantsFeat(FeatType.ShipRouteSurvey3);
            builder.Create(PerkCategoryType.ShipSurveying, PerkType.ShipSurveyResolution).Name("Survey Resolution").Icon("ife_ssurveyres")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 5).Description("3 scanner resolution; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipSurveyResolutionTrait)
                .IncreasesStat(StatType.ShipScannerResolution, 3)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 20).Description("6 scanner resolution; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipScannerResolution, 6)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 35).Description("9 scanner resolution; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipScannerResolution, 9);
            builder.Create(PerkCategoryType.ShipSurveying, PerkType.ShipScanEconomy).Name("Scan Economy").Icon("ife_sscanecon")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 15).Description("3 percent scanner capacitor discount; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipScanEconomyTrait)
                .IncreasesStat(StatType.ShipScannerCapacitorDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 30).Description("6 percent scanner capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipScannerCapacitorDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 45).Description("9 percent scanner capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipScannerCapacitorDiscount, 900);
            builder.Create(PerkCategoryType.ShipSurveying, PerkType.ShipDetailedSurvey).Name("Detailed Survey").Icon("ife_sdetailsur1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 25).Description("+10 scanner resolution; scan cycle +25%")
                .GrantsFeat(FeatType.ShipDetailedSurvey1);
            builder.Create(PerkCategoryType.ShipSurveying, PerkType.ShipDeepSurvey).Name("Deep Survey").Icon("ife_sdeepsurve1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.Astrometrics, 50).Description("+25 scanner resolution on one 15s committed scan; reveals at most one authored hidden seam. Costs 20 extra capacitor; 180s cooldown. Fitted deep scanner; discovery key prevents repeated rewards; no resource creation.")
                .GrantsFeat(FeatType.ShipDeepSurvey1);
            builder.Create(PerkCategoryType.ShipElectronicWarfare, PerkType.ShipTargetAnalysis).Name("Target Analysis").Icon("ife_stargetana1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 2).Description("3 pre-tracking accuracy points for allies attacking one target for 10s. Costs 8 extra capacitor; 30s cooldown. Fitted targeting or survey sensor; hostile target within 35m; highest analysis effect.")
                .GrantsFeat(FeatType.ShipTargetAnalysis1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 15).Description("4 pre-tracking accuracy points for allies attacking one target for 10s. Costs 8 extra capacitor; 30s cooldown. Fitted targeting or survey sensor; hostile target within 35m; highest analysis effect.")
                .GrantsFeat(FeatType.ShipTargetAnalysis2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 30).Description("5 pre-tracking accuracy points for allies attacking one target for 10s. Costs 8 extra capacitor; 30s cooldown. Fitted targeting or survey sensor; hostile target within 35m; highest analysis effect.")
                .GrantsFeat(FeatType.ShipTargetAnalysis3);
            builder.Create(PerkCategoryType.ShipElectronicWarfare, PerkType.ShipSensorDisruption).Name("Sensor Disruption").Icon("ife_sdisruptio1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 10).Description("2 extra accuracy penalty percentage points on next paid interference activation within 8s. Costs 8 extra capacitor; 30s cooldown. Fitted interference suite; combined debuff <=25 points; control DR.")
                .GrantsFeat(FeatType.ShipSensorDisruption1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 25).Description("3 extra accuracy penalty percentage points on next paid interference activation within 8s. Costs 8 extra capacitor; 30s cooldown. Fitted interference suite; combined debuff <=25 points; control DR.")
                .GrantsFeat(FeatType.ShipSensorDisruption2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 40).Description("4 extra accuracy penalty percentage points on next paid interference activation within 8s. Costs 8 extra capacitor; 30s cooldown. Fitted interference suite; combined debuff <=25 points; control DR.")
                .GrantsFeat(FeatType.ShipSensorDisruption3);
            builder.Create(PerkCategoryType.ShipElectronicWarfare, PerkType.ShipCountermeasureTiming).Name("Countermeasure Timing").Icon("ife_scounterti1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 20).Description("5 extra percent incoming ordnance tracking reduction for next paid countermeasure activation within 8s. Costs 8 extra capacitor; 30s cooldown. Fitted countermeasure suite; total tracking reduction <=25%; highest effect.")
                .GrantsFeat(FeatType.ShipCountermeasureTiming1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 35).Description("8 extra percent incoming ordnance tracking reduction for next paid countermeasure activation within 8s. Costs 8 extra capacitor; 30s cooldown. Fitted countermeasure suite; total tracking reduction <=25%; highest effect.")
                .GrantsFeat(FeatType.ShipCountermeasureTiming2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 45).Description("10 extra percent incoming ordnance tracking reduction for next paid countermeasure activation within 8s. Costs 8 extra capacitor; 30s cooldown. Fitted countermeasure suite; total tracking reduction <=25%; highest effect.")
                .GrantsFeat(FeatType.ShipCountermeasureTiming3);
            builder.Create(PerkCategoryType.ShipElectronicWarfare, PerkType.ShipSensorHandling).Name("Sensor Handling").Icon("ife_ssensor")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 5).Description("3 percent electronic module range; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipSensorHandlingTrait)
                .IncreasesStat(StatType.ShipElectronicRange, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 20).Description("6 percent electronic module range; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipElectronicRange, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 35).Description("9 percent electronic module range; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipElectronicRange, 900);
            builder.Create(PerkCategoryType.ShipElectronicWarfare, PerkType.ShipInterferenceEfficiency).Name("Interference Efficiency").Icon("ife_sewarecon")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Astrometrics, 15).Description("3 percent electronic module capacitor discount; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipInterferenceEfficiencyTrait)
                .IncreasesStat(StatType.ShipElectronicCapacitorDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Astrometrics, 30).Description("6 percent electronic module capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipElectronicCapacitorDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 45).Description("9 percent electronic module capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipElectronicCapacitorDiscount, 900);
            builder.Create(PerkCategoryType.ShipElectronicWarfare, PerkType.ShipInterference).Name("Interference").Icon("ife_sinterfere1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Astrometrics, 25).Description("+2 percentage points interference effect; signature +15%, -10% base weapon output")
                .GrantsFeat(FeatType.ShipInterference1);
            builder.Create(PerkCategoryType.ShipElectronicWarfare, PerkType.ShipSignalBreak).Name("Signal Break").Icon("ife_ssignalbre1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.Astrometrics, 50).Description("One hostile target loses module activation for 3s after a 2s telegraph; 20s control-family immunity afterward. Costs 25 extra capacitor; 150s cooldown. Fitted interference suite; range 30m; bosses retain movement and objective processing.")
                .GrantsFeat(FeatType.ShipSignalBreak1);
            builder.Create(PerkCategoryType.ShipExtraction, PerkType.ShipPrecisionExtraction).Name("Precision Extraction").Icon("ife_sprecismin1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 2).Description("2 recovery percentage points for 18s. Costs 8 extra capacitor; 36s cooldown. Fitted miner; reserve removal -10%; recovery <=95%.")
                .GrantsFeat(FeatType.ShipPrecisionExtraction1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 15).Description("4 recovery percentage points for 18s. Costs 8 extra capacitor; 36s cooldown. Fitted miner; reserve removal -10%; recovery <=95%.")
                .GrantsFeat(FeatType.ShipPrecisionExtraction2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 30).Description("6 recovery percentage points for 18s. Costs 8 extra capacitor; 36s cooldown. Fitted miner; reserve removal -10%; recovery <=95%.")
                .GrantsFeat(FeatType.ShipPrecisionExtraction3);
            builder.Create(PerkCategoryType.ShipExtraction, PerkType.ShipExtractionSurge).Name("Extraction Surge").Icon("ife_sminesurge1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 10).Description("10 percent reserve removal per cycle for 18s. Costs 12 extra capacitor; 45s cooldown. Fitted miner; +20% capacitor expenditure; same finite reserve.")
                .GrantsFeat(FeatType.ShipExtractionSurge1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 25).Description("15 percent reserve removal per cycle for 18s. Costs 12 extra capacitor; 45s cooldown. Fitted miner; +20% capacitor expenditure; same finite reserve.")
                .GrantsFeat(FeatType.ShipExtractionSurge2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 40).Description("20 percent reserve removal per cycle for 18s. Costs 12 extra capacitor; 45s cooldown. Fitted miner; +20% capacitor expenditure; same finite reserve.")
                .GrantsFeat(FeatType.ShipExtractionSurge3);
            builder.Create(PerkCategoryType.ShipExtraction, PerkType.ShipSelectiveRecovery).Name("Selective Recovery").Icon("ife_sselectore1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 20).Description("60 percent of recovered output directed to one surveyed constituent for one cycle. Costs 10 extra capacitor; 30s cooldown. Fitted precision cutter/deep drill; reserve composition still limits availability.")
                .GrantsFeat(FeatType.ShipSelectiveRecovery1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 35).Description("70 percent of recovered output directed to one surveyed constituent for one cycle. Costs 10 extra capacitor; 30s cooldown. Fitted precision cutter/deep drill; reserve composition still limits availability.")
                .GrantsFeat(FeatType.ShipSelectiveRecovery2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 45).Description("80 percent of recovered output directed to one surveyed constituent for one cycle. Costs 10 extra capacitor; 30s cooldown. Fitted precision cutter/deep drill; reserve composition still limits availability.")
                .GrantsFeat(FeatType.ShipSelectiveRecovery3);
            builder.Create(PerkCategoryType.ShipExtraction, PerkType.ShipBeamHandling).Name("Beam Handling").Icon("ife_sbeamhandl")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 5).Description("2 recovery percentage points; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipBeamHandlingTrait)
                .IncreasesStat(StatType.ShipResourceRecovery, 200)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 20).Description("4 recovery percentage points; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipResourceRecovery, 400)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 35).Description("6 recovery percentage points; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipResourceRecovery, 600);
            builder.Create(PerkCategoryType.ShipExtraction, PerkType.ShipIndustrialEfficiency).Name("Industrial Efficiency").Icon("ife_sindustrye")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 15).Description("3 percent mining capacitor discount; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipIndustrialEfficiencyTrait)
                .IncreasesStat(StatType.ShipIndustryCapacitorDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 30).Description("6 percent mining capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipIndustryCapacitorDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 45).Description("9 percent mining capacitor discount; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipIndustryCapacitorDiscount, 900);
            builder.Create(PerkCategoryType.ShipExtraction, PerkType.ShipCarefulExtraction).Name("Careful Extraction").Icon("ife_scarefulmi1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 25).Description("+5 recovery percentage points; reserve removal -20%")
                .GrantsFeat(FeatType.ShipCarefulExtraction1);
            builder.Create(PerkCategoryType.ShipExtraction, PerkType.ShipDeepCoreExtraction).Name("Deep-Core Extraction").Icon("ife_sdeepcore1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.SpaceIndustry, 50).Description("Deep drill hardness limit +10 and recovery +8 percentage points for one 20s committed cycle; movement locked. Costs 20 extra capacitor; 120s cooldown. Fitted deep drill; base reserve removal remains 10 for the industrial drill or 4 for the compact drill; recovery <=95%.")
                .GrantsFeat(FeatType.ShipDeepCoreExtraction1);
            builder.Create(PerkCategoryType.ShipSalvageandLogistics, PerkType.ShipCarefulDismantling).Name("Careful Dismantling").Icon("ife_sdismantle1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 2).Description("3 intact-component chance percentage points for next paid attempt within 10s. Costs 8 extra capacitor; 36s cooldown. Fitted electronics kit; component chance <=55%; failed attempt still consumes reserve.")
                .GrantsFeat(FeatType.ShipCarefulDismantling1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 15).Description("5 intact-component chance percentage points for next paid attempt within 10s. Costs 8 extra capacitor; 36s cooldown. Fitted electronics kit; component chance <=55%; failed attempt still consumes reserve.")
                .GrantsFeat(FeatType.ShipCarefulDismantling2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 30).Description("7 intact-component chance percentage points for next paid attempt within 10s. Costs 8 extra capacitor; 36s cooldown. Fitted electronics kit; component chance <=55%; failed attempt still consumes reserve.")
                .GrantsFeat(FeatType.ShipCarefulDismantling3);
            builder.Create(PerkCategoryType.ShipSalvageandLogistics, PerkType.ShipRecoverySweep).Name("Recovery Sweep").Icon("ife_srecoverys1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 10).Description("10 percent bulk salvage reserve removal for 18s. Costs 12 extra capacitor; 45s cooldown. Fitted recovery arm/cutter; +20% activation expenditure.")
                .GrantsFeat(FeatType.ShipRecoverySweep1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 25).Description("15 percent bulk salvage reserve removal for 18s. Costs 12 extra capacitor; 45s cooldown. Fitted recovery arm/cutter; +20% activation expenditure.")
                .GrantsFeat(FeatType.ShipRecoverySweep2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 40).Description("20 percent bulk salvage reserve removal for 18s. Costs 12 extra capacitor; 45s cooldown. Fitted recovery arm/cutter; +20% activation expenditure.")
                .GrantsFeat(FeatType.ShipRecoverySweep3);
            builder.Create(PerkCategoryType.ShipSalvageandLogistics, PerkType.ShipCargoHandling).Name("Cargo Handling").Icon("ife_scargohand1")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 20).Description("5 percent shorter authored loading/unloading time for one operation within 60s. Costs 6 extra capacitor; 30s cooldown. Dock/transfer equipment; cannot change capacity or duplicate cargo.")
                .GrantsFeat(FeatType.ShipCargoHandling1)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 35).Description("8 percent shorter authored loading/unloading time for one operation within 60s. Costs 6 extra capacitor; 30s cooldown. Dock/transfer equipment; cannot change capacity or duplicate cargo.")
                .GrantsFeat(FeatType.ShipCargoHandling2)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 45).Description("10 percent shorter authored loading/unloading time for one operation within 60s. Costs 6 extra capacitor; 30s cooldown. Dock/transfer equipment; cannot change capacity or duplicate cargo.")
                .GrantsFeat(FeatType.ShipCargoHandling3);
            builder.Create(PerkCategoryType.ShipSalvageandLogistics, PerkType.ShipComponentRecovery).Name("Component Recovery").Icon("ife_scomponent")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 5).Description("2 intact-component chance percentage points; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipComponentRecoveryTrait)
                .IncreasesStat(StatType.ShipIntactSalvageChance, 200)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 20).Description("4 intact-component chance percentage points; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipIntactSalvageChance, 400)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 35).Description("6 intact-component chance percentage points; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipIntactSalvageChance, 600);
            builder.Create(PerkCategoryType.ShipSalvageandLogistics, PerkType.ShipFreightEfficiency).Name("Freight Efficiency").Icon("ife_sfreightec")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.SpaceIndustry, 15).Description("3 percent lower freight service charge; replaces earlier rank, never adds all ranks together.")
                .GrantsFeat(FeatType.ShipFreightEfficiencyTrait)
                .IncreasesStat(StatType.ShipServiceDiscount, 300)
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.SpaceIndustry, 30).Description("6 percent lower freight service charge; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipServiceDiscount, 600)
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 45).Description("9 percent lower freight service charge; replaces earlier rank, never adds all ranks together.")
                .IncreasesStat(StatType.ShipServiceDiscount, 900);
            builder.Create(PerkCategoryType.ShipSalvageandLogistics, PerkType.ShipRecoveryOperations).Name("Recovery Operations").Icon("ife_srecovery1")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.SpaceIndustry, 25).Description("+5 bulk salvage recovery points and +3 intact-component chance points; cycles +20%")
                .GrantsFeat(FeatType.ShipRecoveryOperations1);
            builder.Create(PerkCategoryType.ShipSalvageandLogistics, PerkType.ShipSpecialistRecovery).Name("Specialist Recovery").Icon("ife_sspecialis1")
                .AddPerkLevel().Price(7).RequirementSkill(SkillType.SpaceIndustry, 50).Description("+8 intact-component chance points for one 25s committed attempt on a declared difficult component. Costs 20 extra capacitor; 150s cooldown. Fitted electronics kit; chance <=55%; unique wreck reserve; does not guarantee rare loot.")
                .GrantsFeat(FeatType.ShipSpecialistRecovery1);
        }
    }
}
