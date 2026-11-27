using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>Persisted targeting preferences apply to all eligible native ability buttons.</summary>
    public interface IQuickCastService : IFeatHotBar
    {
        FeatType GetOriginalFeat(FeatType feat);

        bool IsEnabled(PlayerSettings settings, QuickCastTargetType type);

        bool IsEnabled(uint player, QuickCastTargetType type);

        bool UsesAutomaticTarget(FeatType original, PlayerSettings settings);

        FeatType GetDisplayedFeat(uint player, FeatType feat);

        void ApplyOnEnter();

        void ApplyOnQuickbarEdit();

        bool TryCast(uint player, FeatType alias, out string error);
    }
}
