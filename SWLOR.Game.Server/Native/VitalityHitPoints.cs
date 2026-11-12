using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Gives player Vitality its half-level HP contribution at odd scores: enables the seven hooks
    /// (<c>SWLOR.Game.NativeHooks.VitalityHitPointsTrampolines</c>) that forward to <c>NativeSinks.VitalityHitPointsSink</c>.
    /// </summary>
    public static class VitalityHitPoints
    {
        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.VitalityHitPoints);
        }

        public static short AddBonus(short maximum, int bonus)
            => bonus == 0 ? maximum : (short)Math.Min(maximum + bonus, short.MaxValue);

        public static int CalculateOddScoreBonus(int vitality, int eligibleLevels)
            => (vitality & 1) == 0 ? 0 : Math.Max(0, eligibleLevels) / 2;
    }
}
