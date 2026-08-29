using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Receives a callback before an originating hit is applied. Use this only for state that must
    /// be validated before damage resolution; ordinary damage reactions belong in IStatusEffect.
    /// </summary>
    public interface IPreDamageStatusEffect : IStatusEffect
    {
        void OnBeforeDamageTaken(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct);
    }
}
