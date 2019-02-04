﻿using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using System;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXEvents : NWNXBase, INWNXEvents
    {
        private readonly INWNXObject _nwnxObject;

        public NWNXEvents(INWScript script, INWNXObject nwnxObject)
            : base(script)
        {
            _nwnxObject = nwnxObject;
        }


        /// <summary>
        /// Scripts can subscribe to events.
        /// Some events are dispatched via the NWNX plugin (see NWNX_EVENTS_EVENT_* constants).
        /// Others can be signalled via script code (see NWNX_Events_SignalEvent).
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="script"></param>
        public void SubscribeEvent(string evt, string script)
        {
            NWNX_PushArgumentString("NWNX_Events", "SUBSCRIBE_EVENT", script);
            NWNX_PushArgumentString("NWNX_Events", "SUBSCRIBE_EVENT", evt);
            NWNX_CallFunction("NWNX_Events", "SUBSCRIBE_EVENT");
        }

        /// <summary>
        /// Pushes event data at the provided tag, which subscribers can access with GetEventData.
        /// This should be called BEFORE SignalEvent.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        public void PushEventData(string tag, string data)
        {
            NWNX_PushArgumentString("NWNX_Events", "PUSH_EVENT_DATA", data);
            NWNX_PushArgumentString("NWNX_Events", "PUSH_EVENT_DATA", tag);
            NWNX_CallFunction("NWNX_Events", "PUSH_EVENT_DATA");
        }

        /// <summary>
        /// Signals an event. This will dispatch a notification to all subscribed handlers.
        /// Returns TRUE if anyone was subscribed to the event, FALSE otherwise.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public int SignalEvent(string evt, NWObject target)
        {
            NWNX_PushArgumentObject("NWNX_Events", "SIGNAL_EVENT", target.Object);
            NWNX_PushArgumentString("NWNX_Events", "SIGNAL_EVENT", evt);
            NWNX_CallFunction("NWNX_Events", "SIGNAL_EVENT");
            return NWNX_GetReturnValueInt("NWNX_Events", "SIGNAL_EVENT");
        }

        /// <summary>
        /// Retrieves the event data for the currently executing script.
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public string GetEventDataString(string tag)
        {
            NWNX_PushArgumentString("NWNX_Events", "GET_EVENT_DATA", tag);
            NWNX_CallFunction("NWNX_Events", "GET_EVENT_DATA");
            return NWNX_GetReturnValueString("NWNX_Events", "GET_EVENT_DATA");
        }


        /// <summary>
        /// // Skips execution of the currently executing event.
        /// If this is a NWNX event, that means that the base function call won't be called.
        /// This won't impact any other subscribers, nor dispatch for before / after functions.
        /// For example, if you are subscribing to NWNX_ON_EXAMINE_OBJECT_BEFORE, and you skip ...
        /// - The other subscribers will still be called.
        /// - The original function in the base game will be skipped.
        /// - The matching after event (NWNX_ON_EXAMINE_OBJECT_AFTER) will also be executed.
        ///
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// ONLY WORKS WITH THE FOLLOWING EVENTS:
        /// - Feat events
        /// - Item events
        /// - Healer's Kit event
        /// - CombatMode events
        /// - Party events
        /// - Skill events
        /// - Map events
        /// - Listen/Spot Detection events
        /// - Polymorph events
        /// </summary>
        public void SkipEvent()
        {
            NWNX_CallFunction("NWNX_Events", "SKIP_EVENT");
        }

        /// <summary>
        /// // Set the return value of the event.
        ///
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// ONLY WORKS WITH THE FOLLOWING EVENTS:
        /// - Healer's Kit event
        /// - Listen/Spot Detection events -> "1" or "0"
        /// </summary>
        /// <param name="data"></param>
        public void SetEventResult(string data)
        {
            NWNX_PushArgumentString("NWNX_Events", "EVENT_RESULT", data);
            NWNX_CallFunction("NWNX_Events", "EVENT_RESULT");
        }

        private int GetEventDataInt(string tag)
        {
            string data = GetEventDataString(tag);
            return Convert.ToInt32(data);
        }

        private bool GetEventDataBoolean(string tag)
        {
            int data = GetEventDataInt(tag);
            return data == 1;
        }

        private float GetEventDataFloat(string tag)
        {
            string data = GetEventDataString(tag);
            return (float)Convert.ToDouble(data);
        }

        private NWObject GetEventDataObject(string tag)
        {
            string data = GetEventDataString(tag);
            return _nwnxObject.StringToObject(data);
        }

        public int OnFeatUsed_GetFeatID()
        {
            return GetEventDataInt("FEAT_ID");
        }

        public int OnFeatUsed_GetSubFeatID()
        {
            return GetEventDataInt("SUBFEAT_ID");
        }

        public NWObject OnFeatUsed_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public NWLocation OnFeatUsed_GetTargetLocation()
        {
            return _.Location(
                    OnFeatUsed_GetArea().Object,
                    _.Vector(OnFeatUsed_GetTargetPositionX(), OnFeatUsed_GetTargetPositionY(), OnFeatUsed_GetTargetPositionZ()),
                    0.0f
            );
        }

        public NWObject OnFeatUsed_GetArea()
        {
            return GetEventDataObject("AREA_OBJECT_ID");
        }

        public float OnFeatUsed_GetTargetPositionX()
        {
            return GetEventDataFloat("TARGET_POSITION_X");
        }

        public float OnFeatUsed_GetTargetPositionY()
        {
            return GetEventDataFloat("TARGET_POSITION_Y");
        }

        public float OnFeatUsed_GetTargetPositionZ()
        {
            return GetEventDataFloat("TARGET_POSITION_Z");
        }

        public NWObject OnItemUsed_GetItem()
        {
            return GetEventDataObject("ITEM_OBJECT_ID");
        }

        public NWObject OnItemUsed_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public int OnItemUsed_GetItemPropertyIndex()
        {
            return GetEventDataInt("ITEM_PROPERTY_INDEX");
        }

        public int OnItemUsed_GetValue2()
        {
            return GetEventDataInt("TEST_VALUE_2");
        }

        public NWObject OnExamineObject_GetTarget()
        {
            return GetEventDataObject("EXAMINEE_OBJECT_ID");
        }

        public int OnCastSpell_GetSpellID()
        {
            return GetEventDataInt("SPELL_ID");
        }

        public int OnCastSpell_GetTargetPositionX()
        {
            return GetEventDataInt("TARGET_POSITION_X");
        }

        public int OnCastSpell_GetTargetPositionY()
        {
            return GetEventDataInt("TARGET_POSITION_Y");
        }

        public int OnCastSpell_GetTargetPositionZ()
        {
            return GetEventDataInt("TARGET_POSITION_Z");
        }

        public NWObject OnCastSpell_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public int OnCastSpell_GetMultiClass()
        {
            return GetEventDataInt("MULTI_CLASS");
        }

        public NWObject OnCastSpell_GetItem()
        {
            return GetEventDataObject("ITEM_OBJECT_ID");
        }

        public bool OnCastSpell_GetSpellCountered()
        {
            return GetEventDataBoolean("SPELL_COUNTERED");
        }

        public bool OnCastSpell_GetCounteringSpell()
        {
            return GetEventDataBoolean("COUNTERING_SPELL");
        }

        public int OnCastSpell_GetProjectilePathType()
        {
            return GetEventDataInt("PROJECTILE_PATH_TYPE");
        }

        public bool OnCastSpell_IsInstantSpell()
        {
            return GetEventDataBoolean("IS_INSTANT_SPELL");
        }

        public NWObject OnCombatRoundStart_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public int OnDMGiveXP_GetAmount()
        {
            return GetEventDataInt("AMOUNT");
        }

        public NWObject OnDMGiveXP_GetTarget()
        {
            return GetEventDataObject("TARGET");
        }

        public int OnDMGiveLevels_GetAmount()
        {
            return GetEventDataInt("NUM_LEVELS");
        }

        public NWObject OnDMGiveLevels_GetTarget()
        {
            return GetEventDataObject("TARGET");
        }

        public int OnDMGiveGold_GetAmount()
        {
            return GetEventDataInt("AMOUNT");
        }

        public NWObject OnDMGiveGold_GetTarget()
        {
            return GetEventDataObject("TARGET");
        }

        /// <summary>
        /// Returns the current event name
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// </summary>
        /// <returns></returns>
        public string GetCurrentEvent()
        {
            NWNX_CallFunction("NWNX_Events", "GET_CURRENT_EVENT");
            return NWNX_GetReturnValueString("NWNX_Events", "GET_CURRENT_EVENT");
        }
    }
}
