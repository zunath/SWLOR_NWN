using System.Collections.Generic;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // MovementActionType enum
        public enum MovementActionType
        {
            Invalid = 0,
            Walk = 1,
            Run = 2,
            Jump = 3
        }
        
        // Mock data storage for movement
        private readonly Dictionary<uint, MovementData> _movementData = new();
        private readonly List<MovementActionRecord> _movementHistory = new();

        private class MovementData
        {
            public float Facing { get; set; } = 0.0f;
            public Location Position { get; set; } = new Location(0);
        }

        private class MovementActionRecord
        {
            public uint Object { get; set; }
            public MovementActionType Action { get; set; }
            public Location TargetLocation { get; set; }
            public uint TargetObject { get; set; }
            public bool Run { get; set; }
            public float Range { get; set; }
        }

        public void SetFacing(float fDirection, uint oObject = OBJECT_INVALID) 
        {
            var data = GetOrCreateMovementData(oObject);
            data.Facing = fDirection;
        }

        public float GetFacing(uint oTarget) => 
            _movementData.GetValueOrDefault(oTarget, new MovementData()).Facing;

        public void ActionMoveAwayFromLocation(Location lMoveAwayFrom, bool bRun = false, float fMoveAwayRange = 40.0f) 
        {
            _movementHistory.Add(new MovementActionRecord 
            { 
                Object = OBJECT_SELF,
                Action = MovementActionType.Walk,
                TargetLocation = lMoveAwayFrom,
                Run = bRun,
                Range = fMoveAwayRange
            });
        }

        public void JumpToObject(uint oToJumpTo, bool nWalkStraightLineToPoint = true) 
        {
            _movementHistory.Add(new MovementActionRecord 
            { 
                Object = OBJECT_SELF,
                Action = MovementActionType.Jump,
                TargetObject = oToJumpTo
            });
        }

        private MovementData GetOrCreateMovementData(uint oObject)
        {
            if (!_movementData.ContainsKey(oObject))
                _movementData[oObject] = new MovementData();
            return _movementData[oObject];
        }

        // Helper methods for testing

    }
}
