using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Engineering perk lines: Overclocker (high load) and Technician (stable, precise). Every perk acts only on the Circuit
    /// minigame through the stats <see cref="CircuitPerkStats"/> reads. Each line has ten purchases at Engineering ranks
    /// 5/10/15/20/25/30/35/40/45/50 priced 1/1/2/2/3/3/3/3/3/4 SP; a ranked perk spends two of them, and a rank restates what the
    /// rank below it gave because only the purchased rank's bonuses apply.
    /// </summary>
    public class EngineeringPerkDefinition : IPerkListDefinition
    {
        private const string PerkIcon = "ife_gearhead";

        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ConductiveWiring();
            CapacitorBank();
            HeatSinks();
            OverclockStyle();
            CascadeDischarge();
            OverdriveBoost();
            ShockAbsorbers();
            CircuitSurge();

            InsulatedTraces();
            FineCalibration();
            CircuitDiagnostics();
            SafeModeStyle();
            MicroTune();
            SignalLock();
            PrecisionPatch();
            CircuitFailsafe();

            return _builder.Build();
        }

        private void ConductiveWiring()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.ConductiveWiring)
                .Name("Conductive Wiring")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, each Load point adds {CircuitPerkRules.ConductiveWiringLoadBoostRank1} more percentage point of output to Wire, Tune and Discharge.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 5)
                .IncreasesStat(StatType.CircuitLoadBoostPercent, CircuitPerkRules.ConductiveWiringLoadBoostRank1)

                .AddPerkLevel()
                .Description($"In the Circuit, each Load point adds {CircuitPerkRules.ConductiveWiringLoadBoostRank2} more percentage points of output to Wire, Tune and Discharge.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 45)
                .IncreasesStat(StatType.CircuitLoadBoostPercent, CircuitPerkRules.ConductiveWiringLoadBoostRank2);
        }

        private void CapacitorBank()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.CapacitorBank)
                .Name("Capacitor Bank")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, Discharge bursts are {CircuitPerkRules.CapacitorBankDischargePercent}% larger.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 10)
                .IncreasesStat(StatType.CircuitDischargePowerPercent, CircuitPerkRules.CapacitorBankDischargePercent);
        }

        private void HeatSinks()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.HeatSinks)
                .Name("Heat Sinks")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, the overload line rises by {CircuitPerkRules.HeatSinksLineRank1}.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 15)
                .IncreasesStat(StatType.CircuitOverloadLine, CircuitPerkRules.HeatSinksLineRank1)

                .AddPerkLevel()
                .Description($"In the Circuit, the overload line rises by {CircuitPerkRules.HeatSinksLineRank2}.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 35)
                .IncreasesStat(StatType.CircuitOverloadLine, CircuitPerkRules.HeatSinksLineRank2);
        }

        private void OverclockStyle()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.OverclockStyle)
                .Name("Overclock")
                .Icon(PerkIcon)
                .TriggerRefund(CircuitStyles.RemoveUnowned)

                .AddPerkLevel()
                .Description($"Circuit style, picked in the Engineering craft window with one style active at a time. {CircuitStyles.Description(CircuitStyle.Overclock)}")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 20);
        }

        private void CascadeDischarge()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.CascadeDischarge)
                .Name("Cascade Discharge")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"Adds the Cascade Discharge action to the Circuit. It costs {CircuitRules.CascadeDischargeCp} CP and spends all Load as one burst into both Assembly and Calibration at {CircuitRules.CascadeDischargeSharePercent}% each. It never overloads.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 25)
                .IncreasesStat(StatType.CircuitCascadeDischargeUnlocked, 1);
        }

        private void OverdriveBoost()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.OverdriveBoost)
                .Name("Overdrive Boost")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, Boost adds {CircuitRules.BoostLoad + CircuitPerkRules.OverdriveBoostExtraLoad} Load instead of {CircuitRules.BoostLoad} and costs {CircuitPerkRules.OverdriveBoostCpReduction} less CP.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 30)
                .IncreasesStat(StatType.CircuitBoostLoad, CircuitPerkRules.OverdriveBoostExtraLoad)
                .IncreasesStat(StatType.CircuitBoostCpReduction, CircuitPerkRules.OverdriveBoostCpReduction);
        }

        private void ShockAbsorbers()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.ShockAbsorbers)
                .Name("Shock Absorbers")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, an overload costs a third less Integrity ({CircuitRules.OverloadIntegrityPercent}% of the piece's maximum becomes {CircuitRules.OverloadIntegrityPercent * (100 - CircuitPerkRules.ShockAbsorbersReductionPercent) / 100}%).")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 40)
                .IncreasesStat(StatType.CircuitOverloadIntegrityReductionPercent, CircuitPerkRules.ShockAbsorbersReductionPercent);
        }

        private void CircuitSurge()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.CircuitSurge)
                .Name("Surge")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description("Once per Circuit craft, the first overload does not cost Integrity. It discharges all your Load at full strength into whichever of Assembly or Calibration is further from full (Assembly on a tie).")
                .Price(4)
                .RequirementSkill(SkillType.Engineering, 50)
                .IncreasesStat(StatType.CircuitSurgeUnlocked, 1);
        }

        private void InsulatedTraces()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.InsulatedTraces)
                .Name("Insulated Traces")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, Wire, Tune, Boost and Discharge cost {CircuitPerkRules.InsulatedTracesReductionRank1} less Integrity, to a minimum of {CircuitRules.MinimumActionIntegrity}. Ground and Patch are unchanged.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 5)
                .IncreasesStat(StatType.CircuitActionIntegrityReduction, CircuitPerkRules.InsulatedTracesReductionRank1)

                .AddPerkLevel()
                .Description($"In the Circuit, Wire, Tune, Boost and Discharge cost {CircuitPerkRules.InsulatedTracesReductionRank2} less Integrity, to a minimum of {CircuitRules.MinimumActionIntegrity}. Ground and Patch are unchanged.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 40)
                .IncreasesStat(StatType.CircuitActionIntegrityReduction, CircuitPerkRules.InsulatedTracesReductionRank2);
        }

        private void FineCalibration()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.FineCalibration)
                .Name("Fine Calibration")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, Tune builds {CircuitPerkRules.FineCalibrationPercentRank1}% more Calibration.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 10)
                .IncreasesStat(StatType.CircuitTunePowerPercent, CircuitPerkRules.FineCalibrationPercentRank1)

                .AddPerkLevel()
                .Description($"In the Circuit, Tune builds {CircuitPerkRules.FineCalibrationPercentRank2}% more Calibration.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 45)
                .IncreasesStat(StatType.CircuitTunePowerPercent, CircuitPerkRules.FineCalibrationPercentRank2);
        }

        private void CircuitDiagnostics()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.CircuitDiagnostics)
                .Name("Diagnostics")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description("In the Circuit, a Clean Signal is announced two actions ahead and stays announced until it applies.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 15)
                .IncreasesStat(StatType.CircuitForecastTurns, CircuitPerkRules.DiagnosticsExtraForecastTurns);
        }

        private void SafeModeStyle()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.SafeModeStyle)
                .Name("Safe Mode")
                .Icon(PerkIcon)
                .TriggerRefund(CircuitStyles.RemoveUnowned)

                .AddPerkLevel()
                .Description($"Circuit style, picked in the Engineering craft window with one style active at a time. {CircuitStyles.Description(CircuitStyle.SafeMode)}")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 20);
        }

        private void MicroTune()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.MicroTune)
                .Name("Micro-Tune")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"Adds the Micro-Tune action to the Circuit. It builds {CircuitRules.MicroTuneSharePercent}% of a Tune's Calibration, adds no Load and costs {CircuitRules.MicroTuneCp} CP and {CircuitRules.MicroTuneIntegrity} Integrity. It can overload only when Load is already over the line.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 25)
                .IncreasesStat(StatType.CircuitMicroTuneUnlocked, 1);
        }

        private void SignalLock()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.SignalLock)
                .Name("Signal Lock")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, a Clean Signal is announced on {CircuitPerkRules.SignalLockChancePercent} percentage points more actions ({CircuitRules.CleanSignalChancePercent}% becomes {CircuitRules.CleanSignalChancePercent + CircuitPerkRules.SignalLockChancePercent}%).")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 30)
                .IncreasesStat(StatType.CircuitSignalChancePercent, CircuitPerkRules.SignalLockChancePercent);
        }

        private void PrecisionPatch()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.PrecisionPatch)
                .Name("Precision Patch")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description($"In the Circuit, Patch restores {CircuitPerkRules.PrecisionPatchExtraRestore} more Integrity and costs {CircuitPerkRules.PrecisionPatchCpReduction} less CP.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 35)
                .IncreasesStat(StatType.CircuitPatchRestore, CircuitPerkRules.PrecisionPatchExtraRestore)
                .IncreasesStat(StatType.CircuitPatchCpReduction, CircuitPerkRules.PrecisionPatchCpReduction);
        }

        private void CircuitFailsafe()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.CircuitFailsafe)
                .Name("Failsafe")
                .Icon(PerkIcon)

                .AddPerkLevel()
                .Description("Once per Circuit craft, the first time Integrity would reach 0 it stays at 1 and the piece survives.")
                .Price(4)
                .RequirementSkill(SkillType.Engineering, 50)
                .IncreasesStat(StatType.CircuitFailsafeUnlocked, 1);
        }
    }
}
