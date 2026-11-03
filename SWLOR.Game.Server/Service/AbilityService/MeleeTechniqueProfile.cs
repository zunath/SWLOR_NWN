using System;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>Content-supplied presentation for a weapon skill's techniques, which any equipped weapon can perform.</summary>
    public sealed class MeleeTechniqueProfile
    {
        public Func<MeleeTechniqueStage, IStatusEffect> SetupMarkerFactory { get; init; }
        public Func<int, int, IStatusEffect> OpeningFinisherWindowFactory { get; init; }
        public Func<int, IStatusEffect> FinishFinisherWindowFactory { get; init; }
        public Func<IStatusEffect> StaminaDiscountMarkerFactory { get; init; }
        public Func<IStatusEffect> RetaliationMarkerFactory { get; init; }

        public bool IsValid => SetupMarkerFactory != null && OpeningFinisherWindowFactory != null &&
            FinishFinisherWindowFactory != null && StaminaDiscountMarkerFactory != null &&
            RetaliationMarkerFactory != null;
    }
}
