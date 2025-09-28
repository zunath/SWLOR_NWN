using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for doors
        private readonly Dictionary<uint, DoorData> _doorData = new();
        private readonly List<DoorActionRecord> _doorActionHistory = new();
        private uint _blockingDoor = OBJECT_INVALID;

        private class DoorData
        {
            public bool IsOpen { get; set; } = false;
            public bool IsLocked { get; set; } = false;
            public bool IsUnlocked { get; set; } = true;
        }

        private class DoorActionRecord
        {
            public DoorActionType Action { get; set; }
            public uint Target { get; set; }
        }

        public bool GetIsOpen(uint oObject) => 
            _doorData.GetValueOrDefault(oObject, new DoorData()).IsOpen;

        public void ActionUnlockObject(uint oTarget) 
        {
            var data = GetOrCreateDoorData(oTarget);
            data.IsLocked = false;
            data.IsUnlocked = true;
            
            _doorActionHistory.Add(new DoorActionRecord 
            { 
                Action = DoorActionType.Unlock, 
                Target = oTarget 
            });
        }

        public void ActionLockObject(uint oTarget) 
        {
            var data = GetOrCreateDoorData(oTarget);
            data.IsLocked = true;
            data.IsUnlocked = false;
            
            _doorActionHistory.Add(new DoorActionRecord 
            { 
                Action = DoorActionType.Unlock, 
                Target = oTarget 
            });
        }

        public void ActionOpenDoor(uint oDoor) 
        {
            var data = GetOrCreateDoorData(oDoor);
            data.IsOpen = true;
            
            _doorActionHistory.Add(new DoorActionRecord 
            { 
                Action = DoorActionType.Open, 
                Target = oDoor 
            });
        }

        public void ActionCloseDoor(uint oDoor) 
        {
            var data = GetOrCreateDoorData(oDoor);
            data.IsOpen = false;
            
            _doorActionHistory.Add(new DoorActionRecord 
            { 
                Action = DoorActionType.Open, 
                Target = oDoor 
            });
        }

        public uint GetBlockingDoor() => _blockingDoor;

        public bool GetIsDoorActionPossible(uint oTargetDoor, DoorActionType nDoorAction) 
        {
            var data = GetOrCreateDoorData(oTargetDoor);
            
            return nDoorAction switch
            {
                DoorActionType.Open => !data.IsOpen,
                DoorActionType.Unlock => data.IsLocked,
                DoorActionType.Bash => data.IsOpen,
                DoorActionType.Ignore => data.IsOpen,
                DoorActionType.Knock => data.IsOpen,
                _ => true
            };
        }

        public void DoDoorAction(uint oTargetDoor, DoorActionType nDoorAction) 
        {
            var data = GetOrCreateDoorData(oTargetDoor);
            
            switch (nDoorAction)
            {
                case DoorActionType.Open:
                    data.IsOpen = true;
                    break;
                case DoorActionType.Bash:
                    // Bash action - no state change
                    break;
                case DoorActionType.Ignore:
                    // Ignore action - no state change
                    break;
                case DoorActionType.Knock:
                    // Knock action - no state change
                    break;
                case DoorActionType.Unlock:
                    data.IsLocked = false;
                    data.IsUnlocked = true;
                    break;
            }
            
            _doorActionHistory.Add(new DoorActionRecord 
            { 
                Action = nDoorAction, 
                Target = oTargetDoor 
            });
        }

        private DoorData GetOrCreateDoorData(uint oDoor)
        {
            if (!_doorData.ContainsKey(oDoor))
                _doorData[oDoor] = new DoorData();
            return _doorData[oDoor];
        }

        // Helper methods for testing


    }
}
