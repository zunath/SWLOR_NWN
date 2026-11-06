using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Shows elapsed recharge on ability icons through native texture overrides. The logic lives in
    /// <see cref="IAbilityCooldownVisualService"/>; the event handlers stay here and every member forwards to the
    /// container's instance.
    /// </summary>
    public static class AbilityCooldownVisual
    {
        public const int MaximumCooldownStage = 5;

        private static IAbilityCooldownVisualService Instance => FacadeAccessor.Resolve<IAbilityCooldownVisualService>();

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RestoreCooldownVisuals()
        {
            Instance.RestoreCooldownVisuals();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearCooldownVisualsOnExit()
        {
            Instance.ClearCooldownVisualsOnExit();
        }

        /// <summary>Builds a native recharge frame name from feat or fitted module artwork.</summary>
        public static string GetCooldownTextureName(string sourceTexture, int stage)
        {
            return Instance.GetCooldownTextureName(sourceTexture, stage);
        }

        public static int CalculateCooldownStage(DateTime now, DateTime startedAt, DateTime endsAt)
        {
            return Instance.CalculateCooldownStage(now, startedAt, endsAt);
        }

        /// <summary>Displays elapsed recharge on cached ability icons or a supplied texture anchor, using optional separate artwork.</summary>
        public static void ApplyRecastDelay(uint player, RecastGroup group, DateTime startedAt, DateTime endsAt,
            string sourceTexture = null, string iconTexture = null)
        {
            Instance.ApplyRecastDelay(player, group, startedAt, endsAt, sourceTexture, iconTexture);
        }

        /// <summary>Updates a deadline while retaining elapsed progress and any active single-texture anchor and artwork.</summary>
        public static void RefreshRecastDelay(uint player, RecastGroup group, DateTime endsAt)
        {
            Instance.RefreshRecastDelay(player, group, endsAt);
        }

        public static void ClearRecastDelay(uint player, RecastGroup group)
        {
            Instance.ClearRecastDelay(player, group);
        }

        /// <summary>
        /// The recharge frame showing for a recast group, or -1 when it is not recharging.
        /// </summary>
        public static int GetCooldownStage(uint player, RecastGroup group)
        {
            return Instance.GetCooldownStage(player, group);
        }

        public static void ClearAllRecastDelays(uint player)
        {
            Instance.ClearAllRecastDelays(player);
        }
    }
}
