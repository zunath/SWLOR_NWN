namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// A status effect that keeps its own count of a keyed temporary HP pool. It is told when the pool is
    /// trimmed without a hit, such as by the temporary HP cap after maximum HP drops.
    /// </summary>
    public interface ITemporaryHitPointPoolStatusEffect : IStatusEffect
    {
        string TemporaryHitPointPoolKey { get; }

        void OnTemporaryHitPointPoolTrimmed(uint creature, int remaining);
    }
}
