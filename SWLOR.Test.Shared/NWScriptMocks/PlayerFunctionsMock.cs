using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        private readonly Dictionary<uint, bool> _playerIsPC = new();
        private readonly Dictionary<uint, bool> _playerIsDM = new();
        private readonly Dictionary<uint, string> _playerNames = new();
        private readonly Dictionary<uint, string> _playerPublicCDKeys = new();
        private readonly Dictionary<uint, string> _playerCDKeys = new();
        private readonly Dictionary<uint, string> _playerIPAddresses = new();
        private readonly Dictionary<uint, int> _playerXP = new();
        private readonly Dictionary<uint, int> _playerGold = new();
        private readonly Dictionary<uint, bool> _playerIsInCombat = new();
        private readonly Dictionary<uint, uint> _playerLastAttacker = new();
        private readonly Dictionary<uint, uint> _playerLastDamager = new();
        private readonly Dictionary<uint, uint> _playerLastKiller = new();
        private readonly Dictionary<uint, bool> _cutsceneMode = new();
        private readonly Dictionary<uint, float> _cameraHeight = new();
        private readonly Dictionary<uint, float> _cutsceneCameraMoveRate = new();
        private readonly Dictionary<uint, int> _bodyBags = new();
        private readonly Dictionary<uint, int> _playerBuildVersionMajor = new();
        private readonly Dictionary<uint, int> _playerBuildVersionMinor = new();
        private readonly Dictionary<uint, int> _playerBuildVersionPostfix = new();
        private readonly Dictionary<uint, string> _playerBuildVersionCommitSha1 = new();
        private readonly Dictionary<uint, PlayerLanguageType> _playerLanguage = new();
        private readonly Dictionary<uint, PlayerDevicePlatformType> _playerDevicePlatform = new();
        private readonly Dictionary<uint, int> _playerNetworkLatency = new();
        private readonly Dictionary<uint, int> _playerDeviceProperty = new();
        private readonly Dictionary<uint, GuiEventType> _lastGuiEventType = new();
        private readonly Dictionary<uint, int> _lastGuiEventInteger = new();
        private readonly Dictionary<uint, uint> _lastGuiEventObject = new();
        private readonly Dictionary<uint, uint> _lastGuiEventPlayer = new();
        private readonly Dictionary<uint, uint> _lastPCToCancelCutscene = new();
        private readonly Dictionary<uint, uint> _lastPlayerDied = new();
        private readonly Dictionary<uint, uint> _lastPlayerDying = new();
        private readonly Dictionary<uint, uint> _lastPlayerToDoTileAction = new();
        private readonly Dictionary<uint, int> _lastTileActionId = new();
        private readonly Dictionary<uint, Vector3> _lastTileActionPosition = new();
        private readonly Dictionary<uint, uint> _lastPCItemEquipped = new();
        private readonly Dictionary<uint, uint> _lastPCItemEquippedBy = new();
        private readonly Dictionary<uint, uint> _lastPCItemUnequipped = new();
        private readonly Dictionary<uint, uint> _lastPCItemUnequippedBy = new();
        private readonly Dictionary<uint, InventorySlotType> _lastPCItemEquippedSlot = new();
        private readonly Dictionary<uint, InventorySlotType> _lastPCItemUnequippedSlot = new();
        private readonly Dictionary<uint, uint> _lastPCLevellingUp = new();
        private readonly Dictionary<uint, uint> _lastPCSpeaker = new();
        private readonly Dictionary<uint, uint> _lastModuleItemAcquiredBy = new();
        private readonly Dictionary<uint, uint> _lastModuleItemLost = new();
        private readonly Dictionary<uint, uint> _lastModuleItemLostBy = new();
        private readonly Dictionary<uint, List<int>> _playerStrRefMessages = new();

        // Cutscene and camera methods
        public bool GetCutsceneMode(uint oCreature = OBJECT_INVALID) => _cutsceneMode.GetValueOrDefault(oCreature, false);
        public void SetCameraHeight(uint oPlayer, float fHeight = 0.0f) => _cameraHeight[oPlayer] = fHeight;
        public float GetCutsceneCameraMoveRate(uint oCreature) => _cutsceneCameraMoveRate.GetValueOrDefault(oCreature, 1.0f);
        public void SetCutsceneCameraMoveRate(uint oCreature, float fRate) => _cutsceneCameraMoveRate[oCreature] = fRate;
        public void SetCutsceneMode(uint oCreature, bool nInCutscene = true, bool nLeftClickingEnabled = false) => _cutsceneMode[oCreature] = nInCutscene;

        // Item equipment methods
        public uint GetPCItemLastEquipped() => _lastPCItemEquipped.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetPCItemLastEquippedBy() => _lastPCItemEquippedBy.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetPCItemLastUnequipped() => _lastPCItemUnequipped.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetPCItemLastUnequippedBy() => _lastPCItemUnequippedBy.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public InventorySlotType GetPCItemLastEquippedSlot() => _lastPCItemEquippedSlot.GetValueOrDefault(OBJECT_SELF, InventorySlotType.Invalid);
        public InventorySlotType GetPCItemLastUnequippedSlot() => _lastPCItemUnequippedSlot.GetValueOrDefault(OBJECT_SELF, InventorySlotType.Invalid);

        // Message and GUI methods
        public void StoreCameraFacing() { }
        public void RestoreCameraFacing() { }
        public void PopUpGUIPanel(uint oPC, GuiPanelType nGUIPanel) { }
        public void SetGuiPanelDisabled(uint oPlayer, GuiPanelType nGuiPanel, bool bDisabled, uint oTarget = OBJECT_INVALID) { }

        // Player iteration methods
        public uint GetFirstPC() => OBJECT_INVALID;
        public uint GetNextPC() => OBJECT_INVALID;
        public uint GetPCLevellingUp() => _lastPCLevellingUp.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetPCSpeaker() => _lastPCSpeaker.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);

        // Player information methods
        public int GetPlayerBuildVersionMajor(uint oPlayer) => _playerBuildVersionMajor.GetValueOrDefault(oPlayer, 1);
        public int GetPlayerBuildVersionMinor(uint oPlayer) => _playerBuildVersionMinor.GetValueOrDefault(oPlayer, 0);
        public int GetPlayerBuildVersionPostfix(uint oPlayer) => _playerBuildVersionPostfix.GetValueOrDefault(oPlayer, 0);
        public string GetPlayerBuildVersionCommitSha1(uint oPlayer) => _playerBuildVersionCommitSha1.GetValueOrDefault(oPlayer, "");
        public bool GetIsPlayerDM(uint oCreature) => _playerIsDM.GetValueOrDefault(oCreature, false);
        public PlayerLanguageType GetPlayerLanguage(uint oPlayer) => _playerLanguage.GetValueOrDefault(oPlayer, PlayerLanguageType.English);
        public PlayerDevicePlatformType GetPlayerDevicePlatform(uint oPlayer) => _playerDevicePlatform.GetValueOrDefault(oPlayer, PlayerDevicePlatformType.Windows_X64);
        public int GetPlayerDeviceProperty(uint oPlayer, string sProperty) => _playerDeviceProperty.GetValueOrDefault(oPlayer, 0);
        public int GetPlayerNetworkLatency(uint oPlayer) => _playerNetworkLatency.GetValueOrDefault(oPlayer, 0);

        // GUI event methods
        public uint GetLastGuiEventPlayer() => _lastGuiEventPlayer.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public GuiEventType GetLastGuiEventType() => _lastGuiEventType.GetValueOrDefault(OBJECT_SELF, GuiEventType.ChatBarFocus);
        public int GetLastGuiEventInteger() => _lastGuiEventInteger.GetValueOrDefault(OBJECT_SELF, 0);
        public uint GetLastGuiEventObject() => _lastGuiEventObject.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);

        // Tile action methods
        public int GetLastTileActionId() => _lastTileActionId.GetValueOrDefault(OBJECT_SELF, 0);
        public Vector3 GetLastTileActionPosition() => _lastTileActionPosition.GetValueOrDefault(OBJECT_SELF, new Vector3(0, 0, 0));
        public uint GetLastPlayerToDoTileAction() => _lastPlayerToDoTileAction.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);

        // Player state methods
        public uint GetLastPCToCancelCutscene() => _lastPCToCancelCutscene.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetLastPlayerDied() => _lastPlayerDied.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetLastPlayerDying() => _lastPlayerDying.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetModuleItemLost() => _lastModuleItemLost.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);
        public uint GetModuleItemLostBy() => _lastModuleItemLostBy.GetValueOrDefault(OBJECT_SELF, OBJECT_INVALID);

        // Player identification methods
        public string GetPCPlayerName(uint oPlayer) => _playerNames.GetValueOrDefault(oPlayer, "");
        public string GetPCPublicCDKey(uint oPlayer, bool nSinglePlayerCDKey = false) => _playerPublicCDKeys.GetValueOrDefault(oPlayer, "");
        public string GetPCIPAddress(uint oPlayer) => _playerIPAddresses.GetValueOrDefault(oPlayer, "");

        // Player interaction methods
        public void SetPCLike(uint oPlayer, uint oTarget) { }
        public void SetPCDislike(uint oPlayer, uint oTarget) { }
        public int GetIsPlayerConnectionRelayed(uint oPlayer) => 0;

        // Character export methods
        public void ExportAllCharacters() { }
        public void ExportSingleCharacter(uint oPlayer) { }

        // Body bag methods
        public int GetBodyBag(uint oCreature) => _bodyBags.GetValueOrDefault(oCreature, 0);
        public void SetBodyBag(uint oCreature, int oBodyBag) => _bodyBags[oCreature] = oBodyBag;

        // Camera methods
        public void SetCameraMode(uint oPlayer, int nCameraMode) { }
        public void PopUpDeathGUIPanel(uint oPC, bool bRespawnButtonEnabled = true, bool bWaitForHelpButtonEnabled = true, int nHelpStringReference = 0, string sHelpString = "") { }

        // Message methods
        
        public void SendMessageToPCByStrRef(uint oPlayer, int nStrRef) 
        {
            if (!_playerStrRefMessages.ContainsKey(oPlayer))
                _playerStrRefMessages[oPlayer] = new List<int>();
            _playerStrRefMessages[oPlayer].Add(nStrRef);
        }

        // Helper methods for testing
        public void SetPlayerName(uint oPlayer, string name) => _playerNames[oPlayer] = name;
        public void SetPlayerIsDM(uint oPlayer, bool isDM) => _playerIsDM[oPlayer] = isDM;
        public void SetPlayerLanguage(uint oPlayer, PlayerLanguageType language) => _playerLanguage[oPlayer] = language;
        public List<int> GetPlayerStrRefMessages(uint oPlayer) => _playerStrRefMessages.GetValueOrDefault(oPlayer, new List<int>());
        public void ClearPlayerStrRefMessages(uint oPlayer) 
        {
            if (_playerStrRefMessages.ContainsKey(oPlayer)) _playerStrRefMessages[oPlayer].Clear();
        }
        public void SetPlayerDevicePlatform(uint oPlayer, PlayerDevicePlatformType platform) => _playerDevicePlatform[oPlayer] = platform;
        public void SetPlayerNetworkLatency(uint oPlayer, int latency) => _playerNetworkLatency[oPlayer] = latency;
        public void SetLastGuiEventType(uint oPlayer, GuiEventType eventType) => _lastGuiEventType[oPlayer] = eventType;
        public void SetLastGuiEventInteger(uint oPlayer, int value) => _lastGuiEventInteger[oPlayer] = value;
        public void SetLastGuiEventObject(uint oPlayer, uint oObject) => _lastGuiEventObject[oPlayer] = oObject;
        public void SetLastGuiEventPlayer(uint oPlayer, uint oEventPlayer) => _lastGuiEventPlayer[oPlayer] = oEventPlayer;
        public void SetLastPCToCancelCutscene(uint oPlayer, uint oPC) => _lastPCToCancelCutscene[oPlayer] = oPC;
        public void SetLastPlayerDied(uint oPlayer, uint oPlayerDied) => _lastPlayerDied[oPlayer] = oPlayerDied;
        public void SetLastPlayerDying(uint oPlayer, uint oPlayerDying) => _lastPlayerDying[oPlayer] = oPlayerDying;
        public void SetLastPlayerToDoTileAction(uint oPlayer, uint oPlayerAction) => _lastPlayerToDoTileAction[oPlayer] = oPlayerAction;
        public void SetLastTileActionId(uint oPlayer, int actionId) => _lastTileActionId[oPlayer] = actionId;
        public void SetLastTileActionPosition(uint oPlayer, Vector3 position) => _lastTileActionPosition[oPlayer] = position;
        public void SetLastPCItemEquipped(uint oPlayer, uint oItem) => _lastPCItemEquipped[oPlayer] = oItem;
        public void SetLastPCItemEquippedBy(uint oPlayer, uint oPC) => _lastPCItemEquippedBy[oPlayer] = oPC;
        public void SetLastPCItemUnequipped(uint oPlayer, uint oItem) => _lastPCItemUnequipped[oPlayer] = oItem;
        public void SetLastPCItemUnequippedBy(uint oPlayer, uint oPC) => _lastPCItemUnequippedBy[oPlayer] = oPC;
        public void SetLastPCItemEquippedSlot(uint oPlayer, InventorySlotType slot) => _lastPCItemEquippedSlot[oPlayer] = slot;
        public void SetLastPCItemUnequippedSlot(uint oPlayer, InventorySlotType slot) => _lastPCItemUnequippedSlot[oPlayer] = slot;
        public void SetLastPCLevellingUp(uint oPlayer, uint oPC) => _lastPCLevellingUp[oPlayer] = oPC;
        public void SetLastPCSpeaker(uint oPlayer, uint oPC) => _lastPCSpeaker[oPlayer] = oPC;
        public void SetLastModuleItemAcquiredBy(uint oPlayer, uint oObject) => _lastModuleItemAcquiredBy[oPlayer] = oObject;
        public void SetLastModuleItemLost(uint oPlayer, uint oItem) => _lastModuleItemLost[oPlayer] = oItem;
        public void SetLastModuleItemLostBy(uint oPlayer, uint oObject) => _lastModuleItemLostBy[oPlayer] = oObject;
    }
}
