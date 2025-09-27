using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive object management functionality including local variable manipulation,
    /// object positioning, serialization, and advanced object properties. This plugin allows for
    /// detailed control over object behavior and state management throughout the game.
    /// </summary>
    public static class ObjectPlugin
    {
        /// <summary>
        /// Retrieves the total number of local variables stored on the specified object.
        /// </summary>
        /// <param name="obj">The object to query. Must be a valid object with local variables.</param>
        /// <returns>The number of local variables on the object. Returns 0 if no variables exist.</returns>
        /// <remarks>
        /// This function counts all local variables that have been set on the object.
        /// Variables with default values (0, 0.0, "", OBJECT_INVALID) are not counted as they are considered "not set".
        /// Use GetLocalVariable() to retrieve individual variables by index.
        /// </remarks>
        public static int GetLocalVariableCount(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLocalVariableCount(obj);
        }


        /// <summary>
        /// Retrieves a local variable from the specified object by its index position.
        /// </summary>
        /// <param name="obj">The object to query. Must be a valid object with local variables.</param>
        /// <param name="index">The zero-based index of the variable to retrieve. Must be between 0 and GetLocalVariableCount() - 1.</param>
        /// <returns>A LocalVariable struct containing the variable's key and type information.</returns>
        /// <remarks>
        /// This function retrieves local variables by their position in the object's variable list.
        /// Index bounds: 0 <= index < GetLocalVariableCount().
        /// As of build 8193.14, local variables no longer have strict ordering, so iteration order may change when variables are modified.
        /// This function takes O(n) time complexity, where n is the number of locals on the object.
        /// Variables with default values (0/0.0/""/OBJECT_INVALID) are not returned as they are considered "not set".
        /// Will return type UNKNOWN for cassowary variables.
        /// For better performance, use individual GetLocalXxx() functions which are O(1) time complexity.
        /// </remarks>
        public static LocalVariable GetLocalVariable(uint obj, int index)
        {
            var coreResult = global::NWN.Core.NWNX.ObjectPlugin.GetLocalVariable(obj, index);
            return new LocalVariable
            {
                Key = coreResult.key,
                Type = (LocalVariableType)coreResult.type
            };
        }

        /// <summary>
        /// Sets the position of the specified object to the provided 3D coordinates.
        /// </summary>
        /// <param name="obj">The object to move. Must be a valid object that can be positioned.</param>
        /// <param name="pos">The new 3D position vector (x, y, z coordinates).</param>
        /// <param name="updateSubareas">If true and obj is a creature, any triggers/traps at the new position will fire their events.</param>
        /// <remarks>
        /// This function immediately moves the object to the specified position in the world.
        /// If updateSubareas is true and the object is a creature, it will trigger any area effects (triggers, traps) at the new location.
        /// The object must be in a valid area for positioning to work correctly.
        /// Use with caution as this can bypass normal movement restrictions and collision detection.
        /// </remarks>
        public static void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetPosition(obj, pos, updateSubareas ? 1 : 0);
        }

        /// <summary>
        /// Sets the provided object's current hit points to the provided value.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <param name="hp">The hit points.</param>
        public static void SetCurrentHitPoints(uint creature, int hp)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetCurrentHitPoints(creature, hp);
        }

        /// <summary>
        /// Set object's maximum hit points; will not work on PCs.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <param name="hp">The maximum hit points.</param>
        public static void SetMaxHitPoints(uint creature, int hp)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetMaxHitPoints(creature, hp);
        }

        /// <summary>
        /// Serialize the full object (including locals, inventory, etc) to base64 string.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>A base64 string representation of the object.</returns>
        public static string Serialize(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.Serialize(obj);
        }

        /// <summary>
        /// Deserialize the object. The object will be created outside of the world and needs to be manually positioned at a location/inventory.
        /// </summary>
        /// <param name="serialized">The base64 string.</param>
        /// <returns>The object.</returns>
        public static uint Deserialize(string serialized)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.Deserialize(serialized);
        }

        /// <summary>
        /// Returns the dialog resref of the object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The name of the dialog resref.</returns>
        public static string GetDialogResref(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetDialogResref(obj);
        }

        /// <summary>
        /// Sets the dialog resref of the object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="dialog">The name of the dialog resref.</param>
        public static void SetDialogResref(uint obj, string dialog)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetDialogResref(obj, dialog);
        }

        /// <summary>
        /// Set obj's appearance. Will not update for PCs until they re-enter the area.
        /// </summary>
        /// <param name="placeable">The placeable.</param>
        /// <param name="appearance">The appearance id.</param>
        public static void SetAppearance(uint placeable, int appearance)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAppearance(placeable, appearance);
        }

        /// <summary>
        /// Get obj's appearance.
        /// </summary>
        /// <param name="obj">The placeable.</param>
        /// <returns>The appearance id.</returns>
        public static int GetAppearance(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAppearance(obj);
        }

        /// <summary>
        /// Return true if obj has visual effect nVFX applied to it.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="nVfx">The visual effect id.</param>
        /// <returns>True if the object has the visual effect applied to it.</returns>
        public static bool GetHasVisualEffect(uint obj, int nVfx)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetHasVisualEffect(obj, nVfx) != 0;
        }

        /// <summary>
        /// Return damage immunity (in percent) against given damage type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="damageType">The damage type to check for immunity. Use DAMAGE_TYPE_* constants.</param>
        /// <returns>Damage immunity as a percentage.</returns>
        public static int GetDamageImmunity(uint obj, int damageType)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetDamageImmunity(obj, damageType);
        }

        /// <summary>
        /// Add or move obj to area at pos.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="area">The area.</param>
        /// <param name="pos">The position.</param>
        public static void AddToArea(uint obj, uint area, Vector3 pos)
        {
            global::NWN.Core.NWNX.ObjectPlugin.AddToArea(obj, area, pos);
        }

        /// <summary>
        /// Set placeable as static or not. Will not update for PCs until they re-enter the area.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>True if placeable is static.</returns>
        public static bool GetPlaceableIsStatic(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetPlaceableIsStatic(obj) != 0;
        }

        /// <summary>
        /// Set placeable as static or not.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="isStatic">True/False.</param>
        /// <remarks>Will not update for PCs until they re-enter the area.</remarks>
        public static void SetPlaceableIsStatic(uint obj, bool isStatic)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetPlaceableIsStatic(obj, isStatic ? 1 : 0);
        }

        /// <summary>
        /// Gets if a door/placeable auto-removes the key after use.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>True/False or -1 on error.</returns>
        public static bool GetAutoRemoveKey(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAutoRemoveKey(obj) != 0;
        }

        /// <summary>
        /// Sets if a door/placeable auto-removes the key after use.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="bRemoveKey">True/False.</param>
        public static void SetAutoRemoveKey(uint obj, bool bRemoveKey)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAutoRemoveKey(obj, bRemoveKey ? 1 : 0);
        }

        /// <summary>
        /// Get the geometry of a trigger.
        /// </summary>
        /// <param name="oTrigger">The trigger object.</param>
        /// <returns>A string of vertex positions.</returns>
        public static string GetTriggerGeometry(uint oTrigger)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetTriggerGeometry(oTrigger);
        }

        /// <summary>
        /// Set the geometry of a trigger with a list of vertex positions.
        /// </summary>
        /// <param name="oTrigger">The trigger object.</param>
        /// <param name="sGeometry">Needs to be in the following format -> {x.x, y.y, z.z} or {x.x, y.y}. Example Geometry: "{1.0, 1.0, 0.0}{4.0, 1.0, 0.0}{4.0, 4.0, 0.0}{1.0, 4.0, 0.0}". The Z position is optional and will be calculated dynamically based on terrain height if it's not provided. The minimum number of vertices is 3.</param>
        public static void SetTriggerGeometry(uint oTrigger, string sGeometry)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetTriggerGeometry(oTrigger, sGeometry);
        }

        /// <summary>
        /// Export an object to the UserDirectory/nwnx folder.
        /// </summary>
        /// <param name="oObject">The object to export. Valid object types: Creature, Item, Placeable, Waypoint, Door, Store, Trigger.</param>
        /// <param name="sFileName">The filename without extension, 16 or less characters.</param>
        /// <param name="sAlias">The alias of the resource directory to add the .git file to. Default: UserDirectory/nwnx.</param>
        public static void Export(uint oObject, string sFileName, string sAlias = "NWNX")
        {
            global::NWN.Core.NWNX.ObjectPlugin.Export(oObject, sFileName, sAlias);
        }

        /// <summary>
        /// Get an object's integer variable variableName.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        /// <returns>The value or 0 on error.</returns>
        public static int GetInt(uint obj, string variableName)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetInt(obj, variableName);
        }

        /// <summary>
        /// Set an object's integer variable variableName to newValue. Toggle persistence with persist.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="newValue">The integer value to set.</param>
        /// <param name="persist">When true, the value is persisted to GFF, this means that it'll be saved in the .bic file of a player's character or when an object is serialized.</param>
        public static void SetInt(uint obj, string variableName, int newValue, bool persist)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetInt(obj, variableName, newValue, persist ? 1 : 0);
        }

        /// <summary>
        /// Delete an object's integer variable variableName.
        /// </summary>
        /// <param name="obj">The object to delete the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        public static void DeleteInt(uint obj, string variableName)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteInt(obj, variableName);
        }

        /// <summary>
        /// Get an object's string variable variableName.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        /// <returns>The value or "" on error.</returns>
        public static string GetString(uint obj, string variableName)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetString(obj, variableName);
        }

        /// <summary>
        /// Set an object's string variable variableName to newValue. Toggle persistence with persist.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="newValue">The string value to set.</param>
        /// <param name="persist">When true, the value is persisted to GFF, this means that it'll be saved in the .bic file of a player's character or when an object is serialized.</param>
        public static void SetString(uint obj, string variableName, string newValue, bool persist)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetString(obj, variableName, newValue, persist ? 1 : 0);
        }

        /// <summary>
        /// Delete an object's string variable variableName.
        /// </summary>
        /// <param name="obj">The object to delete the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        public static void DeleteString(uint obj, string variableName)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteString(obj, variableName);
        }

        /// <summary>
        /// Get an object's float variable variableName.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        /// <returns>The value or 0.0f on error.</returns>
        public static float GetFloat(uint obj, string variableName)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetFloat(obj, variableName);
        }

        /// <summary>
        /// Set an object's float variable variableName to newValue. Toggle persistence with persist.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="newValue">The float value to set.</param>
        /// <param name="persist">When true, the value is persisted to GFF, this means that it'll be saved in the .bic file of a player's character or when an object is serialized.</param>
        public static void SetFloat(uint obj, string variableName, float newValue, bool persist)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetFloat(obj, variableName, newValue, persist ? 1 : 0);
        }

        /// <summary>
        /// Delete an object's float variable variableName.
        /// </summary>
        /// <param name="obj">The object to delete the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        public static void DeleteFloat(uint obj, string variableName)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteFloat(obj, variableName);
        }

        /// <summary>
        /// Delete any variables that match regexString set by SetInt, SetFloat, or SetString.
        /// </summary>
        /// <param name="obj">The object to delete the variables from.</param>
        /// <param name="regexString">The regular expression, for example .*Test.* removes every variable that has Test in it.</param>
        public static void DeleteVarRegex(uint obj, string regexString)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteVarRegex(obj, regexString);
        }

        /// <summary>
        /// Get if vPosition is inside oTrigger's geometry.
        /// </summary>
        /// <param name="obj">The trigger.</param>
        /// <param name="position">The position.</param>
        /// <returns>True if vPosition is inside oTrigger's geometry.</returns>
        /// <remarks>The Z value of vPosition is ignored.</remarks>
        public static bool GetPositionIsInTrigger(uint obj, Vector3 position)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetPositionIsInTrigger(obj, position) != 0;
        }

        /// <summary>
        /// Gets the given object's internal type (NWNX_OBJECT_TYPE_INTERNAL_*).
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The object's type (NWNX_OBJECT_TYPE_INTERNAL_*).</returns>
        public static InternalObjectType GetInternalObjectType(uint oObject)
        {
            return (InternalObjectType)global::NWN.Core.NWNX.ObjectPlugin.GetInternalObjectType(oObject);
        }

        /// <summary>
        /// Have oObject acquire oItem.
        /// </summary>
        /// <param name="oObject">The object receiving oItem, must be a Creature, Placeable, Store or Item.</param>
        /// <param name="oItem">The item.</param>
        /// <returns>True on success.</returns>
        /// <remarks>Useful to give deserialized items to an object, may not work if oItem is already possessed by an object.</remarks>
        public static bool AcquireItem(uint oObject, uint oItem)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.AcquireItem(oObject, oItem) != 0;
        }


        /// <summary>
        /// Checks for specific spell immunity. Should only be called in spellscripts.
        /// </summary>
        /// <param name="oDefender">The object defending against the spell.</param>
        /// <param name="oCaster">The object casting the spell.</param>
        /// <param name="nSpellId">The casted spell id. Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <returns>-1 if defender has no immunity, 2 if the defender is immune.</returns>
        public static int DoSpellImmunity(uint oDefender, uint oCaster, int nSpellId = -1)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.DoSpellImmunity(oDefender, oCaster, nSpellId);
        }

        /// <summary>
        /// Checks for spell school/level immunities and mantles. Should only be called in spellscripts.
        /// </summary>
        /// <param name="oDefender">The object defending against the spell.</param>
        /// <param name="oCaster">The object casting the spell.</param>
        /// <param name="nSpellId">The casted spell id. Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <param name="nSpellLevel">The level of the casted spell. Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <param name="nSpellSchool">The school of the casted spell (SPELL_SCHOOL_* constant). Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <returns>-1 defender no immunity. 2 if immune. 3 if immune, but the immunity has a limit (example: mantles).</returns>
        public static int DoSpellLevelAbsorption(uint oDefender, uint oCaster, int nSpellId = -1, int nSpellLevel = -1, int nSpellSchool = -1)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.DoSpellLevelAbsorption(oDefender, oCaster, nSpellId, nSpellLevel, nSpellSchool);
        }

        /// <summary>
        /// Local variable structure.
        /// </summary>
        public struct LocalVariable
        {
            public LocalVariableType Type;
            public string Key;
        }

        /// <summary>
        /// Get an object's hit points.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <returns>The hit points.</returns>
        /// <remarks>Unlike the native GetCurrentHitpoints function, this excludes temporary hitpoints.</remarks>
        public static int GetCurrentHitPoints(uint creature)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetCurrentHitPoints(creature);
        }

        /// <summary>
        /// Get if oDoor has a visible model.
        /// </summary>
        /// <param name="oDoor">The door.</param>
        /// <returns>True if oDoor has a visible model.</returns>
        public static bool GetDoorHasVisibleModel(uint oDoor)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetDoorHasVisibleModel(oDoor) != 0;
        }

        /// <summary>
        /// Get if oObject is destroyable.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>True if oObject is destroyable.</returns>
        public static bool GetIsDestroyable(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetIsDestroyable(oObject) != 0;
        }


        /// <summary>
        /// Clear all spell effects oObject has applied to others.
        /// </summary>
        /// <param name="oObject">The object that applied the spell effects.</param>
        public static void ClearSpellEffectsOnOthers(uint oObject)
        {
            global::NWN.Core.NWNX.ObjectPlugin.ClearSpellEffectsOnOthers(oObject);
        }

        /// <summary>
        /// Peek at the UUID of oObject without assigning one if it does not have one.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The UUID or "" when the object does not have or cannot have an UUID.</returns>
        public static string PeekUUID(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.PeekUUID(oObject);
        }

        /// <summary>
        /// Sets if a placeable has an inventory.
        /// </summary>
        /// <param name="obj">The placeable.</param>
        /// <param name="bHasInventory">True/False.</param>
        /// <remarks>Only works on placeables.</remarks>
        public static void SetHasInventory(uint obj, bool bHasInventory)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetHasInventory(obj, bHasInventory ? 1 : 0);
        }

        /// <summary>
        /// Get the current animation of oObject.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>-1 on error or the engine animation constant.</returns>
        /// <remarks>The returned value will be an engine animation constant, not a NWScript ANIMATION_ constant.</remarks>
        public static int GetCurrentAnimation(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetCurrentAnimation(oObject);
        }

        /// <summary>
        /// Gets the AI level of an object.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The AI level (AI_LEVEL_* -1 to 4).</returns>
        public static int GetAILevel(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAILevel(oObject);
        }

        /// <summary>
        /// Sets the AI level of an object.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <param name="nLevel">The level to set (AI_LEVEL_* -1 to 4).</param>
        public static void SetAILevel(uint oObject, int nLevel)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAILevel(oObject, nLevel);
        }

        /// <summary>
        /// Retrieves the Map Note (AKA Map Pin) from a waypoint - Returns even if currently disabled.
        /// </summary>
        /// <param name="oObject">The Waypoint object.</param>
        /// <param name="nID">The Language ID (default English).</param>
        /// <param name="nGender">0 = Male, 1 = Female.</param>
        public static string GetMapNote(uint oObject, int nID = 0, int nGender = 0)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetMapNote(oObject, nID, nGender);
        }

        /// <summary>
        /// Sets a Map Note (AKA Map Pin) to any waypoint, even if no previous map note. Only updates for clients on area-load. Use SetMapPinEnabled() as required.
        /// </summary>
        /// <param name="oObject">The Waypoint object.</param>
        /// <param name="sMapNote">The contents to set as the Map Note.</param>
        /// <param name="nID">The Language ID (default English).</param>
        /// <param name="nGender">0 = Male, 1 = Female.</param>
        public static void SetMapNote(uint oObject, string sMapNote, int nID = 0, int nGender = 0)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetMapNote(oObject, sMapNote, nID, nGender);
        }

        /// <summary>
        /// Gets the last spell cast feat of the object.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The feat ID, or 65535 when not cast by a feat, or -1 on error.</returns>
        /// <remarks>Should be called in a spell script.</remarks>
        public static int GetLastSpellCastFeat(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellCastFeat(oObject);
        }

        /// <summary>
        /// Sets the last object that triggered door or placeable trap.
        /// </summary>
        /// <param name="oObject">Door or placeable object.</param>
        /// <param name="oLast">Object that last triggered trap.</param>
        /// <remarks>Should be retrieved with GetEnteringObject.</remarks>
        public static void SetLastTriggered(uint oObject, uint oLast)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetLastTriggered(oObject, oLast);
        }

        /// <summary>
        /// Gets the remaining duration of the AoE object.
        /// </summary>
        /// <param name="oAoE">The AreaOfEffect object.</param>
        /// <returns>The remaining duration, in seconds, or zero on failure.</returns>
        public static float GetAoEObjectDurationRemaining(uint oAoE)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAoEObjectDurationRemaining(oAoE);
        }

        /// <summary>
        /// Sets conversations started by the object to be private or not.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <param name="bPrivate">True/False.</param>
        /// <remarks>ActionStartConversation()'s bPrivateConversation parameter will overwrite this flag.</remarks>
        public static void SetConversationPrivate(uint oObject, bool bPrivate)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetConversationPrivate(oObject, bPrivate ? 1 : 0);
        }

        /// <summary>
        /// Sets the radius of a circle AoE object.
        /// </summary>
        /// <param name="oAoE">The AreaOfEffect object.</param>
        /// <param name="fRadius">The radius, must be bigger than 0.0f.</param>
        public static void SetAoEObjectRadius(uint oAoE, float fRadius)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAoEObjectRadius(oAoE, fRadius);
        }

        /// <summary>
        /// Gets the radius of a circle AoE object.
        /// </summary>
        /// <param name="oAoE">The AreaOfEffect object.</param>
        /// <returns>The radius or 0.0f on error.</returns>
        public static float GetAoEObjectRadius(uint oAoE)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAoEObjectRadius(oAoE);
        }

        /// <summary>
        /// Gets whether the last spell cast of the object was spontaneous.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>True if the last spell was cast spontaneously.</returns>
        /// <remarks>Should be called in a spell script.</remarks>
        public static bool GetLastSpellCastSpontaneous(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellCastSpontaneous(oObject) != 0;
        }

        /// <summary>
        /// Gets the last spell cast domain level.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>Domain level of the cast spell, 0 if not a domain spell.</returns>
        /// <remarks>Should be called in a spell script.</remarks>
        public static int GetLastSpellCastDomainLevel(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellCastDomainLevel(oObject);
        }

        /// <summary>
        /// Force the given object to carry the given UUID. Any other object currently owning the UUID is stripped of it.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <param name="sUUID">The UUID to force.</param>
        public static void ForceAssignUUID(uint oObject, string sUUID)
        {
            global::NWN.Core.NWNX.ObjectPlugin.ForceAssignUUID(oObject, sUUID);
        }

        /// <summary>
        /// Returns how many items are in the object's inventory.
        /// </summary>
        /// <param name="oObject">A creature, placeable, item or store.</param>
        /// <returns>Returns a count of how many items are in the object's inventory.</returns>
        public static int GetInventoryItemCount(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetInventoryItemCount(oObject);
        }

        /// <summary>
        /// Override the projectile visual effect of ranged/throwing weapons and spells.
        /// </summary>
        /// <param name="oCreature">The creature.</param>
        /// <param name="nProjectileType">A projectile type constant or -1 to remove the override.</param>
        /// <param name="nProjectilePathType">A projectile path type constant or -1 to ignore.</param>
        /// <param name="nSpellID">A spell constant. -1 to ignore.</param>
        /// <param name="bPersist">Whether the override should persist to the .bic file (for PCs).</param>
        /// <remarks>
        /// Persistence is enabled after a server reset by the first use of this function. Recommended to trigger on a dummy target OnModuleLoad to enable persistence.
        /// This will override all spell projectile VFX from the creature until the override is removed.
        /// </remarks>
        public static void OverrideSpellProjectileVFX(uint oCreature, int nProjectileType = -1, int nProjectilePathType = -1, int nSpellID = -1, bool bPersist = false)
        {
            global::NWN.Core.NWNX.ObjectPlugin.OverrideSpellProjectileVFX(oCreature, nProjectileType, nProjectilePathType, nSpellID, bPersist ? 1 : 0);
        }

        /// <summary>
        /// Returns true if the last spell was cast instantly. This function should only be called in a spell script.
        /// </summary>
        /// <returns>True if the last spell was instant.</returns>
        /// <remarks>To initialize the hooks used by this function it is recommended to call this function once in your module load script.</remarks>
        public static bool GetLastSpellInstant()
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellInstant() != 0;
        }

        /// <summary>
        /// Sets the creator of a trap on door, placeable, or trigger. Also changes trap Faction to that of the new Creator.
        /// </summary>
        /// <param name="oObject">Door, placeable or trigger (trap) object.</param>
        /// <param name="oCreator">The new creator of the trap. Any non-creature creator will assign OBJECT_INVALID (similar to toolset-laid traps).</param>
        /// <remarks>
        /// Triggers (ground traps) will instantly update colour (Green/Red). Placeable/doors will not change if client has already seen them.
        /// </remarks>
        public static void SetTrapCreator(uint oObject, uint oCreator)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetTrapCreator(oObject, oCreator);
        }

        /// <summary>
        /// Return the name of the object for the specified language.
        /// </summary>
        /// <param name="oObject">An object.</param>
        /// <param name="nLanguage">A PLAYER_LANGUAGE constant.</param>
        /// <param name="nGender">Gender to use, 0 or 1.</param>
        /// <returns>The localized string.</returns>
        public static string GetLocalizedName(uint oObject, int nLanguage, int nGender = 0)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLocalizedName(oObject, nLanguage, nGender);
        }

        /// <summary>
        /// Set the name of the object as set in the toolset for the specified language.
        /// </summary>
        /// <param name="oObject">An object.</param>
        /// <param name="sName">New value to set.</param>
        /// <param name="nLanguage">A PLAYER_LANGUAGE constant.</param>
        /// <param name="nGender">Gender to use, 0 or 1.</param>
        /// <remarks>You may have to SetName(oObject, "") for the translated string to show.</remarks>
        public static void SetLocalizedName(uint oObject, string sName, int nLanguage, int nGender = 0)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetLocalizedName(oObject, sName, nLanguage, nGender);
        }
    }
}