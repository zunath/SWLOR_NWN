using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class ObjectPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Object";

        public static int LocalVarTypeInt { get; } = 1;
        public static int LocalVarTypeFloat { get; } = 2;
        public static int LocalVarTypeObject { get; } = 4;
        public static int LocalVarTypeString { get; } = 3;
        public static int LocalVarTypeLocation { get; } = 5;

        // Gets the count of all local variables on the provided object.
        public static int GetLocalVariableCount(uint obj)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetLocalVariableCount");
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }


        // @brief Gets the local variable at the provided index of the provided object.
        // @param obj The object.
        // @param index The index.
        // @note Index bounds: 0 >= index < NWNX_Object_GetLocalVariableCount().
        // @note As of build 8193.14 local variables no longer have strict ordering.
        //       this means that any change to the variables can result in drastically
        //       different order when iterating.
        // @note As of build 8193.14, this function takes O(n) time, where n is the number
        //       of locals on the object. Individual variable access with GetLocalXxx()
        //       is now O(1) though.
        // @note As of build 8193.14, this function may return variable type UNKNOWN
        //       if the value is the default (0/0.0/""/OBJECT_INVALID) for the type.
        // @return An NWNX_Object_LocalVariable struct.
        public static LocalVariable GetLocalVariable(uint obj, int index)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetLocalVariable");
            NWNXPInvoke.NWNXPushInt(index);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();

            var lv = new LocalVariable
            {
                Key = NWNXPInvoke.NWNXPopString(),
                Type = (LocalVariableType)NWNXPInvoke.NWNXPopInt()
            };
            return lv;
        }

        // Set the provided object's position to the provided vector.
        public static void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetPosition");
            NWNXPInvoke.NWNXPushInt(updateSubareas ? 1 : 0);
            NWNXPInvoke.NWNXPushFloat(pos.X);
            NWNXPInvoke.NWNXPushFloat(pos.Y);
            NWNXPInvoke.NWNXPushFloat(pos.Z);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Sets the provided object's current hit points to the provided value.
        public static void SetCurrentHitPoints(uint creature, int hp)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetCurrentHitPoints");
            NWNXPInvoke.NWNXPushInt(hp);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set object's maximum hit points; will not work on PCs.
        public static void SetMaxHitPoints(uint creature, int hp)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMaxHitPoints");
            NWNXPInvoke.NWNXPushInt(hp);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Serialize the full object (including locals, inventory, etc) to base64 string
        public static string Serialize(uint obj)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "Serialize");
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Deserialize the object. The object will be created outside of the world and
        // needs to be manually positioned at a location/inventory.
        public static uint Deserialize(string serialized)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "Deserialize");
            NWNXPInvoke.NWNXPushString(serialized);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopObject();
        }

        // Returns the dialog resref of the object.
        public static string GetDialogResref(uint obj)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetDialogResref");
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Sets the dialog resref of the object.
        public static void SetDialogResref(uint obj, string dialog)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetDialogResref");
            NWNXPInvoke.NWNXPushString(dialog);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set obj's appearance. Will not update for PCs until they
        // re-enter the area.
        public static void SetAppearance(uint placeable, int appearance)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAppearance");
            NWNXPInvoke.NWNXPushInt(appearance);
            NWNXPInvoke.NWNXPushObject(placeable);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get obj's appearance
        public static int GetAppearance(uint obj)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetAppearance");
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Return true if obj has visual effect nVFX applied to it
        public static int GetHasVisualEffect(uint obj, int nVfx)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetHasVisualEffect");
            NWNXPInvoke.NWNXPushInt(nVfx);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Return damage immunity (in percent) against given damage type
        // Use DAMAGE_TYPE_* constants for damageType
        public static int GetDamageImmunity(uint obj, int damageType)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetDamageImmunity");
            NWNXPInvoke.NWNXPushInt(damageType);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        /// Add or move obj to area at pos
        public static void AddToArea(uint obj, uint area, Vector3 pos)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddToArea");
            NWNXPInvoke.NWNXPushFloat(pos.Z);
            NWNXPInvoke.NWNXPushFloat(pos.Y);
            NWNXPInvoke.NWNXPushFloat(pos.X);
            NWNXPInvoke.NWNXPushObject(area);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set placeable as static or not.
        // Will not update for PCs until they re-enter the area
        public static bool GetPlaceableIsStatic(uint obj)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetPlaceableIsStatic");
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt() != 0;
        }

        // Set placeable as static or not
        public static void SetPlaceableIsStatic(uint obj, bool isStatic)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetPlaceableIsStatic");
            NWNXPInvoke.NWNXPushInt(isStatic ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets if a door/placeable auto-removes the key after use.
        public static bool GetAutoRemoveKey(uint obj)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetAutoRemoveKey");
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt() != 0;
        }

        // Sets if a door/placeable auto-removes the key after use
        public static void SetAutoRemoveKey(uint obj, bool bRemoveKey)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAutoRemoveKey");
            NWNXPInvoke.NWNXPushInt(bRemoveKey ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get the geometry of a trigger
        public static string GetTriggerGeometry(uint oTrigger)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetTriggerGeometry");
            NWNXPInvoke.NWNXPushObject(oTrigger);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Set the geometry of a trigger with a list of vertex positions.
        // sGeometry Needs to be in the following format -> {x.x, y.y, z.z} or {x.x, y.y}
        // Example Geometry: "{1.0, 1.0, 0.0}{4.0, 1.0, 0.0}{4.0, 4.0, 0.0}{1.0, 4.0, 0.0}"
        // The Z position is optional and will be calculated dynamically based
        // on terrain height if it's not provided.
        // The minimum number of vertices is 3.
        public static void SetTriggerGeometry(uint oTrigger, string sGeometry)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetTriggerGeometry");
            NWNXPInvoke.NWNXPushString(sGeometry);
            NWNXPInvoke.NWNXPushObject(oTrigger);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Export an object to the UserDirectory/nwnx folder
        public static void Export(uint oObject, string sFileName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "Export");
            NWNXPInvoke.NWNXPushString(sFileName);
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get an object's integer variable variableName.
        public static int GetInt(uint obj, string variableName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetInt");
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Set an object's integer variable variableName to newValue. Toggle persistence with persist.
        public static void SetInt(uint obj, string variableName, int newValue, bool persist)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetInt");
            NWNXPInvoke.NWNXPushInt(persist ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(newValue);
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Delete an object's integer variable variableName.
        public static void DeleteInt(uint obj, string variableName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DeleteInt");
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get an object's string variable variableName.
        public static string GetString(uint obj, string variableName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetString");
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Set an object's string variable variableName to newValue. Toggle persistence with persist.
        public static void SetString(uint obj, string variableName, string newValue, bool persist)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetString");
            NWNXPInvoke.NWNXPushInt(persist ? 1 : 0);
            NWNXPInvoke.NWNXPushString(newValue);
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Delete an object's string variable variableName.
        public static void DeleteString(uint obj, string variableName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DeleteString");
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get an object's float variable variableName.
        public static float GetFloat(uint obj, string variableName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFloat");
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopFloat();
        }

        // Set an object's float variable variableName to newValue. Toggle persistence with persist.
        public static void SetFloat(uint obj, string variableName, float newValue, bool persist)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetFloat");
            NWNXPInvoke.NWNXPushInt(persist ? 1 : 0);
            NWNXPInvoke.NWNXPushFloat(newValue);
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Delete an object's float variable variableName.
        public static void DeleteFloat(uint obj, string variableName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DeleteFloat");
            NWNXPInvoke.NWNXPushString(variableName);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Delete any variables that match regexString set by SetInt, SetFloat, or SetString.
        public static void DeleteVarRegex(uint obj, string regexString)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DeleteVarRegex");
            NWNXPInvoke.NWNXPushString(regexString);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// <summary>
        /// Get if vPosition is inside oTrigger's geometry.
        /// The Z value of vPosition is ignored.
        /// oTrigger The trigger.
        /// vPosition The position.
        /// TRUE if vPosition is inside oTrigger's geometry.
        /// </summary>
        public static bool GetPositionIsInTrigger(uint obj, Vector3 position)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetPositionIsInTrigger");
            NWNXPInvoke.NWNXPushFloat(position.Z);
            NWNXPInvoke.NWNXPushFloat(position.Y);
            NWNXPInvoke.NWNXPushFloat(position.X);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt() != 0;
        }

        /// <summary>
        /// Gets the given object's internal type (NWNX_OBJECT_TYPE_INTERNAL_*)
        /// oObject The object.
        /// The object's type (NWNX_OBJECT_TYPE_INTERNAL_*)
        /// </summary>
        public static InternalObjectType GetInternalObjectType(uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetInternalObjectType");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();

            return (InternalObjectType)NWNXPInvoke.NWNXPopInt();
        }

        /// <summary>
        /// Have oObject acquire oItem.
        /// Useful to give deserialized items to an object, may not work if oItem is already possessed by an object.
        /// oObject The object receiving oItem, must be a Creature, Placeable, Store or Item
        /// oItem The item.
        /// TRUE on success.
        /// </summary>
        public static int AcquireItem(uint oObject, uint oItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AcquireItem");
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }


        /// @brief Checks for specific spell immunity. Should only be called in spellscripts
        /// @param oDefender The object defending against the spell.
        /// @param oCaster The object casting the spell.
        /// @return -1 if defender has no immunity, 2 if the defender is immune
        public static int DoSpellImmunity(uint oDefender, uint oCaster)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DoSpellImmunity");

            NWNXPInvoke.NWNXPushObject(oCaster);
            NWNXPInvoke.NWNXPushObject(oDefender);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        /// @brief Checks for spell school/level immunities and mantles. Should only be called in spellscripts
        /// @param oDefender The object defending against the spell.
        /// @param oCaster The object casting the spell.
        /// @return -1 defender no immunity. 2 if immune. 3 if immune, but the immunity has a limit (example: mantles)
        public static int DoSpellLevelAbsorption(uint oDefender, uint oCaster)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DoSpellLevelAbsorption");
            NWNXPInvoke.NWNXPushObject(oCaster);
            NWNXPInvoke.NWNXPushObject(oDefender);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public struct LocalVariable
        {
            public LocalVariableType Type;
            public string Key;
        }

        /// @brief Get an object's hit points.
        /// @note Unlike the native GetCurrentHitpoints function, this excludes temporary hitpoints.
        /// @param obj The object.
        /// @return The hit points.
        public static int GetCurrentHitPoints(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCurrentHitPoints");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static int GetDoorHasVisibleModel(uint oDoor)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetDoorHasVisibleModel");
            NWNXPInvoke.NWNXPushObject(oDoor);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static int GetIsDestroyable(uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetIsDestroyable");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }


        public static void ClearSpellEffectsOnOthers(uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "ClearSpellEffectsOnOthers");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static string PeekUUID(uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "PeekUUID");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopString();
        }

        public static void SetHasInventory(uint obj, bool bHasInventory)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetHasInventory");
            NWNXPInvoke.NWNXPushInt(bHasInventory ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(obj);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetCurrentAnimation(uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCurrentAnimation");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static int GetAILevel(uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetAILevel");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static void SetAILevel(uint oObject, int nLevel)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAILevel");
            NWNXPInvoke.NWNXPushInt(nLevel);
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static string GetMapNote(uint oObject, int nID = 0, int nGender = 0)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMapNote");
            NWNXPInvoke.NWNXPushInt(nGender);
            NWNXPInvoke.NWNXPushInt(nID);
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopString();
        }

        public static void SetMapNote(uint oObject, string sMapNote, int nID = 0, int nGender = 0)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMapNote");
            NWNXPInvoke.NWNXPushInt(nGender);
            NWNXPInvoke.NWNXPushInt(nID);
            NWNXPInvoke.NWNXPushString(sMapNote);
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();
        }
    }
}