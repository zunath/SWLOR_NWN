using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The minutes a performer's Flourish banks for the watchers who follow them. A part of the Performance audience.
    /// </summary>
    public interface IPerformanceFlourishService
    {
        /// <summary>
        /// A Flourish banks minutes for every watcher past their warm-up who follows the performer, at most once per two
        /// minutes each. Two performers who Flourish within two seconds of each other double it for both audiences.
        /// </summary>
        void OnFlourish(PerformanceSession session);
    }
}
