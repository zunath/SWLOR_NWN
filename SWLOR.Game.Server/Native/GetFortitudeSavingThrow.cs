using NWN.Native.API;
using NWNX.NET;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using System;
using System.Runtime.InteropServices;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Server;
using SavingThrow = NWN.Native.API.SavingThrow;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetFortitudeSavingThrow
    {
        // Hash constants for ruleset entries (computed using djb2 hash algorithm)
        private const uint LUCKOFHEROES_SAVE_BONUS_HASH = 0x390339C3; // djb2 hash of "LUCKOFHEROES_SAVE_BONUS"

        // Effect type constants
        private const int EffectTypeSavingThrow = 3;

        // Default values
        private const int DefaultLuckOfHeroesBonus = 1;
        private const int ExcludeEffectBonus = 0;
        internal delegate sbyte GetFortitudeSavingThrowHook(void* thisPtr, int bExcludeEffectBonus);

        // ReSharper disable once NotAccessedField.Local
        private static GetFortitudeSavingThrowHook _callOriginal;

        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, int, sbyte> pHook = &OnGetFortitudeSavingThrow;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN17CNWSCreatureStats18GetFortSavingThrowEi");
            var hookPtr = NWNXAPI.RequestFunctionHook(
                functionPtr,
                (IntPtr)pHook,
                -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<GetFortitudeSavingThrowHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static sbyte OnGetFortitudeSavingThrow(void* thisPtr, int bExcludeEffectBonus)
        {
            return ServerManager.Executor.ExecuteInScriptContext(() =>
            {
                var stats = CNWSCreatureStats.FromPointer(thisPtr);

                var effectBonus = CalculateEffectBonus(stats, bExcludeEffectBonus);
                var featModifiers = CalculateFeatModifiers(stats);

                return CalculateTotal(stats, effectBonus, featModifiers);
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
                (int)SavingThrow.Fortitude);
        }

        private static sbyte CalculateFeatModifiers(CNWSCreatureStats stats)
        {
            sbyte modifier = 0;

            modifier += CalculateLuckOfHeroesBonus(stats);
            modifier += CalculatePrestigeDarkBlessingBonus(stats);

            return modifier;
        }

        private static sbyte CalculateLuckOfHeroesBonus(CNWSCreatureStats stats)
        {
            return stats.HasFeat((ushort)FeatType.LuckOfHeroes) == 1
                ? (sbyte)NWNXLib.Rules().GetRulesetIntEntry(LUCKOFHEROES_SAVE_BONUS_HASH, DefaultLuckOfHeroesBonus)
                : (sbyte)0;
        }

        private static sbyte CalculatePrestigeDarkBlessingBonus(CNWSCreatureStats stats)
        {
            return stats.HasFeat((ushort)FeatType.PrestigeDarkBlessing) == 1
                ? (sbyte)stats.m_nCharismaModifier
                : (sbyte)0;
        }

        private static sbyte CalculateTotal(CNWSCreatureStats stats, int effectBonus, sbyte featModifiers)
        {
            return (sbyte)(stats.m_nStrengthModifier +
                          stats.GetBaseFortSavingThrow() +
                          stats.m_nFortSavingThrowMisc +
                          effectBonus +
                          featModifiers);
        }
    }
}
