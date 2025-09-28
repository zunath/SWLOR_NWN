using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        private readonly Dictionary<uint, string> _objectTags = new();
        private readonly Dictionary<uint, string> _objectNames = new();
        private readonly Dictionary<uint, bool> _objectIsValid = new();
        private readonly Dictionary<uint, ObjectType> _objectTypes = new();
        private readonly Dictionary<uint, string> _objectDescriptions = new();
        private readonly Dictionary<uint, int> _objectPortraitIds = new();
        private readonly Dictionary<uint, string> _objectPortraitResRefs = new();

        private readonly Dictionary<uint, int> _objectColors = new();
        private readonly Dictionary<uint, string> _objectKeyRequiredFeedback = new();
        private readonly Dictionary<uint, bool> _objectLocked = new();
        private readonly Dictionary<uint, bool> _objectCommandable = new();
        private readonly Dictionary<uint, bool> _objectListening = new();
        private readonly Dictionary<uint, string> _objectListenPatterns = new();
        private readonly Dictionary<uint, int> _objectListenPatternNumbers = new();
        private readonly Dictionary<uint, bool> _objectPlotFlag = new();
        private readonly Dictionary<uint, int> _objectHardness = new();
        private readonly Dictionary<uint, bool> _objectKeyRequired = new();
        private readonly Dictionary<uint, string> _objectKeyTags = new();
        private readonly Dictionary<uint, bool> _objectLockable = new();
        private readonly Dictionary<uint, int> _objectUnlockDC = new();
        private readonly Dictionary<uint, int> _objectLockDC = new();
        private readonly Dictionary<uint, int> _objectWillSavingThrow = new();
        private readonly Dictionary<uint, int> _objectReflexSavingThrow = new();
        private readonly Dictionary<uint, int> _objectFortitudeSavingThrow = new();
        private readonly Dictionary<uint, float> _objectWeight = new();
        private readonly Dictionary<uint, bool> _objectHasInventory = new();
        private readonly Dictionary<uint, string> _objectResRefs = new();
        private readonly Dictionary<uint, uint> _objectTransitionTargets = new();
        private readonly Dictionary<uint, uint> _objectSittingCreatures = new();
        private readonly Dictionary<uint, uint> _objectLastClickedBy = new();
        private readonly Dictionary<uint, uint> _objectLastDisarmed = new();
        private readonly Dictionary<uint, uint> _objectLastDisturbed = new();
        private readonly Dictionary<uint, uint> _objectLastLocked = new();
        private readonly Dictionary<uint, uint> _objectLastUnlocked = new();
        private readonly Dictionary<uint, uint> _objectClickingObject = new();
        private readonly Dictionary<uint, bool> _objectUseableFlag = new();
        private readonly Dictionary<uint, int> _objectAC = new();
        private readonly Dictionary<uint, int> _objectCurrentHitPoints = new();
        private readonly Dictionary<uint, int> _objectMaxHitPoints = new();
        private readonly Dictionary<uint, float> _objectDistanceBetween = new();
        private readonly Dictionary<uint, Vector3> _objectFacingPoints = new();
        private int _nextObjectId = 1000;

        public void SetTag(uint oObject, string sNewTag) { _objectTags[oObject] = sNewTag; }
        public string GetTag(uint oObject) => _objectTags.GetValueOrDefault(oObject, "");
        public void SetName(uint oObject, string sNewName = "") { _objectNames[oObject] = sNewName; }
        public string GetName(uint oObject, bool bOriginalName = false) => _objectNames.GetValueOrDefault(oObject, "");
        public bool GetIsObjectValid(uint oObject) => _objectIsValid.GetValueOrDefault(oObject, false);
        public ObjectType GetObjectType(uint oTarget) => _objectTypes.GetValueOrDefault(oTarget, ObjectType.Invalid);
        public uint CreateObject(ObjectType nObjectType, string sTemplate, Location lLocation, bool bUseAppearAnimation = true, string sNewTag = "") { var newObject = (uint)(_nextObjectId++); _objectTypes[newObject] = nObjectType; _objectTags[newObject] = sNewTag; _objectNames[newObject] = sTemplate; _objectIsValid[newObject] = true; return newObject; }
        public uint GetNearestObject(ObjectType nObjectType = ObjectType.All, uint oTarget = OBJECT_INVALID, int nNth = 1) => (uint)(_nextObjectId++);
        public uint GetObjectByTag(string sTag, int nNth = 0) => (uint)(_nextObjectId++);
        public string ObjectToString(uint oObject) => oObject.ToString("X");
        public uint StringToObject(string sHex) { if (uint.TryParse(sHex, System.Globalization.NumberStyles.HexNumber, null, out uint result)) return result; return OBJECT_INVALID; }
        
        // Object management methods
        public uint CopyObject(uint oSource, Location locLocation, uint oOwner = OBJECT_INVALID, string sNewTag = "", bool bCopyLocalState = false) 
        { 
            var newObject = (uint)(_nextObjectId++);
            _objectTypes[newObject] = _objectTypes.GetValueOrDefault(oSource, ObjectType.Invalid);
            _objectTags[newObject] = sNewTag;
            _objectNames[newObject] = _objectNames.GetValueOrDefault(oSource, "");
            _objectIsValid[newObject] = true;
            return newObject;
        }
        
        public void DestroyObject(uint oDestroy, float fDelay = 0.0f) 
        { 
            _objectIsValid[oDestroy] = false;
        }
        
        public void ExecuteNWScript(string sScript, uint oTarget) 
        { 
            // Mock implementation - no-op for testing
        }

        // Portrait methods
        public int GetPortraitId(uint oTarget = OBJECT_INVALID) => _objectPortraitIds.GetValueOrDefault(oTarget == OBJECT_INVALID ? OBJECT_SELF : oTarget, 0);
        public void SetPortraitId(uint oTarget, int nPortraitId) => _objectPortraitIds[oTarget] = nPortraitId;
        public string GetPortraitResRef(uint oTarget = OBJECT_INVALID) => _objectPortraitResRefs.GetValueOrDefault(oTarget == OBJECT_INVALID ? OBJECT_SELF : oTarget, "");
        public void SetPortraitResRef(uint oTarget, string sPortraitResRef) => _objectPortraitResRefs[oTarget] = sPortraitResRef;

        // Click and interaction methods
        public uint GetClickingObject() => _objectClickingObject.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetPlaceableLastClickedBy() => _objectLastClickedBy.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetLastDisarmed() => _objectLastDisarmed.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetLastDisturbed(uint oObject = OBJECT_INVALID) => _objectLastDisturbed.GetValueOrDefault(oObject == OBJECT_INVALID ? OBJECT_SELF : oObject, OBJECT_INVALID);
        public uint GetLastLocked(uint oObject = OBJECT_INVALID) => _objectLastLocked.GetValueOrDefault(oObject == OBJECT_INVALID ? OBJECT_SELF : oObject, OBJECT_INVALID);
        public uint GetLastUnlocked(uint oObject = OBJECT_INVALID) => _objectLastUnlocked.GetValueOrDefault(oObject == OBJECT_INVALID ? OBJECT_SELF : oObject, OBJECT_INVALID);

        // Description methods
        public string GetDescription(uint oObject, bool bOriginalDescription = false, bool bIdentifiedDescription = true) => _objectDescriptions.GetValueOrDefault(oObject, "");
        public void SetDescription(uint oObject, string sNewDescription = "", bool bIdentifiedDescription = true) => _objectDescriptions[oObject] = sNewDescription;

        // Color methods
        public int GetColor(uint oObject, ColorChannelType nColorChannel) => _objectColors.GetValueOrDefault(oObject, 0);
        public void SetColor(uint oObject, ColorChannelType nColorChannel, int nColorValue) => _objectColors[oObject] = nColorValue;

        // Key and lock methods
        public string GetKeyRequiredFeedback(uint oObject) => _objectKeyRequiredFeedback.GetValueOrDefault(oObject, "");
        public void SetKeyRequiredFeedback(uint oObject, string sFeedbackMessage) => _objectKeyRequiredFeedback[oObject] = sFeedbackMessage;
        public bool GetLocked(uint oTarget) => _objectLocked.GetValueOrDefault(oTarget, false);
        public void SetLocked(uint oTarget, bool nLocked) => _objectLocked[oTarget] = nLocked;
        public void SetLockKeyRequired(uint oObject, bool nKeyRequired = true) => _objectKeyRequired[oObject] = nKeyRequired;

        // Camera methods
        public void LockCameraPitch(uint oPlayer, bool bLocked = true) { }
        public void LockCameraDistance(uint oPlayer, bool bLocked = true) { }
        public void LockCameraDirection(uint oPlayer, bool bLocked = true) { }

        // Hardness methods
        public int GetHardness(uint oObject = OBJECT_INVALID) => _objectHardness.GetValueOrDefault(oObject == OBJECT_INVALID ? OBJECT_SELF : oObject, 0);
        public void SetHardness(int nHardness, uint oObject = OBJECT_INVALID) => _objectHardness[oObject == OBJECT_INVALID ? OBJECT_SELF : oObject] = nHardness;

        // Saving throw methods
        public void SetWillSavingThrow(uint oObject, int nWillSave) => _objectWillSavingThrow[oObject] = nWillSave;
        public void SetReflexSavingThrow(uint oObject, int nReflexSave) => _objectReflexSavingThrow[oObject] = nReflexSave;
        public void SetFortitudeSavingThrow(uint oObject, int nFortitudeSave) => _objectFortitudeSavingThrow[oObject] = nFortitudeSave;

        // Weight and inventory methods
        public int GetWeight(uint oTarget = OBJECT_INVALID) => (int)_objectWeight.GetValueOrDefault(oTarget == OBJECT_INVALID ? OBJECT_SELF : oTarget, 0.0f);
        public bool GetHasInventory(uint oObject) => _objectHasInventory.GetValueOrDefault(oObject, false);

        // Module item methods
        public uint GetModuleItemAcquiredBy() => OBJECT_INVALID;

        // Sound and speech methods

        // Object state methods
        public void SetIsDestroyable(bool bDestroyable = true, bool bRaiseable = true, bool bSelectableWhenDead = false, uint oObject = OBJECT_INVALID) { }
        public void SetUseableFlag(uint oPlaceable, bool nUseable) => _objectUseableFlag[oPlaceable] = nUseable;

        // Object finding methods
        public uint GetNearestObjectToLocation(Location lLocation, ObjectType nObjectType = ObjectType.All, int nNth = 1) => (uint)(_nextObjectId++);
        public uint GetNearestObjectByTag(string sTag, uint oTarget = OBJECT_INVALID, int nNth = 1) => (uint)(_nextObjectId++);

        // Object properties
        public int GetAC(uint oObject) => _objectAC.GetValueOrDefault(oObject, 10);
        public int GetCurrentHitPoints(uint oObject = OBJECT_INVALID) => _objectCurrentHitPoints.GetValueOrDefault(oObject == OBJECT_INVALID ? OBJECT_SELF : oObject, 100);
        public int GetMaxHitPoints(uint oObject = OBJECT_INVALID) => _objectMaxHitPoints.GetValueOrDefault(oObject == OBJECT_INVALID ? OBJECT_SELF : oObject, 100);
        public void SetCurrentHitPoints(uint oObject, int nHitPoints) => _objectCurrentHitPoints[oObject] = nHitPoints;

        // Shape methods
        public uint GetFirstObjectInShape(ShapeType nShape, float fSize, Location lTarget, bool bLineOfSight = false, ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default) => (uint)(_nextObjectId++);
        public uint GetNextObjectInShape(ShapeType nShape, float fSize, Location lTarget, bool bLineOfSight = false, ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default) => (uint)(_nextObjectId++);

        // Movement and facing
        public void SetFacingPoint(Vector3 vTarget, uint oObject = OBJECT_INVALID) => _objectFacingPoints[oObject == OBJECT_INVALID ? OBJECT_SELF : oObject] = vTarget;
        public float GetDistanceBetween(uint oObjectA, uint oObjectB) => _objectDistanceBetween.GetValueOrDefault(oObjectA, 0.0f);

        // Commandable methods
        public void SetCommandable(bool nCommandable, uint oTarget = OBJECT_INVALID) => _objectCommandable[oTarget == OBJECT_INVALID ? OBJECT_SELF : oTarget] = nCommandable;
        public bool GetCommandable(uint oTarget = OBJECT_INVALID) => _objectCommandable.GetValueOrDefault(oTarget == OBJECT_INVALID ? OBJECT_SELF : oTarget, true);

        // Listening methods
        public bool GetIsListening(uint oObject) => _objectListening.GetValueOrDefault(oObject, false);
        public void SetListening(uint oObject, bool bValue) => _objectListening[oObject] = bValue;
        public void SetListenPattern(uint oObject, string sPattern, int nNumber = 0) => _objectListenPatterns[oObject] = sPattern;
        public int GetListenPatternNumber() => _objectListenPatternNumbers.GetValueOrDefault(OBJECT_SELF, 0);

        // Waypoint and transition methods
        public uint GetWaypointByTag(string sWaypointTag) => (uint)(_nextObjectId++);
        public uint GetTransitionTarget(uint oTransition) => _objectTransitionTargets.GetValueOrDefault(oTransition, OBJECT_INVALID);

        // Sitting methods
        public uint GetSittingCreature(uint oChair) => _objectSittingCreatures.GetValueOrDefault(oChair, OBJECT_INVALID);

        // Speech methods
        public void SpeakString(string sStringToSpeak, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) 
        {
            // Mock implementation - speech tracking handled in ChatFunctions.cs
        }
        
        public void SpeakStringByStrRef(int nStrRef, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) 
        {
            // Mock implementation - speech tracking handled in ChatFunctions.cs
        }
        
        public void SpeakOneLinerConversation(string sDialogResRef = "", uint oTokenTarget = OBJECT_INVALID) 
        {
            // Mock implementation - speech tracking handled in ChatFunctions.cs
        }

        // ResRef methods
        public string GetResRef(uint oObject) => _objectResRefs.GetValueOrDefault(oObject, "");

        // Texture methods
        public void ReplaceObjectTexture(uint oObject, string sOld, string sNew = "") { }

        // Helper methods for testing
        public void SetObjectValid(uint oObject, bool isValid) => _objectIsValid[oObject] = isValid;
        public void SetObjectType(uint oObject, ObjectType type) => _objectTypes[oObject] = type;
        public void SetObjectAC(uint oObject, int ac) => _objectAC[oObject] = ac;
        public void SetObjectCurrentHitPoints(uint oObject, int hitPoints) => _objectCurrentHitPoints[oObject] = hitPoints;
        public void SetObjectMaxHitPoints(uint oObject, int hitPoints) => _objectMaxHitPoints[oObject] = hitPoints;
        public void SetObjectWeight(uint oObject, float weight) => _objectWeight[oObject] = weight;
        public void SetObjectHasInventory(uint oObject, bool hasInventory) => _objectHasInventory[oObject] = hasInventory;
        public void SetObjectCommandable(uint oObject, bool commandable) => _objectCommandable[oObject] = commandable;
        public void SetObjectPlotFlag(uint oObject, bool plotFlag) => _objectPlotFlag[oObject] = plotFlag;
        public void SetObjectLocked(uint oObject, bool locked) => _objectLocked[oObject] = locked;
        public void SetObjectUseableFlag(uint oObject, bool useable) => _objectUseableFlag[oObject] = useable;
        public void SetObjectDistanceBetween(uint oObjectA, uint oObjectB, float distance) => _objectDistanceBetween[oObjectA] = distance;
        public void SetObjectFacingPoint(uint oObject, Vector3 facing) => _objectFacingPoints[oObject] = facing;
        public void SetObjectResRef(uint oObject, string resRef) => _objectResRefs[oObject] = resRef;
        public void SetObjectTransitionTarget(uint oObject, uint target) => _objectTransitionTargets[oObject] = target;
        public void SetObjectSittingCreature(uint oChair, uint oCreature) => _objectSittingCreatures[oChair] = oCreature;
        public void SetObjectLastClickedBy(uint oObject, uint oClicker) => _objectLastClickedBy[oObject] = oClicker;
        public void SetObjectLastDisarmed(uint oObject, uint oDisarmer) => _objectLastDisarmed[oObject] = oDisarmer;
        public void SetObjectLastDisturbed(uint oObject, uint oDisturber) => _objectLastDisturbed[oObject] = oDisturber;
        public void SetObjectLastLocked(uint oObject, uint oLocker) => _objectLastLocked[oObject] = oLocker;
        public void SetObjectLastUnlocked(uint oObject, uint oUnlocker) => _objectLastUnlocked[oObject] = oUnlocker;
        public void SetObjectClickingObject(uint oObject, uint oClicker) => _objectClickingObject[oObject] = oClicker;
        public void SetObjectHardness(uint oObject, int hardness) => _objectHardness[oObject] = hardness;
        public void SetObjectKeyRequired(uint oObject, bool keyRequired) => _objectKeyRequired[oObject] = keyRequired;
        public void SetObjectKeyTag(uint oObject, string keyTag) => _objectKeyTags[oObject] = keyTag;
        public void SetObjectLockable(uint oObject, bool lockable) => _objectLockable[oObject] = lockable;
        public void SetObjectUnlockDC(uint oObject, int dc) => _objectUnlockDC[oObject] = dc;
        public void SetObjectLockDC(uint oObject, int dc) => _objectLockDC[oObject] = dc;
        public void SetObjectWillSavingThrow(uint oObject, int save) => _objectWillSavingThrow[oObject] = save;
        public void SetObjectReflexSavingThrow(uint oObject, int save) => _objectReflexSavingThrow[oObject] = save;
        public void SetObjectFortitudeSavingThrow(uint oObject, int save) => _objectFortitudeSavingThrow[oObject] = save;
        public void SetObjectKeyRequiredFeedback(uint oObject, string feedback) => _objectKeyRequiredFeedback[oObject] = feedback;
        public void SetObjectListening(uint oObject, bool listening) => _objectListening[oObject] = listening;
        public void SetObjectListenPattern(uint oObject, string pattern) => _objectListenPatterns[oObject] = pattern;
        public void SetObjectListenPatternNumber(uint oObject, int number) => _objectListenPatternNumbers[oObject] = number;
        public void SetObjectDescription(uint oObject, string description) => _objectDescriptions[oObject] = description;
        public void SetObjectPortraitId(uint oObject, int portraitId) => _objectPortraitIds[oObject] = portraitId;
        public void SetObjectPortraitResRef(uint oObject, string resRef) => _objectPortraitResRefs[oObject] = resRef;
        public void SetObjectColor(uint oObject, int color) => _objectColors[oObject] = color;
        public void SetObjectName(uint oObject, string name) => _objectNames[oObject] = name;
        public void SetObjectTag(uint oObject, string tag) => _objectTags[oObject] = tag;
    }
}
