namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>What one shot did.</summary>
public readonly record struct ShotResult(bool Hit, bool Critical, double Damage, double Applied);
