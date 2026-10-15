using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// The ally-facing half of the Warden Wall Stance: applied by the stance's aura tick to party
    /// members near the warden, granting the same defensive hardening the warden has. Short-lived;
    /// continuously refreshed while the ally stays in range of an active Warden Wall Stance.
    /// </summary>
    public sealed class WardenWallStanceAuraStatusEffect : StatusEffectBase
    {
        public override string Name => "Warden Wall Stance Aura";
        public override EffectIconType Icon => EffectIconType.WardenWallStanceAuraStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override bool PersistsOnLogout => false;

        /// <summary>
        /// False for the stance's periodic refresh so allies already covered are not re-announced every tick.
        /// </summary>
        public bool AnnouncesApplication { get; init; } = true;
        public override bool SendsApplicationMessage => AnnouncesApplication;

        public WardenWallStanceAuraStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceDefenseAuraPercentAdjustment] = 10;
        }
    }
}
