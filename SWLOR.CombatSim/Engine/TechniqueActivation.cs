using SWLOR.Game.Server.Service.AbilityService;
namespace SWLOR.CombatSim.Engine;
public sealed class TechniqueActivation
{
    public MeleeTechniqueStage Stage { get; init; }
    public int ConditionalPower { get; init; }
    public int Accuracy { get; init; }
    public int FlatPower { get; init; }
    public int HitPercent { get; init; }
    public int PaidStamina { get; init; }
    public int HitStaminaRefunded { get; set; }
    public bool CostlyHitRefundApplied { get; set; }
    public bool IsSweep { get; set; }
    public bool IsArea { get; set; }
    public int ImpactedTargets { get; set; }
    public Combatant SweepSecondary { get; set; }
}
