namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface ICombatService
    {
        (int, int) CalculateDamageRange(uint attacker, uint target, int baseDamage, int variance);
        int CalculateDamage(uint attacker, uint target, int baseDamage, int variance);
    }
}
