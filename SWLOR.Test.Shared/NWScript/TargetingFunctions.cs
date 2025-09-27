using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for targeting
        private readonly Dictionary<uint, TargetingData> _targetingData = new();
        private uint _targetingModeSelectedObject = OBJECT_INVALID;
        private Vector3 _targetingModeSelectedPosition = Vector3.Zero;
        private uint _lastPlayerToSelectTarget = OBJECT_INVALID;

        private class TargetingData
        {
            public ObjectType ValidObjectTypes { get; set; } = ObjectType.All;
            public MouseCursorType MouseCursorId { get; set; } = MouseCursorType.Magic;
            public MouseCursorType BadTargetCursor { get; set; } = MouseCursorType.NoMagic;
            public SpellType Spell { get; set; } = SpellType.Invalid;
            public int Shape { get; set; } = 0;
            public float SizeX { get; set; } = 0.0f;
            public float SizeY { get; set; } = 0.0f;
            public int Flags { get; set; } = 0;
        }

        public void EnterTargetingMode(uint oPC, ObjectType nValidObjectTypes = ObjectType.All, MouseCursorType nMouseCursorId = MouseCursorType.Magic, MouseCursorType nBadTargetCursor = MouseCursorType.NoMagic) 
        {
            var data = GetOrCreateTargetingData(oPC);
            data.ValidObjectTypes = nValidObjectTypes;
            data.MouseCursorId = nMouseCursorId;
            data.BadTargetCursor = nBadTargetCursor;
        }

        public uint GetTargetingModeSelectedObject() => _targetingModeSelectedObject;

        public Vector3 GetTargetingModeSelectedPosition() => _targetingModeSelectedPosition;

        public uint GetLastPlayerToSelectTarget() => _lastPlayerToSelectTarget;

        public void SetSpellTargetingData(uint oPlayer, SpellType nSpell, int nShape, float fSizeX, float fSizeY, int nFlags) 
        {
            var data = GetOrCreateTargetingData(oPlayer);
            data.Spell = nSpell;
            data.Shape = nShape;
            data.SizeX = fSizeX;
            data.SizeY = fSizeY;
            data.Flags = nFlags;
        }

        public void SetEnterTargetingModeData(uint oPlayer, ObjectType nValidObjectTypes, MouseCursorType nMouseCursorId, MouseCursorType nBadTargetCursor, SpellType nSpell, int nShape, float fSizeX, float fSizeY, int nFlags) 
        {
            var data = GetOrCreateTargetingData(oPlayer);
            data.ValidObjectTypes = nValidObjectTypes;
            data.MouseCursorId = nMouseCursorId;
            data.BadTargetCursor = nBadTargetCursor;
            data.Spell = nSpell;
            data.Shape = nShape;
            data.SizeX = fSizeX;
            data.SizeY = fSizeY;
            data.Flags = nFlags;
        }

        private TargetingData GetOrCreateTargetingData(uint oPlayer)
        {
            if (!_targetingData.ContainsKey(oPlayer))
                _targetingData[oPlayer] = new TargetingData();
            return _targetingData[oPlayer];
        }

        // Helper methods for testing
    }
}
