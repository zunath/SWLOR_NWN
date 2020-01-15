using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using static SWLOR.Game.Server.NWScript._;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXEvents
    {
        private const string NWNX_Events = "NWNX_Events";

        /// <summary>
        /// Scripts can subscribe to events.
        /// Some events are dispatched via the NWNX plugin (see NWNX_EVENTS_EVENT_* constants).
        /// Others can be signalled via script code (see NWNX_Events_SignalEvent).
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="script"></param>
        public static void SubscribeEvent(string evt, string script)
        {
            string sFunc = "SubscribeEvent";

            NWNX_PushArgumentString(NWNX_Events, sFunc, script);
            NWNX_PushArgumentString(NWNX_Events, sFunc, evt);
            NWNX_CallFunction(NWNX_Events, sFunc);
        }

        /// <summary>
        /// Pushes event data at the provided tag, which subscribers can access with GetEventData.
        /// This should be called BEFORE SignalEvent.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        public static void PushEventData(string tag, string data)
        {
            string sFunc = "PushEventData";

            NWNX_PushArgumentString(NWNX_Events, sFunc, data);
            NWNX_PushArgumentString(NWNX_Events, sFunc, tag);
            NWNX_CallFunction(NWNX_Events, sFunc);
        }

        /// <summary>
        /// Signals an event. This will dispatch a notification to all subscribed handlers.
        /// Returns true if anyone was subscribed to the event, false otherwise.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int SignalEvent(string evt, NWGameObject target)
        {
            string sFunc = "SignalEvent";

            NWNX_PushArgumentObject(NWNX_Events, sFunc, target);
            NWNX_PushArgumentString(NWNX_Events, sFunc, evt);
            NWNX_CallFunction(NWNX_Events, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Events, sFunc);
        }

        /// <summary>
        /// Retrieves the event data for the currently executing script.
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static string GetEventDataString(string tag)
        {
            string sFunc = "GetEventData";

            NWNX_PushArgumentString(NWNX_Events, sFunc, tag);
            NWNX_CallFunction(NWNX_Events, sFunc);
            return NWNX_GetReturnValueString(NWNX_Events, sFunc);
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
            string sFunc = "SkipEvent";

            NWNX_CallFunction(NWNX_Events, sFunc);
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
            string sFunc = "SetEventResult";

            NWNX_PushArgumentString(NWNX_Events, sFunc, data);
            NWNX_CallFunction(NWNX_Events, sFunc);
        }

        /// <summary>
        /// Returns the current event name
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentEvent()
        {
            string sFunc = "GetCurrentEvent";

            NWNX_CallFunction(NWNX_Events, sFunc);
            return NWNX_GetReturnValueString(NWNX_Events, sFunc);
        }

        /// <summary>
        /// Toggles DispatchListMode for sEvent+sScript
        /// If enabled, sEvent for sScript will only be signalled if the target object is on its dispatch list.
        /// </summary>
        /// <param name="sEvent"></param>
        /// <param name="sScript"></param>
        /// <param name="bEnable"></param>
        public static void ToggleDispatchListMode(string sEvent, string sScript, int bEnable)
        {
            string sFunc = "ToggleDispatchListMode";

            NWNX_PushArgumentInt(NWNX_Events, sFunc, bEnable);
            NWNX_PushArgumentString(NWNX_Events, sFunc, sScript);
            NWNX_PushArgumentString(NWNX_Events, sFunc, sEvent);
            NWNX_CallFunction(NWNX_Events, sFunc);
        }

        /// <summary>
        /// Add oObject to the dispatch list for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent"></param>
        /// <param name="sScript"></param>
        /// <param name="oObject"></param>
        public static void AddObjectToDispatchList(string sEvent, string sScript, NWGameObject oObject)
        {
            string sFunc = "AddObjectToDispatchList";

            NWNX_PushArgumentObject(NWNX_Events, sFunc, oObject);
            NWNX_PushArgumentString(NWNX_Events, sFunc, sScript);
            NWNX_PushArgumentString(NWNX_Events, sFunc, sEvent);
            NWNX_CallFunction(NWNX_Events, sFunc);
        }

        /// <summary>
        /// Remove oObject from the dispatch list for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent"></param>
        /// <param name="sScript"></param>
        /// <param name="oObject"></param>
        public static void RemoveObjectFromDispatchList(string sEvent, string sScript, NWGameObject oObject)
        {
            string sFunc = "RemoveObjectFromDispatchList";

            NWNX_PushArgumentObject(NWNX_Events, sFunc, oObject);
            NWNX_PushArgumentString(NWNX_Events, sFunc, sScript);
            NWNX_PushArgumentString(NWNX_Events, sFunc, sEvent);
            NWNX_CallFunction(NWNX_Events, sFunc);
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

        private static NWGameObject GetEventDataObject(string tag)
        {
            string data = GetEventDataString(tag);
            return NWNXObject.StringToObject(data);
        }

        // The following methods are specific to our implementation which makes the API a little easier to use.
        // Pattern is: "Event_Action()"

        public static Feat OnFeatUsed_GetFeat()
        {
            return (Feat)GetEventDataInt("FEAT_ID");
        }

        public static int OnFeatUsed_GetSubFeatID()
        {
            return GetEventDataInt("SUBFEAT_ID");
        }

        public static NWGameObject OnFeatUsed_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static Location OnFeatUsed_GetTargetLocation()
        {
            return Location(
                    OnFeatUsed_GetArea(),
                    Vector(OnFeatUsed_GetTargetPositionX(), OnFeatUsed_GetTargetPositionY(), OnFeatUsed_GetTargetPositionZ()),
                    0.0f
            );
        }

        public static NWGameObject OnFeatUsed_GetArea()
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

        public static NWGameObject OnItemUsed_GetItem()
        {
            return GetEventDataObject("ITEM_OBJECT_ID");
        }

        public static NWGameObject OnItemUsed_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static Location OnItemUsed_GetTargetLocation()
        {
            NWGameObject user = NWGameObject.OBJECT_SELF;
            var x = GetEventDataFloat("TARGET_POSITION_X");
            var y = GetEventDataFloat("TARGET_POSITION_Y");
            var z = GetEventDataFloat("TARGET_POSITION_Z");
            var vector = Vector(x, y, z);

            return Location(GetArea(user), vector, 0.0f);
        }

        public static int OnItemUsed_GetItemPropertyIndex()
        {
            return GetEventDataInt("ITEM_PROPERTY_INDEX");
        }

        public static int OnItemUsed_GetValue2()
        {
            return GetEventDataInt("TEST_VALUE_2");
        }

        public static NWGameObject OnExamineObject_GetTarget()
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

        public static NWGameObject OnCastSpell_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static int OnCastSpell_GetMultiClass()
        {
            return GetEventDataInt("MULTI_CLASS");
        }

        public static NWGameObject OnCastSpell_GetItem()
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

        public static NWGameObject OnCombatRoundStart_GetTarget()
        {
            return GetEventDataObject("TARGET_OBJECT_ID");
        }

        public static int OnDMGiveXP_GetAmount()
        {
            return GetEventDataInt("AMOUNT");
        }

        public static NWGameObject OnDMGiveXP_GetTarget()
        {
            return GetEventDataObject("OBJECT");
        }

        public static int OnDMGiveLevels_GetAmount()
        {
            return GetEventDataInt("AMOUNT");
        }

        public static NWGameObject OnDMGiveLevels_GetTarget()
        {
            return GetEventDataObject("OBJECT");
        }

        public static int OnDMGiveGold_GetAmount()
        {
            return GetEventDataInt("AMOUNT");
        }

        public static NWGameObject OnDMGiveGold_GetTarget()
        {
            return GetEventDataObject("OBJECT");
        }

        public static NWGameObject OnDMSpawnObject_GetArea()
        {
            return GetEventDataObject("AREA");
        }
        
        public static NWGameObject OnDMSpawnObject_GetObject()
        {
            return GetEventDataObject("OBJECT");
        }

        public static ObjectType OnDMSpawnObject_GetObjectType()
        {
            // For whatever reason, NWNX uses different object type IDs from standard NWN.
            // I don't want to deal with this nonsense so we'll convert to the correct IDs here.
            int nwnxObjectTypeID = GetEventDataInt("OBJECT_TYPE");

            switch (nwnxObjectTypeID)
            {
                case 5: return ObjectType.Creature;
                case 6: return ObjectType.Item;
                case 7: return ObjectType.Trigger;
                case 9: return ObjectType.Placeable;
                case 12: return ObjectType.Waypoint;
                case 13: return ObjectType.Encounter;
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

        public static GameDifficulty OnDMChangeDifficulty_GetDifficultySetting()
        {
            return (GameDifficulty) GetEventDataInt("DIFFICULTY_SETTING");
        }

        public static NWGameObject OnDMDisableTrap_GetTrap()
        {
            return GetEventDataObject("TARGET");
        }

        public static List<NWGameObject> DMEvents_GetTargetList(string tagPrefix = "TARGET_")
        {
            var targetCount = GetEventDataInt("NUM_TARGETS");
            var result = new List<NWGameObject>();

            for (int x = 1; x <= targetCount; x++)
            {
                var target = GetEventDataObject(tagPrefix + x);
                result.Add(target);
            }

            return result;
        }

        public static NWGameObject OnDMGiveItem_GetTarget()
        {
            return GetEventDataObject("TARGET");
        }

        public static NWGameObject OnDMGiveItem_GetItem()
        {
            return GetEventDataObject("ITEM");
        }

        public static NWGameObject OnDMJumpToPoint_GetArea()
        {
            return GetEventDataObject("TARGET_AREA");
        }

        public static float OnDMJumpToPoint_GetX()
        {
            return GetEventDataFloat("POS_X");
        }

        public static float OnDMJumpToPoint_GetY()
        {
            return GetEventDataFloat("POS_Y");
        }

        public static float OnDMJumpToPoint_GetZ()
        {
            return GetEventDataFloat("POS_Z");
        }

        public static NWGameObject OnDMPossess_GetTarget()
        {
            return GetEventDataObject("TARGET");
        }

        public static NWGameObject OnInventoryAddItem_GetItem()
        {
            return GetEventDataObject("ITEM");
        }

        public static NWGameObject OnInventoryAddItem_GetPlayer()
        {
            var item = OnInventoryAddItem_GetItem();
            var player = GetItemPossessor(item);

            return player;
        }

        public static NWGameObject OnEquipItem_GetItem()
        {
            return GetEventDataObject("ITEM");
        }

        public static InventorySlot OnEquipItem_GetInventorySlot()
        {
            return (InventorySlot)GetEventDataInt("SLOT");
        }

    }
}
