namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// A status effect that can spare its holder the XP debt of their next death. Death claims the
    /// waiver when the holder dies, because status effects are cleared before the debt is charged.
    /// </summary>
    public interface IDeathDebtWaiverStatusEffect : IStatusEffect
    {
        /// <summary>Uses up the waiver. Returns false when there is none left to use.</summary>
        bool TryConsumeDeathDebtWaiver();
    }
}
