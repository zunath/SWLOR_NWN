using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
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

        /// <summary>
        /// Gets the count of all local variables on the provided object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetLocalVariableCount(NWObject obj)
        {
            string sFunc = "GetLocalVariableCount";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);
            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Gets the local variable at the provided index of the provided object.
        /// Index bounds: 0 >= index "less than" GetLocalVariableCount(obj).
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="index"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns an NWObject from the provided NWObject ID.
        /// This is the counterpart to ObjectToString.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public NWObject StringToObject(string id)
        {
            string sFunc = "StringToObject";

            NWNX_PushArgumentString(NWNX_Object, sFunc, id);
            NWNX_CallFunction(NWNX_Object, sFunc);
            return (NWNX_GetReturnValueObject(NWNX_Object, sFunc));
        }

        /// <summary>
        /// Set the provided object's position to the provided vector.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pos"></param>
        public void SetPosition(NWObject obj, Vector pos)
        {
            string sFunc = "SetPosition";

            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_X);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Y);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Z);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);
            NWNX_CallFunction(NWNX_Object, sFunc);

        }

        /// <summary>
        /// Sets the provided object's current hit points to the provided value.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="hp"></param>
        public void SetCurrentHitPoints(NWCreature creature, int hp)
        {
            string sFunc = "SetCurrentHitPoints";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, hp);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set object's maximum hit points; will not work on PCs.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="hp"></param>
        public void SetMaxHitPoints(NWObject creature, int hp)
        {
            string sFunc = "SetMaxHitPoints";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, hp);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Get the name of the portrait NWObject is using.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public string GetPortrait(NWObject creature)
        {
            string sFunc = "GetPortrait";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set the portrait NWObject is using. The portrait String must be no more than 15 characters long.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="portrait"></param>
        public void SetPortrait(NWObject creature, string portrait)
        {
            string sFunc = "SetPortrait";

            NWNX_PushArgumentString(NWNX_Object, sFunc, portrait);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }
        
        /// <summary>
        /// Serialize the full NWObject (including locals, inventory, etc) to base64 string
        /// Only works on Creatures and Items currently.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Serialize(Object obj)
        {
            string sFunc = "Serialize";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Deserialize the object. The NWObject will be created outside of the world and
        /// needs to be manually positioned at a location/inventory.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public Object Deserialize(string serialized)
        {
            string sFunc = "Deserialize";

            NWNX_PushArgumentString(NWNX_Object, sFunc, serialized);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueObject(NWNX_Object, sFunc);
        }


        /// <summary>
        /// Returns the dialog resref of the object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string GetDialogResref(NWObject obj)
        {
            string sFunc = "GetDialogResref";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Sets the dialog resref of the object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dialog"></param>
        public void SetDialogResref(NWObject obj, string dialog)
        {
            string sFunc = "SetDialogResref";

            NWNX_PushArgumentString(NWNX_Object, sFunc, dialog);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set obj's appearance. Will not update for PCs until they
        /// re-enter the area.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="app"></param>
        public void SetAppearance(NWObject obj, int app)
        {
            string sFunc = "SetAppearance";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, app);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Get obj's appearance
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetAppearance(NWObject obj)
        {
            string sFunc = "GetAppearance";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj.Object);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Object, sFunc);
        }

    }
}
