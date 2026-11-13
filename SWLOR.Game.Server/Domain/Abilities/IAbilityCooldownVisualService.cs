using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>Shows elapsed recharge on ability icons through native texture overrides.</summary>
    public interface IAbilityCooldownVisualService
    {
        void CacheData();

        void RestoreCooldownVisuals();

        /// <summary>The work of <see cref="RestoreCooldownVisuals"/> for one player (a hot reload's restore step).</summary>
        void RestoreCooldownVisualsFor(uint player);

        void ClearCooldownVisualsOnExit();

        /// <summary>Builds a native recharge frame name from feat or fitted module artwork.</summary>
        string GetCooldownTextureName(string sourceTexture, int stage);

        int CalculateCooldownStage(DateTime now, DateTime startedAt, DateTime endsAt);

        /// <summary>Displays elapsed recharge on cached ability icons or a supplied texture anchor, using optional separate artwork.</summary>
        void ApplyRecastDelay(uint player, RecastGroup group, DateTime startedAt, DateTime endsAt,
            string sourceTexture = null, string iconTexture = null);

        /// <summary>Updates a deadline while retaining elapsed progress and any active single-texture anchor and artwork.</summary>
        void RefreshRecastDelay(uint player, RecastGroup group, DateTime endsAt);

        void ClearRecastDelay(uint player, RecastGroup group);

        /// <summary>
        /// The recharge frame showing for a recast group, or -1 when it is not recharging.
        /// </summary>
        int GetCooldownStage(uint player, RecastGroup group);

        void ClearAllRecastDelays(uint player);
    }
}
