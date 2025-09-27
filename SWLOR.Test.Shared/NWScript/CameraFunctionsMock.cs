using System.Collections.Generic;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for camera
        private readonly Dictionary<uint, CameraData> _cameraData = new();

        private class CameraData
        {
            public float Direction { get; set; } = 0.0f;
            public float Distance { get; set; } = -1.0f;
            public float Pitch { get; set; } = -1.0f;
            public float Height { get; set; } = -1.0f;
            public CameraLimits Limits { get; set; } = new();
            public uint AttachedTarget { get; set; } = OBJECT_INVALID;
            public bool FindClearView { get; set; } = false;
            public int Flags { get; set; } = 0;
        }

        private class CameraLimits
        {
            public float Left { get; set; } = 0.0f;
            public float Right { get; set; } = 0.0f;
            public float Bottom { get; set; } = 0.0f;
            public float Top { get; set; } = 0.0f;
            public float Distance { get; set; } = 0.0f;
            public float Pitch { get; set; } = 0.0f;
        }

        public void SetCameraFacing(float fDirection, float fDistance = -1.0f, float fPitch = -1.0f, float fHeight = -1.0f) 
        {
            var data = GetOrCreateCameraData(OBJECT_SELF);
            data.Direction = fDirection;
            data.Distance = fDistance;
            data.Pitch = fPitch;
            data.Height = fHeight;
        }

        public void SetCameraLimits(float fLeft, float fRight, float fBottom, float fTop, float fDistance, float fPitch) 
        {
            var data = GetOrCreateCameraData(OBJECT_SELF);
            data.Limits.Left = fLeft;
            data.Limits.Right = fRight;
            data.Limits.Bottom = fBottom;
            data.Limits.Top = fTop;
            data.Limits.Distance = fDistance;
            data.Limits.Pitch = fPitch;
        }

        public void AttachCamera(uint oPlayer, uint oTarget, bool bFindClearView = false) 
        {
            var data = GetOrCreateCameraData(oPlayer);
            data.AttachedTarget = oTarget;
            data.FindClearView = bFindClearView;
        }

        public void SetCameraFlags(uint oPlayer, int nFlags = 0) 
        {
            var data = GetOrCreateCameraData(oPlayer);
            data.Flags = nFlags;
        }

        private CameraData GetOrCreateCameraData(uint oPlayer)
        {
            if (!_cameraData.ContainsKey(oPlayer))
                _cameraData[oPlayer] = new CameraData();
            return _cameraData[oPlayer];
        }

        // Camera methods
        public void LockCameraDirection(uint oPlayer, float fDirection) 
        { 
            // Mock implementation - no-op for testing
        }
        
        public void LockCameraDistance(uint oPlayer, float fDistance) 
        { 
            // Mock implementation - no-op for testing
        }
        
        public void LockCameraPitch(uint oPlayer, float fPitch) 
        { 
            // Mock implementation - no-op for testing
        }
        
        public void RestoreCameraFacing(uint oPlayer) 
        { 
            // Mock implementation - no-op for testing
        }
        
        public void StoreCameraFacing(uint oPlayer) 
        { 
            // Mock implementation - no-op for testing
        }

        // Helper methods for testing
    }
}
