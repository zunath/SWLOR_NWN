using NWN.Native.API;
using NWNX.NET;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using System;
using System.Runtime.InteropServices;
using SavingThrow = NWN.Native.API.SavingThrow;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetFortitudeSavingThrow
    {
        // Hash constants for ruleset entries (computed using djb2 hash algorithm)
        private const uint LUCKOFHEROES_SAVE_BONUS_HASH = 0x390339C3; // djb2 hash of "LUCKOFHEROES_SAVE_BONUS"
        internal delegate sbyte GetFortitudeSavingThrowHook(void* thisPtr, int bExcludeEffectBonus);

        // ReSharper disable once NotAccessedField.Local
        private static GetFortitudeSavingThrowHook _callOriginal;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
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
            var stats = CNWSCreatureStats.FromPointer(thisPtr);
            var rules = NWNXLib.Rules();

            var effectBonus = 0;
            sbyte modifier = 0;

            if (bExcludeEffectBonus == 0)
                effectBonus = stats.m_pBaseCreature
                    .GetTotalEffectBonus(3, // 3 = EFFECT_TYPE_SAVING_THROW
                        null, 
                        0, 
                        0, 
                        (int)SavingThrow.Fortitude);

            if (stats.HasFeat((ushort)FeatType.LuckOfHeroes) == 1)
                modifier += (sbyte)rules.GetRulesetIntEntry(LUCKOFHEROES_SAVE_BONUS_HASH, 1);

            if (stats.HasFeat((ushort)FeatType.PrestigeDarkBlessing) == 1)
                modifier += (sbyte)stats.m_nCharismaModifier;

            return (sbyte)(stats.m_nStrengthModifier + 
                           stats.GetBaseFortSavingThrow() + 
                           stats.m_nFortSavingThrowMisc + 
                           effectBonus + 
                           modifier);
        }
    }
}
