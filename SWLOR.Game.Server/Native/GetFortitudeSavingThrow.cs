using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SavingThrow = NWN.Native.API.SavingThrow;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetFortitudeSavingThrow
    {
        internal delegate sbyte GetFortitudeSavingThrowHook(void* thisPtr, int bExcludeEffectBonus);

        // ReSharper disable once NotAccessedField.Local
        private static GetFortitudeSavingThrowHook _callOriginal;

        [NWNEventHandler("mod_load")]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, int, sbyte> pHook = &OnGetFortitudeSavingThrow;
            var hookPtr = VM.RequestHook(new IntPtr(FunctionsLinux._ZN17CNWSCreatureStats18GetFortSavingThrowEi), (IntPtr)pHook, -1000001);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<GetFortitudeSavingThrowHook>(hookPtr);
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
                modifier += (sbyte)rules.GetRulesetIntEntry(new CExoString("LUCKOFHEROES_SAVE_BONUS"), 1);

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
