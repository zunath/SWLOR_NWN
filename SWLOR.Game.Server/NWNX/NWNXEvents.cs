using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.NWNX.Contracts;

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


        // Scripts can subscribe to events.
        // Some events are dispatched via the NWNX plugin (see NWNX_EVENTS_EVENT_* constants).
        // Others can be signalled via script code (see NWNX_Events_SignalEvent).
        public void SubscribeEvent(string evt, string script)
        {
            NWNX_PushArgumentString("NWNX_Events", "SUBSCRIBE_EVENT", script);
            NWNX_PushArgumentString("NWNX_Events", "SUBSCRIBE_EVENT", evt);
            NWNX_CallFunction("NWNX_Events", "SUBSCRIBE_EVENT");
        }

        // Pushes event data at the provided tag, which subscribers can access with GetEventData.
        // This should be called BEFORE SignalEvent.
        public void PushEventData(string tag, string data)
        {
            NWNX_PushArgumentString("NWNX_Events", "PUSH_EVENT_DATA", data);
            NWNX_PushArgumentString("NWNX_Events", "PUSH_EVENT_DATA", tag);
            NWNX_CallFunction("NWNX_Events", "PUSH_EVENT_DATA");
        }

        // Signals an event. This will dispatch a notification to all subscribed handlers.
        // Returns TRUE if anyone was subscribed to the event, FALSE otherwise.
        public int SignalEvent(string evt, NWObject target)
        {
            NWNX_PushArgumentObject("NWNX_Events", "SIGNAL_EVENT", target.Object);
            NWNX_PushArgumentString("NWNX_Events", "SIGNAL_EVENT", evt);
            NWNX_CallFunction("NWNX_Events", "SIGNAL_EVENT");
            return NWNX_GetReturnValueInt("NWNX_Events", "SIGNAL_EVENT");
        }

        // Retrieves the event data for the currently executing script.
        // THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        public string GetEventDataString(string tag)
        {
            NWNX_PushArgumentString("NWNX_Events", "GET_EVENT_DATA", tag);
            NWNX_CallFunction("NWNX_Events", "GET_EVENT_DATA");
            return NWNX_GetReturnValueString("NWNX_Events", "GET_EVENT_DATA");
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

        public Location OnFeatUsed_GetTargetLocation()
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

    }
}
