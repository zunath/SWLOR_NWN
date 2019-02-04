﻿using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXEvents
    {
        void SubscribeEvent(string evt, string script);
        void PushEventData(string tag, string data);
        int SignalEvent(string evt, NWObject target);
        string GetEventDataString(string tag);
        int OnFeatUsed_GetFeatID();
        int OnFeatUsed_GetSubFeatID();
        NWObject OnFeatUsed_GetTarget();
        NWLocation OnFeatUsed_GetTargetLocation();
        NWObject OnFeatUsed_GetArea();
        float OnFeatUsed_GetTargetPositionX();
        float OnFeatUsed_GetTargetPositionY();
        float OnFeatUsed_GetTargetPositionZ();
        NWObject OnItemUsed_GetItem();
        NWObject OnItemUsed_GetTarget();
        int OnItemUsed_GetItemPropertyIndex();
        int OnItemUsed_GetValue2();
        NWObject OnExamineObject_GetTarget();
        int OnCastSpell_GetSpellID();
        int OnCastSpell_GetTargetPositionX();
        int OnCastSpell_GetTargetPositionY();
        int OnCastSpell_GetTargetPositionZ();
        NWObject OnCastSpell_GetTarget();
        int OnCastSpell_GetMultiClass();
        NWObject OnCastSpell_GetItem();
        bool OnCastSpell_GetSpellCountered();
        bool OnCastSpell_GetCounteringSpell();
        int OnCastSpell_GetProjectilePathType();
        bool OnCastSpell_IsInstantSpell();
        NWObject OnCombatRoundStart_GetTarget();
        void SkipEvent();
        void SetEventResult(string data);

        int OnDMGiveXP_GetAmount();
        NWObject OnDMGiveXP_GetTarget();
        int OnDMGiveLevels_GetAmount();
        NWObject OnDMGiveLevels_GetTarget();
        int OnDMGiveGold_GetAmount();
        NWObject OnDMGiveGold_GetTarget();
        string GetCurrentEvent();
    }
}