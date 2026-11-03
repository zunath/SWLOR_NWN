using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Economy;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the casino slot machine. The logic lives in <see cref="ICasinoSlotService"/>; every member here
    /// forwards to the container's instance, and the event handler and the constants stay here so the name in the
    /// handler order manifest and the callers that read the constants do not change.
    /// </summary>
    public static class CasinoSlot
    {
        private static ICasinoSlotService Instance => FacadeAccessor.Resolve<ICasinoSlotService>();

        public const int SpinCost = Domain.Economy.CasinoSlotService.SpinCost;
        public const int MaximumPayout = Domain.Economy.CasinoSlotService.MaximumPayout;

        public static bool IsCasinoMachine(string areaResref, string resref, string tag) =>
            Instance.IsCasinoMachine(areaResref, resref, tag);

        public static int GetPayout(int roll) =>
            Instance.GetPayout(roll);

        [NWNEventHandler(ScriptName.OnCasinoSlotUsed)]
        public static void OnUsed()
        {
            Instance.OnUsed();
        }

        public static DateTime GetNextSpinUtc(uint player) =>
            Instance.GetNextSpinUtc(player);

        public static CasinoSpinResult Spin(uint player, uint machine) =>
            Instance.Spin(player, machine);
    }
}
