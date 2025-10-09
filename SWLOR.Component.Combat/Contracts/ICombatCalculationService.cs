using SWLOR.Component.Combat.Model;

namespace SWLOR.Component.Combat.Contracts;

internal interface ICombatCalculationService
{
    int CalculateDamage(
        uint attacker, 
        uint defender, 
        uint weapon,
        bool isCritical);

    HitResult CalculateHitType(
        uint attacker, 
        uint defender,
        uint weapon);
}