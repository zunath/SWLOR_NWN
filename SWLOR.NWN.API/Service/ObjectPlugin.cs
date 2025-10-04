using System;
using System.Numerics;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class ObjectPlugin
    {
        private static IObjectPluginService _service = new ObjectPluginService();

        internal static void SetService(IObjectPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IObjectPluginService.GetLocalVariableCount"/>
        public static int GetLocalVariableCount(uint obj) => _service.GetLocalVariableCount(obj);

        /// <inheritdoc cref="IObjectPluginService.GetLocalVariable"/>
        public static ObjectPluginService.LocalVariable GetLocalVariable(uint obj, int index) => _service.GetLocalVariable(obj, index);

        /// <inheritdoc cref="IObjectPluginService.SetPosition"/>
        public static void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true) => _service.SetPosition(obj, pos, updateSubareas);

        /// <inheritdoc cref="IObjectPluginService.SetCurrentHitPoints"/>
        public static void SetCurrentHitPoints(uint creature, int hp) => _service.SetCurrentHitPoints(creature, hp);

        /// <inheritdoc cref="IObjectPluginService.SetMaxHitPoints"/>
        public static void SetMaxHitPoints(uint creature, int hp) => _service.SetMaxHitPoints(creature, hp);

        /// <inheritdoc cref="IObjectPluginService.Serialize"/>
        public static string Serialize(uint obj) => _service.Serialize(obj);

        /// <inheritdoc cref="IObjectPluginService.Deserialize"/>
        public static uint Deserialize(string serialized) => _service.Deserialize(serialized);

        /// <inheritdoc cref="IObjectPluginService.GetDialogResref"/>
        public static string GetDialogResref(uint obj) => _service.GetDialogResref(obj);

        /// <inheritdoc cref="IObjectPluginService.SetDialogResref"/>
        public static void SetDialogResref(uint obj, string dialog) => _service.SetDialogResref(obj, dialog);

        /// <inheritdoc cref="IObjectPluginService.SetAppearance"/>
        public static void SetAppearance(uint placeable, int appearance) => _service.SetAppearance(placeable, appearance);

        /// <inheritdoc cref="IObjectPluginService.GetAppearance"/>
        public static int GetAppearance(uint obj) => _service.GetAppearance(obj);

        /// <inheritdoc cref="IObjectPluginService.GetHasVisualEffect"/>
        public static bool GetHasVisualEffect(uint obj, int nVfx) => _service.GetHasVisualEffect(obj, nVfx);

        /// <inheritdoc cref="IObjectPluginService.GetDamageImmunity"/>
        public static int GetDamageImmunity(uint obj, int damageType) => _service.GetDamageImmunity(obj, damageType);

        /// <inheritdoc cref="IObjectPluginService.AddToArea"/>
        public static void AddToArea(uint obj, uint area, Vector3 pos) => _service.AddToArea(obj, area, pos);

        /// <inheritdoc cref="IObjectPluginService.GetPlaceableIsStatic"/>
        public static bool GetPlaceableIsStatic(uint obj) => _service.GetPlaceableIsStatic(obj);

        /// <inheritdoc cref="IObjectPluginService.SetPlaceableIsStatic"/>
        public static void SetPlaceableIsStatic(uint obj, bool isStatic) => _service.SetPlaceableIsStatic(obj, isStatic);

        /// <inheritdoc cref="IObjectPluginService.GetAutoRemoveKey"/>
        public static bool GetAutoRemoveKey(uint obj) => _service.GetAutoRemoveKey(obj);

        /// <inheritdoc cref="IObjectPluginService.SetAutoRemoveKey"/>
        public static void SetAutoRemoveKey(uint obj, bool bRemoveKey) => _service.SetAutoRemoveKey(obj, bRemoveKey);

        /// <inheritdoc cref="IObjectPluginService.GetTriggerGeometry"/>
        public static string GetTriggerGeometry(uint oTrigger) => _service.GetTriggerGeometry(oTrigger);

        /// <inheritdoc cref="IObjectPluginService.SetTriggerGeometry"/>
        public static void SetTriggerGeometry(uint oTrigger, string sGeometry) => _service.SetTriggerGeometry(oTrigger, sGeometry);

        /// <inheritdoc cref="IObjectPluginService.Export"/>
        public static void Export(uint oObject, string sFileName, string sAlias = "NWNX") => _service.Export(oObject, sFileName, sAlias);

        /// <inheritdoc cref="IObjectPluginService.GetInt"/>
        public static int GetInt(uint obj, string variableName) => _service.GetInt(obj, variableName);

        /// <inheritdoc cref="IObjectPluginService.SetInt"/>
        public static void SetInt(uint obj, string variableName, int newValue, bool persist) => _service.SetInt(obj, variableName, newValue, persist);

        /// <inheritdoc cref="IObjectPluginService.DeleteInt"/>
        public static void DeleteInt(uint obj, string variableName) => _service.DeleteInt(obj, variableName);

        /// <inheritdoc cref="IObjectPluginService.GetString"/>
        public static string GetString(uint obj, string variableName) => _service.GetString(obj, variableName);

        /// <inheritdoc cref="IObjectPluginService.SetString"/>
        public static void SetString(uint obj, string variableName, string newValue, bool persist) => _service.SetString(obj, variableName, newValue, persist);

        /// <inheritdoc cref="IObjectPluginService.DeleteString"/>
        public static void DeleteString(uint obj, string variableName) => _service.DeleteString(obj, variableName);

        /// <inheritdoc cref="IObjectPluginService.GetFloat"/>
        public static float GetFloat(uint obj, string variableName) => _service.GetFloat(obj, variableName);

        /// <inheritdoc cref="IObjectPluginService.SetFloat"/>
        public static void SetFloat(uint obj, string variableName, float newValue, bool persist) => _service.SetFloat(obj, variableName, newValue, persist);

        /// <inheritdoc cref="IObjectPluginService.DeleteFloat"/>
        public static void DeleteFloat(uint obj, string variableName) => _service.DeleteFloat(obj, variableName);

        /// <inheritdoc cref="IObjectPluginService.DeleteVarRegex"/>
        public static void DeleteVarRegex(uint obj, string regexString) => _service.DeleteVarRegex(obj, regexString);

        /// <inheritdoc cref="IObjectPluginService.GetPositionIsInTrigger"/>
        public static bool GetPositionIsInTrigger(uint obj, Vector3 position) => _service.GetPositionIsInTrigger(obj, position);

        /// <inheritdoc cref="IObjectPluginService.GetInternalObjectType"/>
        public static InternalObjectType GetInternalObjectType(uint oObject) => _service.GetInternalObjectType(oObject);

        /// <inheritdoc cref="IObjectPluginService.AcquireItem"/>
        public static bool AcquireItem(uint oObject, uint oItem) => _service.AcquireItem(oObject, oItem);

        /// <inheritdoc cref="IObjectPluginService.DoSpellImmunity"/>
        public static int DoSpellImmunity(uint oDefender, uint oCaster, int nSpellId = -1) => _service.DoSpellImmunity(oDefender, oCaster, nSpellId);

        /// <inheritdoc cref="IObjectPluginService.DoSpellLevelAbsorption"/>
        public static int DoSpellLevelAbsorption(uint oDefender, uint oCaster, int nSpellId = -1, int nSpellLevel = -1, int nSpellSchool = -1) => _service.DoSpellLevelAbsorption(oDefender, oCaster, nSpellId, nSpellLevel, nSpellSchool);

        /// <inheritdoc cref="IObjectPluginService.GetCurrentHitPoints"/>
        public static int GetCurrentHitPoints(uint creature) => _service.GetCurrentHitPoints(creature);

        /// <inheritdoc cref="IObjectPluginService.GetDoorHasVisibleModel"/>
        public static bool GetDoorHasVisibleModel(uint oDoor) => _service.GetDoorHasVisibleModel(oDoor);

        /// <inheritdoc cref="IObjectPluginService.GetIsDestroyable"/>
        public static bool GetIsDestroyable(uint oObject) => _service.GetIsDestroyable(oObject);

        /// <inheritdoc cref="IObjectPluginService.ClearSpellEffectsOnOthers"/>
        public static void ClearSpellEffectsOnOthers(uint oObject) => _service.ClearSpellEffectsOnOthers(oObject);

        /// <inheritdoc cref="IObjectPluginService.PeekUUID"/>
        public static string PeekUUID(uint oObject) => _service.PeekUUID(oObject);

        /// <inheritdoc cref="IObjectPluginService.SetHasInventory"/>
        public static void SetHasInventory(uint obj, bool bHasInventory) => _service.SetHasInventory(obj, bHasInventory);

        /// <inheritdoc cref="IObjectPluginService.GetCurrentAnimation"/>
        public static int GetCurrentAnimation(uint oObject) => _service.GetCurrentAnimation(oObject);

        /// <inheritdoc cref="IObjectPluginService.GetAILevel"/>
        public static int GetAILevel(uint oObject) => _service.GetAILevel(oObject);

        /// <inheritdoc cref="IObjectPluginService.SetAILevel"/>
        public static void SetAILevel(uint oObject, int nLevel) => _service.SetAILevel(oObject, nLevel);

        /// <inheritdoc cref="IObjectPluginService.GetMapNote"/>
        public static string GetMapNote(uint oObject, int nID = 0, int nGender = 0) => _service.GetMapNote(oObject, nID, nGender);

        /// <inheritdoc cref="IObjectPluginService.SetMapNote"/>
        public static void SetMapNote(uint oObject, string sMapNote, int nID = 0, int nGender = 0) => _service.SetMapNote(oObject, sMapNote, nID, nGender);

        /// <inheritdoc cref="IObjectPluginService.GetLastSpellCastFeat"/>
        public static int GetLastSpellCastFeat(uint oObject) => _service.GetLastSpellCastFeat(oObject);

        /// <inheritdoc cref="IObjectPluginService.SetLastTriggered"/>
        public static void SetLastTriggered(uint oObject, uint oLast) => _service.SetLastTriggered(oObject, oLast);

        /// <inheritdoc cref="IObjectPluginService.GetAoEObjectDurationRemaining"/>
        public static float GetAoEObjectDurationRemaining(uint oAoE) => _service.GetAoEObjectDurationRemaining(oAoE);

        /// <inheritdoc cref="IObjectPluginService.SetConversationPrivate"/>
        public static void SetConversationPrivate(uint oObject, bool bPrivate) => _service.SetConversationPrivate(oObject, bPrivate);

        /// <inheritdoc cref="IObjectPluginService.SetAoEObjectRadius"/>
        public static void SetAoEObjectRadius(uint oAoE, float fRadius) => _service.SetAoEObjectRadius(oAoE, fRadius);

        /// <inheritdoc cref="IObjectPluginService.GetAoEObjectRadius"/>
        public static float GetAoEObjectRadius(uint oAoE) => _service.GetAoEObjectRadius(oAoE);

        /// <inheritdoc cref="IObjectPluginService.GetLastSpellCastSpontaneous"/>
        public static bool GetLastSpellCastSpontaneous(uint oObject) => _service.GetLastSpellCastSpontaneous(oObject);

        /// <inheritdoc cref="IObjectPluginService.GetLastSpellCastDomainLevel"/>
        public static int GetLastSpellCastDomainLevel(uint oObject) => _service.GetLastSpellCastDomainLevel(oObject);

        /// <inheritdoc cref="IObjectPluginService.ForceAssignUUID"/>
        public static void ForceAssignUUID(uint oObject, string sUUID) => _service.ForceAssignUUID(oObject, sUUID);

        /// <inheritdoc cref="IObjectPluginService.GetInventoryItemCount"/>
        public static int GetInventoryItemCount(uint oObject) => _service.GetInventoryItemCount(oObject);

        /// <inheritdoc cref="IObjectPluginService.OverrideSpellProjectileVFX"/>
        public static void OverrideSpellProjectileVFX(uint oCreature, int nProjectileType = -1, int nProjectilePathType = -1, int nSpellID = -1, bool bPersist = false) => _service.OverrideSpellProjectileVFX(oCreature, nProjectileType, nProjectilePathType, nSpellID, bPersist);

        /// <inheritdoc cref="IObjectPluginService.GetLastSpellInstant"/>
        public static bool GetLastSpellInstant() => _service.GetLastSpellInstant();

        /// <inheritdoc cref="IObjectPluginService.SetTrapCreator"/>
        public static void SetTrapCreator(uint oObject, uint oCreator) => _service.SetTrapCreator(oObject, oCreator);

        /// <inheritdoc cref="IObjectPluginService.GetLocalizedName"/>
        public static string GetLocalizedName(uint oObject, int nLanguage, int nGender = 0) => _service.GetLocalizedName(oObject, nLanguage, nGender);

        /// <inheritdoc cref="IObjectPluginService.SetLocalizedName"/>
        public static void SetLocalizedName(uint oObject, string sName, int nLanguage, int nGender = 0) => _service.SetLocalizedName(oObject, sName, nLanguage, nGender);
    }
}
