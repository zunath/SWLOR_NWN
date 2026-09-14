namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public interface IGuardedHitStatusEffect
    {
        void OnGuardedHitEffect(uint defender, uint attacker, int preventedDamage);
    }
}
