using NWN.Native.API;
using NWNX.NET;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Runtime.InteropServices;
using SavingThrow = NWN.Native.API.SavingThrow;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetWillSavingThrow
    {
        private const uint LUCKOFHEROES_SAVE_BONUS_HASH = 0x390339C3;
        private const int EffectTypeSavingThrow = 3;
        private const int DefaultLuckOfHeroesBonus = 1;
        private const int ExcludeEffectBonus = 0;

        internal delegate sbyte GetWillSavingThrowHook(void* thisPtr, int bExcludeEffectBonus);

        // ReSharper disable once NotAccessedField.Local
        private static GetWillSavingThrowHook _callOriginal;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, int, sbyte> pHook = &OnGetWillSavingThrow;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN17CNWSCreatureStats18GetWillSavingThrowEi");
            var hookPtr = NWNXAPI.RequestFunctionHook(
                functionPtr,
                (IntPtr)pHook,
                -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<GetWillSavingThrowHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static sbyte OnGetWillSavingThrow(void* thisPtr, int bExcludeEffectBonus)
        {
            return ServerManager.Executor.ExecuteInScriptContext(() =>
            {
                var stats = CNWSCreatureStats.FromPointer(thisPtr);
                var effectBonus = CalculateEffectBonus(stats, bExcludeEffectBonus);
                var featModifiers = CalculateFeatModifiers(stats);
                var statBonus = Stat.GetStatAdjustment(stats.m_pBaseCreature.m_idSelf, StatType.WillSavingThrow);

                return CalculateTotal(stats, effectBonus, featModifiers, statBonus);
            });
        }

        private static int CalculateEffectBonus(CNWSCreatureStats stats, int bExcludeEffectBonus)
        {
            if (bExcludeEffectBonus != ExcludeEffectBonus)
                return 0;

            return stats.m_pBaseCreature.GetTotalEffectBonus(
                EffectTypeSavingThrow,
                null,
                0,
                0,
                (int)SavingThrow.Will);
        }

        private static sbyte CalculateFeatModifiers(CNWSCreatureStats stats)
        {
            sbyte modifier = 0;

            modifier += stats.HasFeat((ushort)FeatType.LuckOfHeroes) == 1
                ? (sbyte)NWNXLib.Rules().GetRulesetIntEntry(LUCKOFHEROES_SAVE_BONUS_HASH, DefaultLuckOfHeroesBonus)
                : (sbyte)0;
            modifier += stats.HasFeat((ushort)FeatType.PrestigeDarkBlessing) == 1
                ? (sbyte)stats.m_nCharismaModifier
                : (sbyte)0;

            return modifier;
        }

        private static sbyte CalculateTotal(CNWSCreatureStats stats, int effectBonus, sbyte featModifiers, int statBonus)
        {
            return (sbyte)(stats.m_nWisdomModifier +
                          stats.GetBaseWillSavingThrow() +
                          stats.m_nWillSavingThrowMisc +
                          effectBonus +
                          featModifiers +
                          statBonus);
        }
    }
}
