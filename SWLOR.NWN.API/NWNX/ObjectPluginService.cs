using System.Numerics;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive object management functionality including local variable manipulation,
    /// object positioning, serialization, and advanced object properties. This plugin allows for
    /// detailed control over object behavior and state management throughout the game.
    /// </summary>
    public class ObjectPluginService : IObjectPluginService
    {
        /// <inheritdoc/>
        public int GetLocalVariableCount(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLocalVariableCount(obj);
        }


        /// <inheritdoc/>
        public LocalVariable GetLocalVariable(uint obj, int index)
        {
            var coreResult = global::NWN.Core.NWNX.ObjectPlugin.GetLocalVariable(obj, index);
            return new LocalVariable
            {
                Key = coreResult.key,
                Type = (LocalVariableType)coreResult.type
            };
        }

        /// <inheritdoc/>
        public void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetPosition(obj, pos, updateSubareas ? 1 : 0);
        }

        /// <inheritdoc/>
        public void SetCurrentHitPoints(uint creature, int hp)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetCurrentHitPoints(creature, hp);
        }

        /// <inheritdoc/>
        public void SetMaxHitPoints(uint creature, int hp)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetMaxHitPoints(creature, hp);
        }

        /// <inheritdoc/>
        public string Serialize(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.Serialize(obj);
        }

        /// <inheritdoc/>
        public uint Deserialize(string serialized)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.Deserialize(serialized);
        }

        /// <inheritdoc/>
        public string GetDialogResref(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetDialogResref(obj);
        }

        /// <inheritdoc/>
        public void SetDialogResref(uint obj, string dialog)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetDialogResref(obj, dialog);
        }

        /// <inheritdoc/>
        public void SetAppearance(uint placeable, int appearance)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAppearance(placeable, appearance);
        }

        /// <inheritdoc/>
        public int GetAppearance(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAppearance(obj);
        }

        /// <inheritdoc/>
        public bool GetHasVisualEffect(uint obj, int nVfx)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetHasVisualEffect(obj, nVfx) != 0;
        }

        /// <inheritdoc/>
        public int GetDamageImmunity(uint obj, int damageType)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetDamageImmunity(obj, damageType);
        }

        /// <inheritdoc/>
        public void AddToArea(uint obj, uint area, Vector3 pos)
        {
            global::NWN.Core.NWNX.ObjectPlugin.AddToArea(obj, area, pos);
        }

        /// <inheritdoc/>
        public bool GetPlaceableIsStatic(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetPlaceableIsStatic(obj) != 0;
        }

        /// <inheritdoc/>
        public void SetPlaceableIsStatic(uint obj, bool isStatic)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetPlaceableIsStatic(obj, isStatic ? 1 : 0);
        }

        /// <inheritdoc/>
        public bool GetAutoRemoveKey(uint obj)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAutoRemoveKey(obj) != 0;
        }

        /// <inheritdoc/>
        public void SetAutoRemoveKey(uint obj, bool bRemoveKey)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAutoRemoveKey(obj, bRemoveKey ? 1 : 0);
        }

        /// <inheritdoc/>
        public string GetTriggerGeometry(uint oTrigger)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetTriggerGeometry(oTrigger);
        }

        /// <inheritdoc/>
        public void SetTriggerGeometry(uint oTrigger, string sGeometry)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetTriggerGeometry(oTrigger, sGeometry);
        }

        /// <inheritdoc/>
        public void Export(uint oObject, string sFileName, string sAlias = "NWNX")
        {
            global::NWN.Core.NWNX.ObjectPlugin.Export(oObject, sFileName, sAlias);
        }

        /// <inheritdoc/>
        public int GetInt(uint obj, string variableName)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetInt(obj, variableName);
        }

        /// <inheritdoc/>
        public void SetInt(uint obj, string variableName, int newValue, bool persist)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetInt(obj, variableName, newValue, persist ? 1 : 0);
        }

        /// <inheritdoc/>
        public void DeleteInt(uint obj, string variableName)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteInt(obj, variableName);
        }

        /// <inheritdoc/>
        public string GetString(uint obj, string variableName)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetString(obj, variableName);
        }

        /// <inheritdoc/>
        public void SetString(uint obj, string variableName, string newValue, bool persist)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetString(obj, variableName, newValue, persist ? 1 : 0);
        }

        /// <inheritdoc/>
        public void DeleteString(uint obj, string variableName)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteString(obj, variableName);
        }

        /// <inheritdoc/>
        public float GetFloat(uint obj, string variableName)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetFloat(obj, variableName);
        }

        /// <inheritdoc/>
        public void SetFloat(uint obj, string variableName, float newValue, bool persist)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetFloat(obj, variableName, newValue, persist ? 1 : 0);
        }

        /// <inheritdoc/>
        public void DeleteFloat(uint obj, string variableName)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteFloat(obj, variableName);
        }

        /// <inheritdoc/>
        public void DeleteVarRegex(uint obj, string regexString)
        {
            global::NWN.Core.NWNX.ObjectPlugin.DeleteVarRegex(obj, regexString);
        }

        /// <inheritdoc/>
        public bool GetPositionIsInTrigger(uint obj, Vector3 position)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetPositionIsInTrigger(obj, position) != 0;
        }

        /// <inheritdoc/>
        public InternalObjectType GetInternalObjectType(uint oObject)
        {
            return (InternalObjectType)global::NWN.Core.NWNX.ObjectPlugin.GetInternalObjectType(oObject);
        }

        /// <inheritdoc/>
        public bool AcquireItem(uint oObject, uint oItem)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.AcquireItem(oObject, oItem) != 0;
        }


        /// <inheritdoc/>
        public int DoSpellImmunity(uint oDefender, uint oCaster, int nSpellId = -1)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.DoSpellImmunity(oDefender, oCaster, nSpellId);
        }

        /// <inheritdoc/>
        public int DoSpellLevelAbsorption(uint oDefender, uint oCaster, int nSpellId = -1, int nSpellLevel = -1, int nSpellSchool = -1)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.DoSpellLevelAbsorption(oDefender, oCaster, nSpellId, nSpellLevel, nSpellSchool);
        }

        /// <inheritdoc/>
        public struct LocalVariable
        {
            public LocalVariableType Type;
            public string Key;
        }

        /// <inheritdoc/>
        public int GetCurrentHitPoints(uint creature)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetCurrentHitPoints(creature);
        }

        /// <inheritdoc/>
        public bool GetDoorHasVisibleModel(uint oDoor)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetDoorHasVisibleModel(oDoor) != 0;
        }

        /// <inheritdoc/>
        public bool GetIsDestroyable(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetIsDestroyable(oObject) != 0;
        }


        /// <inheritdoc/>
        public void ClearSpellEffectsOnOthers(uint oObject)
        {
            global::NWN.Core.NWNX.ObjectPlugin.ClearSpellEffectsOnOthers(oObject);
        }

        /// <inheritdoc/>
        public string PeekUUID(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.PeekUUID(oObject);
        }

        /// <inheritdoc/>
        public void SetHasInventory(uint obj, bool bHasInventory)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetHasInventory(obj, bHasInventory ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetCurrentAnimation(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetCurrentAnimation(oObject);
        }

        /// <inheritdoc/>
        public int GetAILevel(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAILevel(oObject);
        }

        /// <inheritdoc/>
        public void SetAILevel(uint oObject, int nLevel)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAILevel(oObject, nLevel);
        }

        /// <inheritdoc/>
        public string GetMapNote(uint oObject, int nID = 0, int nGender = 0)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetMapNote(oObject, nID, nGender);
        }

        /// <inheritdoc/>
        public void SetMapNote(uint oObject, string sMapNote, int nID = 0, int nGender = 0)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetMapNote(oObject, sMapNote, nID, nGender);
        }

        /// <inheritdoc/>
        public int GetLastSpellCastFeat(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellCastFeat(oObject);
        }

        /// <inheritdoc/>
        public void SetLastTriggered(uint oObject, uint oLast)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetLastTriggered(oObject, oLast);
        }

        /// <inheritdoc/>
        public float GetAoEObjectDurationRemaining(uint oAoE)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAoEObjectDurationRemaining(oAoE);
        }

        /// <inheritdoc/>
        public void SetConversationPrivate(uint oObject, bool bPrivate)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetConversationPrivate(oObject, bPrivate ? 1 : 0);
        }

        /// <inheritdoc/>
        public void SetAoEObjectRadius(uint oAoE, float fRadius)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetAoEObjectRadius(oAoE, fRadius);
        }

        /// <inheritdoc/>
        public float GetAoEObjectRadius(uint oAoE)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetAoEObjectRadius(oAoE);
        }

        /// <inheritdoc/>
        public bool GetLastSpellCastSpontaneous(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellCastSpontaneous(oObject) != 0;
        }

        /// <inheritdoc/>
        public int GetLastSpellCastDomainLevel(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellCastDomainLevel(oObject);
        }

        /// <inheritdoc/>
        public void ForceAssignUUID(uint oObject, string sUUID)
        {
            global::NWN.Core.NWNX.ObjectPlugin.ForceAssignUUID(oObject, sUUID);
        }

        /// <inheritdoc/>
        public int GetInventoryItemCount(uint oObject)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetInventoryItemCount(oObject);
        }

        /// <inheritdoc/>
        public void OverrideSpellProjectileVFX(uint oCreature, int nProjectileType = -1, int nProjectilePathType = -1, int nSpellID = -1, bool bPersist = false)
        {
            global::NWN.Core.NWNX.ObjectPlugin.OverrideSpellProjectileVFX(oCreature, nProjectileType, nProjectilePathType, nSpellID, bPersist ? 1 : 0);
        }

        /// <inheritdoc/>
        public bool GetLastSpellInstant()
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLastSpellInstant() != 0;
        }

        /// <inheritdoc/>
        public void SetTrapCreator(uint oObject, uint oCreator)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetTrapCreator(oObject, oCreator);
        }

        /// <inheritdoc/>
        public string GetLocalizedName(uint oObject, int nLanguage, int nGender = 0)
        {
            return global::NWN.Core.NWNX.ObjectPlugin.GetLocalizedName(oObject, nLanguage, nGender);
        }

        /// <inheritdoc/>
        public void SetLocalizedName(uint oObject, string sName, int nLanguage, int nGender = 0)
        {
            global::NWN.Core.NWNX.ObjectPlugin.SetLocalizedName(oObject, sName, nLanguage, nGender);
        }
    }
}


