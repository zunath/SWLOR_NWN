using NWN;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWNX.NWNXCore;
using System;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXEvents
    {
        /// <summary>
        /// Scripts can subscribe to events.
        /// Some events are dispatched via the NWNX plugin (see NWNX_EVENTS_EVENT_* constants).
        /// Others can be signalled via script code (see NWNX_Events_SignalEvent).
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="script"></param>
        public static void SubscribeEvent(string evt, string script)
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
        public static void PushEventData(string tag, string data)
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
        public static int SignalEvent(string evt, NWObject target)
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
        public static string GetEventDataString(string tag)
        {
            NWNX_PushArgumentString("NWNX_Events", "GET_EVENT_DATA", tag);
            NWNX_CallFunction("NWNX_Events", "GET_EVENT_DATA");
            return NWNX_GetReturnValueString("NWNX_Events", "GET_EVENT_DATA");
        }


        /// <summary>
        /// 
        /// Skips execution of the currently executing event.
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
        /// - DMAction events
        /// - Client connect event
        /// - Spell events
        /// - QuickChat events
        /// - Barter event (START only)
        /// - Trap events
        /// - Sticky Player Name event
        /// </summary>
        public static void SkipEvent()
        {
            NWNX_CallFunction("NWNX_Events", "SKIP_EVENT");
        }

        /// <summary>
        /// Set the return value of the event.
        ///
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// ONLY WORKS WITH THE FOLLOWING EVENTS:
        /// - Healer's Kit event
        /// - Listen/Spot Detection events -> "1" or "0"
        /// - OnClientConnectBefore -> Reason for disconnect if skipped
        /// - Ammo Reload event -> Forced ammunition returned
        /// - Trap events -> "1" or "0"
        /// - Sticky Player Name event -> "1" or "0"
        /// </summary>
        /// <param name="data"></param>
        public static void SetEventResult(string data)
        {
            NWNX_PushArgumentString("NWNX_Events", "EVENT_RESULT", data);
            NWNX_CallFunction("NWNX_Events", "EVENT_RESULT");
        }

        /// <summary>
        /// Returns the current event name
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentEvent()
        {
            NWNX_CallFunction("NWNX_Events", "GET_CURRENT_EVENT");
            return NWNX_GetReturnValueString("NWNX_Events", "GET_CURRENT_EVENT");
        }

        private static int GetEventDataInt(string tag)
        {
            string data = GetEventDataString(tag);
            return Convert.ToInt32(data);
        }

        private static bool GetEventDataBoolean(string tag)
        {
            int data = GetEventDataInt(tag);
            return data == 1;
        }

        private static float GetEventDataFloat(string tag)
        {
            string data = GetEventDataString(tag);
            return (float)Convert.ToDouble(data);
        }

        private static NWObject GetEventDataObject(string tag)
        {
            string data = GetEventDataString(tag);
            return NWNXObject.StringToObject(data);
        }

        // The following methods are specific to our implementation which makes the API a little easier to use.
        // Pattern is: "Event_Action()"

        public static int OnFeatUsed_GetFeatID()
        {
            return GetEventDataInt("FEAT_ID");
        }

        public static int OnFeatUsed_GetSubFeatID()
        {
            return GetEventDataInt("SUBFEAT_ID");
        }

        public static NWObject OnFeatUsed_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static NWLocation OnFeatUsed_GetTargetLocation()
        {
            return _.Location(
                    OnFeatUsed_GetArea().Object,
                    _.Vector(OnFeatUsed_GetTargetPositionX(), OnFeatUsed_GetTargetPositionY(), OnFeatUsed_GetTargetPositionZ()),
                    0.0f
            );
        }

        public static NWObject OnFeatUsed_GetArea()
        {
            return GetEventDataObject("AREA_OBJECT_ID");
        }

        public static float OnFeatUsed_GetTargetPositionX()
        {
            return GetEventDataFloat("TARGET_POSITION_X");
        }

        public static float OnFeatUsed_GetTargetPositionY()
        {
            return GetEventDataFloat("TARGET_POSITION_Y");
        }

        public static float OnFeatUsed_GetTargetPositionZ()
        {
            return GetEventDataFloat("TARGET_POSITION_Z");
        }

        public static NWObject OnItemUsed_GetItem()
        {
            return GetEventDataObject("ITEM_OBJECT_ID");
        }

        public static NWObject OnItemUsed_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static int OnItemUsed_GetItemPropertyIndex()
        {
            return GetEventDataInt("ITEM_PROPERTY_INDEX");
        }

        public static int OnItemUsed_GetValue2()
        {
            return GetEventDataInt("TEST_VALUE_2");
        }

        public static NWObject OnExamineObject_GetTarget()
        {
            return GetEventDataObject("EXAMINEE_OBJECT_ID");
        }

        public static int OnCastSpell_GetSpellID()
        {
            return GetEventDataInt("SPELL_ID");
        }

        public static int OnCastSpell_GetTargetPositionX()
        {
            return GetEventDataInt("TARGET_POSITION_X");
        }

        public static int OnCastSpell_GetTargetPositionY()
        {
            return GetEventDataInt("TARGET_POSITION_Y");
        }

        public static int OnCastSpell_GetTargetPositionZ()
        {
            return GetEventDataInt("TARGET_POSITION_Z");
        }

        public static NWObject OnCastSpell_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static int OnCastSpell_GetMultiClass()
        {
            return GetEventDataInt("MULTI_CLASS");
        }

        public static NWObject OnCastSpell_GetItem()
        {
            return GetEventDataObject("ITEM_OBJECT_ID");
        }

        public static bool OnCastSpell_GetSpellCountered()
        {
            return GetEventDataBoolean("SPELL_COUNTERED");
        }

        public static bool OnCastSpell_GetCounteringSpell()
        {
            return GetEventDataBoolean("COUNTERING_SPELL");
        }

        public static int OnCastSpell_GetProjectilePathType()
        {
            return GetEventDataInt("PROJECTILE_PATH_TYPE");
        }

        public static bool OnCastSpell_IsInstantSpell()
        {
            return GetEventDataBoolean("IS_INSTANT_SPELL");
        }

        public static NWObject OnCombatRoundStart_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static int OnDMGiveXP_GetAmount()
        {
            return GetEventDataInt("AMOUNT");
        }

        public static NWObject OnDMGiveXP_GetTarget()
        {
            return GetEventDataObject("OBJECT");
        }

        public static int OnDMGiveLevels_GetAmount()
        {
            return GetEventDataInt("NUM_LEVELS");
        }

        public static NWObject OnDMGiveLevels_GetTarget()
        {
            return GetEventDataObject("OBJECT");
        }

        public static int OnDMGiveGold_GetAmount()
        {
            return GetEventDataInt("AMOUNT");
        }

        public static NWObject OnDMGiveGold_GetTarget()
        {
            return GetEventDataObject("OBJECT");
        }

        public static NWArea OnDMSpawnObject_GetArea()
        {
            return GetEventDataObject("AREA").Object;
        }
        
        public static NWObject OnDMSpawnObject_GetObject()
        {
            return GetEventDataObject("OBJECT");
        }

        public static int OnDMSpawnObject_GetObjectType()
        {
            // For whatever reason, NWNX uses different object type IDs from standard NWN.
            // I don't want to deal with this nonsense so we'll convert to the correct IDs here.
            int nwnxObjectTypeID = GetEventDataInt("OBJECT_TYPE");

            switch (nwnxObjectTypeID)
            {
                case 5: return _.OBJECT_TYPE_CREATURE;
                case 6: return _.OBJECT_TYPE_ITEM;
                case 7: return _.OBJECT_TYPE_TRIGGER;
                case 9: return _.OBJECT_TYPE_PLACEABLE;
                case 12: return _.OBJECT_TYPE_WAYPOINT;
                case 13: return _.OBJECT_TYPE_ENCOUNTER;
                case 15: return 15; // Only exception are portals, whatever those are!
            }

            throw new Exception("Invalid object type: " + nwnxObjectTypeID);
        }

        public static float OnDMSpawnObject_GetPositionX()
        {
            return GetEventDataFloat("POS_X");
        }
        public static float OnDMSpawnObject_GetPositionY()
        {
            return GetEventDataFloat("POS_Y");
        }
        public static float OnDMSpawnObject_GetPositionZ()
        {
            return GetEventDataFloat("POS_Z");
        }
    }
}
