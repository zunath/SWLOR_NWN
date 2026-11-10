using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>Persisted targeting preferences apply to all eligible native ability buttons.</summary>
    public interface IQuickCastService
    {
        FeatType GetOriginalFeat(FeatType feat);

        bool IsEnabled(PlayerSettings settings, QuickCastTargetType type);

        bool IsEnabled(uint player, QuickCastTargetType type);

        bool UsesAutomaticTarget(FeatType original, PlayerSettings settings);

        FeatType GetDisplayedFeat(uint player, FeatType feat);

        bool KnowsFeat(uint creature, FeatType feat);

        void RemoveFeat(uint creature, FeatType feat);

        void ApplyPreferences(uint player);

        void ApplyOnEnter();

        void ApplyOnQuickbarEdit();

        bool TryCast(uint player, FeatType alias, out string error);
    }
}
