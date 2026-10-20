using System;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>Content-supplied equipment rules and presentation for a weapon skill's techniques.</summary>
    public sealed class MeleeTechniqueProfile
    {
        public Func<uint, bool> HasValidEquipment { get; init; }
        public string EquipmentError { get; init; }
        public Func<MeleeTechniqueStage, IStatusEffect> SetupMarkerFactory { get; init; }
        public Func<int, int, IStatusEffect> OpeningFinisherWindowFactory { get; init; }
        public Func<int, IStatusEffect> FinishFinisherWindowFactory { get; init; }
        public Func<IStatusEffect> StaminaDiscountMarkerFactory { get; init; }
        public Func<IStatusEffect> RetaliationMarkerFactory { get; init; }

        public bool IsValid => HasValidEquipment != null && !string.IsNullOrWhiteSpace(EquipmentError) &&
            SetupMarkerFactory != null && OpeningFinisherWindowFactory != null &&
            FinishFinisherWindowFactory != null && StaminaDiscountMarkerFactory != null &&
            RetaliationMarkerFactory != null;
    }
}
