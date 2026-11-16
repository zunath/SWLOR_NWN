using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Whether a player may start a performance and which instrument they play it on: the logic that was
    /// <see cref="PerformanceService"/>'s own, moved here unchanged.
    /// </summary>
    public class PerformanceEligibilityService : IPerformanceEligibilityService
    {
        /// <summary>The item local string that names an instrument's family; also set by tests on the player or module.</summary>
        public const string InstrumentFamilyVariable = "PERFORMANCE_INSTRUMENT_FAMILY";

        private const string AllowNoInstrumentVariable = "PERFORMANCE_ALLOW_NO_INSTRUMENT";

        private readonly IActivityService _activity;
        private readonly ISpaceService _space;

        public PerformanceEligibilityService(IActivityService activity, ISpaceService space)
        {
            _activity = activity;
            _space = space;
        }

        public string GetStartError(
            uint player,
            PerformanceKind kind,
            out PerformanceInstrumentFamily family,
            out InventorySlot instrumentSlot,
            out uint instrument)
        {
            family = PerformanceInstrumentFamily.None;
            instrumentSlot = InventorySlot.Invalid;
            instrument = OBJECT_INVALID;
            string error = null;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                error = "Only players may perform.";
            else if (GetIsDead(player) || GetCurrentHitPoints(player) <= 0)
                error = "You cannot perform right now.";
            else if (GetIsInCombat(player))
                error = "You cannot perform while in combat.";
            else if (IsBusy(player))
                error = "You are busy.";
            else if (IsPlayerInSpaceMode(player))
                error = "You cannot perform in space.";
            else if (kind == PerformanceKind.Music && !TryFindInstrument(player, out family, out instrumentSlot, out instrument))
                error = "You must equip an instrument or microphone to perform music. A violin also requires a Violin Bow.";

            return error;
        }

        public bool HasInstrumentEquipped(uint player, out PerformanceInstrumentFamily family)
        {
            return TryFindInstrument(player, out family, out _, out _);
        }

        // Found once when a performance starts; each beat then checks only that slot (see IsInstrumentStillEquipped).
        private bool TryFindInstrument(
            uint player,
            out PerformanceInstrumentFamily family,
            out InventorySlot instrumentSlot,
            out uint instrument)
        {
            for (var slot = InventorySlot.Head; slot <= InventorySlot.Belt; slot++)
            {
                var item = GetItemInSlot(slot, player);
                if (GetIsObjectValid(item) &&
                    (PerformanceInstrumentCatalog.TryGetFamily(GetResRef(item), out family) ||
                     TryParseFamily(GetLocalString(item, InstrumentFamilyVariable), out family)))
                {
                    if (PerformanceInstrumentCatalog.RequiresBow(family) &&
                        !string.Equals(GetResRef(GetItemInSlot(InventorySlot.RightHand, player)), "violin_bow", StringComparison.OrdinalIgnoreCase))
                        continue;

                    instrumentSlot = slot;
                    instrument = item;
                    return true;
                }
            }

            instrumentSlot = InventorySlot.Invalid;
            instrument = OBJECT_INVALID;

            var module = GetModule();
            if (GetLocalInt(module, AllowNoInstrumentVariable) != 0 || GetLocalInt(player, AllowNoInstrumentVariable) != 0)
            {
                if (!TryParseFamily(GetLocalString(player, InstrumentFamilyVariable), out family) &&
                    !TryParseFamily(GetLocalString(module, InstrumentFamilyVariable), out family))
                {
                    family = PerformanceInstrumentFamily.Strings;
                }

                return true;
            }

            family = PerformanceInstrumentFamily.None;
            return false;
        }

        public bool IsInstrumentStillEquipped(PerformanceSession session)
        {
            return (session.Instrument == OBJECT_INVALID ||
                    GetItemInSlot(session.InstrumentSlot, session.Performer) == session.Instrument) &&
                   (session.InstrumentBow == OBJECT_INVALID ||
                    GetItemInSlot(InventorySlot.RightHand, session.Performer) == session.InstrumentBow);
        }

        private bool TryParseFamily(string text, out PerformanceInstrumentFamily family)
        {
            return Enum.TryParse(text, true, out family) &&
                   family != PerformanceInstrumentFamily.None &&
                   Enum.IsDefined(family);
        }

        // The calls into the Activity and Space services, one line each so a test can answer them.
        protected virtual bool IsBusy(uint target) => _activity.IsBusy(target);

        protected virtual bool IsPlayerInSpaceMode(uint player) => _space.IsPlayerInSpaceMode(player);
    }
}
