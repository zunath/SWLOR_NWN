using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class ResolveAttackRoll
    {
        internal delegate void ResolveAttackRollHook(void* thisPtr, void* pTarget);

        // ReSharper disable once NotAccessField.Local
        private static ResolveAttackRollHook _callOriginal;

        [NWNEventHandler("mod_load")]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, void> pHook = &OnResolveAttackRoll;

            var hookPtr = Internal.NativeFunctions.RequestHook(new IntPtr(FunctionsLinux._ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject), (IntPtr)pHook, -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<ResolveAttackRollHook>(hookPtr);
        }

        [UnmanagedCallersOnly]
        private static void OnResolveAttackRoll(void* thisPtr, void* pTarget)
        {
            Console.WriteLine("Running OnResolveAttackRoll");
            var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
            var attacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);
            var targetObject = CNWSObject.FromPointer(pTarget);

            CNWSCreatureStats targetStats = null;
            if (targetObject.m_nObjectType == (int)ObjectType.Creature)
            {
                targetStats = CNWSCreatureStats.FromPointer(pTarget);
            }
        }
    }
}

/**
 *         if (!pTarget)
            return;

        CNWSCombatRound *pCombatRound = pThis->m_pcCombatRound;
        CNWSCombatAttackData *pAttackData = pCombatRound->GetAttack(pCombatRound->m_nCurrentAttack);
        int32_t nAttackRoll = Globals::Rules()->RollDice(1, 20);

        // DEBUG
        if (Globals::EnableCombatDebugging())
        {
            pAttackData->m_sAttackDebugText.Format("%s Attack Roll: %d", pThis->m_pStats->GetFullName().CStr(), nAttackRoll);
        }
        // /////

        CNWSCreature *pCreature = Utils::AsNWSCreature(pTarget);
        int32_t nAttackRollModifier, nArmorClass;

        if (pCreature)
        {
            nAttackRollModifier = pThis->m_pStats->GetAttackModifierVersus(pCreature);
            nArmorClass = pCreature->m_pStats->GetArmorClassVersus(pThis);
        }
        else
        {
            nAttackRollModifier = pThis->m_pStats->GetAttackModifierVersus();
            nArmorClass = 0;
        }

        // DEBUG
        if (Globals::EnableCombatDebugging())
        {
            CExoString sCurrent = pAttackData->m_sAttackDebugText;
            CExoString sAdd;

            sAdd.Format(" Versus AC %d", nArmorClass);
            pAttackData->m_sAttackDebugText = sCurrent + sAdd;
        }
        // /////

        if (pCreature)
        {
            pThis->ResolveSneakAttack(pCreature);
            pThis->ResolveDeathAttack(pCreature);
        }

        if (pAttackData->m_bCoupDeGrace)
        {
            pAttackData->m_nToHitRoll = 20;
            pAttackData->m_nToHitMod = nAttackRollModifier;
            pAttackData->m_nAttackResult = 7;//Automatic Hit
return;
        }

        pAttackData->m_nToHitRoll = nAttackRoll;
pAttackData->m_nToHitMod = nAttackRollModifier;

if (pThis->ResolveDefensiveEffects(pTarget, (nAttackRoll + nAttackRollModifier >= nArmorClass)))
    return;

// Parry Check
if (pCreature)
{
    if (nAttackRoll != 20)
    {
        if (pCreature->m_nCombatMode == Constants::CombatMode::Parry &&
            pCreature->m_pcCombatRound->m_nParryActions > 0 &&
            !pCreature->m_pcCombatRound->m_bRoundPaused &&
            pCreature->m_nState != 6  && // Stunned
            !pAttackData->m_bRangedAttack &&
            !pCreature->GetRangeWeaponEquipped())
        {
            static int32_t nParryRiposteDifference = Globals::Rules()->GetRulesetIntEntry("PARRY_RIPOSTE_DIFFERENCE", 10);
            int32_t nParryRoll = Globals::Rules()->RollDice(1, 20) +
                    pCreature->m_pStats->GetSkillRank(Constants::Skill::Parry, Utils::AsNWSObject(pThis));

            if (nParryRoll >= nAttackRoll + nAttackRollModifier)
            {
                if (nParryRoll - nParryRiposteDifference >= nAttackRoll + nAttackRollModifier)
                {
                    pCreature->m_pcCombatRound->AddParryAttack(pThis->m_idSelf);
                }

                pAttackData->m_nAttackResult = 2;//Parried
                pCreature->m_pcCombatRound->m_nParryActions--;
                return;
            }

            pCreature->m_pcCombatRound->AddParryIndex();
            pCreature->m_pcCombatRound->m_nParryActions--;
        }
    }
}

if (nAttackRoll != 1)
{
    if ((nAttackRoll + nAttackRollModifier >= nArmorClass) || nAttackRoll == 20)
    {
        if (nAttackRoll >= pThis->m_pStats->GetCriticalHitRoll(pCombatRound->GetOffHandAttack()))
        {
            int32_t nCriticalHitRoll = Globals::Rules()->RollDice(1, 20);

            pAttackData->m_bCriticalThreat = true;
            pAttackData->m_nThreatRoll = nCriticalHitRoll;

            if (nCriticalHitRoll + nAttackRollModifier >= nArmorClass)
            {
                if (pCreature)
                {
                    if (Globals::AppManager()->m_pServerExoApp->GetDifficultyOption(0)) //No Critical Hits On PCs
{
    if (pCreature->m_bPlayerCharacter && !pThis->m_bPlayerCharacter)
                        {
                            pAttackData->m_nAttackResult = 1;//Successful Hit
                            return;
                        }
                    }

                    if (pCreature->m_pStats->GetEffectImmunity(Constants::ImmunityType::CriticalHit, pThis))
                    {
                        auto* pData = new CNWCCMessageData;
                        pData->SetObjectID(0, pCreature->m_idSelf);
                        pData->SetInteger(0, 126); //Critical Hit Immunity Feedback

pAttackData->m_alstPendingFeedback.Add(pData);
                        pAttackData->m_nAttackResult = 1;//Successful Hit
                        return;
                    }
                }

                pAttackData->m_nAttackResult = 3;//Critical Hit
                return;
            }
        }

        pAttackData->m_nAttackResult = 1;//Successful Hit
        return;
    }
}

pAttackData->m_nAttackResult = 4;//Miss

if (nAttackRoll == 1)
    pAttackData->m_nMissedBy = 1;
else
    pAttackData->m_nMissedBy = std::abs(nAttackRoll + nAttackRollModifier - nArmorClass);

*/