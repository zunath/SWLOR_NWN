namespace SWLOR.Game.Server.Domain.Character
{
    public interface IGuardedHitStatusEffect
    {
        void OnGuardedHitEffect(uint defender, uint attacker, int preventedDamage);
    }
}
