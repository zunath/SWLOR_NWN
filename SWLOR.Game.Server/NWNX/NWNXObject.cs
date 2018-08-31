using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXObject : NWNXBase, INWNXObject
    {
        public NWNXObject(INWScript script)
            : base(script)
        {
        }


        private const string NWNX_Object = "NWNX_Object";

        // Gets the count of all local variables on the provided object.
        public int GetLocalVariableCount(NWObject obj)
        {
            string sFunc = "GetLocalVariableCount";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);
            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc);
        }

        //Gets the local variable at the provided index of the provided object.
        // Index bounds: 0 >= index < GetLocalVariableCount(obj).
        public LocalVariable GetLocalVariable(NWObject obj, int index)
        {
            string sFunc = "GetLocalVariable";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);
            NWNX_CallFunction(NWNX_Object, sFunc);

            LocalVariable var = new LocalVariable();
            var.Key = NWNX_GetReturnValueString(NWNX_Object, sFunc);
            var.Type = (LocalVariableType)NWNX_GetReturnValueInt(NWNX_Object, sFunc);
            return var;
        }

        // Returns an NWObject from the provided NWObject ID.
        // This is the counterpart to ObjectToString.
        public NWObject StringToObject(string id)
        {
            string sFunc = "StringToObject";

            NWNX_PushArgumentString(NWNX_Object, sFunc, id);
            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWObject.Wrap(NWNX_GetReturnValueObject(NWNX_Object, sFunc));
        }

        // Set the provided object's position to the provided vector.
        public void SetPosition(NWObject obj, Vector pos)
        {
            string sFunc = "SetPosition";

            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_X);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Y);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Z);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);
            NWNX_CallFunction(NWNX_Object, sFunc);

        }

        // Sets the provided object's current hit points to the provided value.
        public void SetCurrentHitPoints(NWCreature creature, int hp)
        {
            string sFunc = "SetCurrentHitPoints";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, hp);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        // Set object's maximum hit points; will not work on PCs.
        public void SetMaxHitPoints(NWObject creature, int hp)
        {
            string sFunc = "SetMaxHitPoints";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, hp);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        // Get the name of the portrait NWObject is using.
        public string GetPortrait(NWObject creature)
        {
            string sFunc = "GetPortrait";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        // Set the portrait NWObject is using. The portrait String must be no more than 15 characters long.
        public void SetPortrait(NWObject creature, string portrait)
        {
            string sFunc = "SetPortrait";

            NWNX_PushArgumentString(NWNX_Object, sFunc, portrait);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }


        // Serialize the full NWObject (including locals, inventory, etc) to base64 string
        // Only works on Creatures and Items currently.
        public string Serialize(Object obj)
        {
            string sFunc = "Serialize";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        // Deserialize the object. The NWObject will be created outside of the world and
        // needs to be manually positioned at a location/inventory.
        public Object Deserialize(string serialized)
        {
            string sFunc = "Deserialize";

            NWNX_PushArgumentString(NWNX_Object, sFunc, serialized);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueObject(NWNX_Object, sFunc);
        }


        // Returns the dialog resref of the object.
        public string GetDialogResref(NWObject obj)
        {
            string sFunc = "GetDialogResref";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        // Set obj's appearance. Will not update for PCs until they
        // re-enter the area.
        public void SetAppearance(NWObject obj, int app)
        {
            string sFunc = "SetAppearance";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, app);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        // Get obj's appearance
        public int GetAppearance(NWObject obj)
        {
            string sFunc = "GetAppearance";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Object, sFunc);
        }

    }
}
