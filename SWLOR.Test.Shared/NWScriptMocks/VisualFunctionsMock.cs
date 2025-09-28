using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for visual effects
        private readonly Dictionary<uint, VisualData> _visualData = new();
        private readonly Dictionary<string, string> _textureOverrides = new();
        private readonly Dictionary<uint, Dictionary<string, Dictionary<string, object>>> _materialShaderUniforms = new();
        private readonly Dictionary<uint, HashSet<int>> _flashingIcons = new();

        private class VisualData
        {
            public int HiliteColor { get; set; } = -1;
            public MouseCursorType MouseCursor { get; set; } = MouseCursorType.Invalid;
            public Dictionary<ObjectVisualTransformType, float> Transforms { get; set; } = new();
            public float VisibleDistance { get; set; } = 45.0f;
        }

        public void FloatingTextStrRefOnCreature(int nStrRefToDisplay, uint oCreatureToFloatAbove, bool bBroadcastToFaction = false) 
        {
            // Mock implementation - no-op for testing
        }

        public void FloatingTextStringOnCreature(string sStringToDisplay, uint oCreatureToFloatAbove, bool bBroadcastToFaction = false) 
        {
            // Mock implementation - no-op for testing
        }

        public void PostString(uint PC, string Msg, int X = 0, int Y = 0, ScreenAnchorType anchor = ScreenAnchorType.TopLeft, int nLifeTime = 0, int nRGBA = -1) 
        {
            // Mock implementation - no-op for testing
        }

        public void SetObjectHiliteColor(uint oObject, int nColor = -1) 
        {
            var data = GetOrCreateVisualData(oObject);
            data.HiliteColor = nColor;
        }

        public void SetObjectMouseCursor(uint oObject, MouseCursorType nCursor = MouseCursorType.Invalid) 
        {
            var data = GetOrCreateVisualData(oObject);
            data.MouseCursor = nCursor;
        }

        public void SetTextureOverride(string OldName, string NewName = "", uint PC = OBJECT_INVALID) 
        {
            _textureOverrides[OldName] = NewName;
        }

        public float GetObjectVisualTransform(uint oObject, ObjectVisualTransformType nTransform) => 
            _visualData.GetValueOrDefault(oObject, new VisualData()).Transforms.GetValueOrDefault(nTransform, 0.0f);

        public float SetObjectVisualTransform(uint oObject, ObjectVisualTransformType nTransform, float fValue, ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Invalid) 
        {
            var data = GetOrCreateVisualData(oObject);
            data.Transforms[nTransform] = fValue;
            return fValue;
        }

        public void SetMaterialShaderUniformInt(uint oObject, string sMaterial, string sParam, int nValue) 
        {
            if (!_materialShaderUniforms.ContainsKey(oObject))
                _materialShaderUniforms[oObject] = new Dictionary<string, Dictionary<string, object>>();
            if (!_materialShaderUniforms[oObject].ContainsKey(sMaterial))
                _materialShaderUniforms[oObject][sMaterial] = new Dictionary<string, object>();
            _materialShaderUniforms[oObject][sMaterial][sParam] = nValue;
        }

        public void SetMaterialShaderUniformVec4(uint oObject, string sMaterial, string sParam, float fValue1, float fValue2, float fValue3, float fValue4) 
        {
            if (!_materialShaderUniforms.ContainsKey(oObject))
                _materialShaderUniforms[oObject] = new Dictionary<string, Dictionary<string, object>>();
            if (!_materialShaderUniforms[oObject].ContainsKey(sMaterial))
                _materialShaderUniforms[oObject][sMaterial] = new Dictionary<string, object>();
            _materialShaderUniforms[oObject][sMaterial][sParam] = new Vector4(fValue1, fValue2, fValue3, fValue4);
        }

        public void ResetMaterialShaderUniforms(uint oObject, string sMaterial = "", string sParam = "") 
        {
            if (_materialShaderUniforms.ContainsKey(oObject))
            {
                if (string.IsNullOrEmpty(sMaterial))
                    _materialShaderUniforms[oObject].Clear();
                else if (string.IsNullOrEmpty(sParam))
                    _materialShaderUniforms[oObject].Remove(sMaterial);
                else
                    _materialShaderUniforms[oObject].GetValueOrDefault(sMaterial, new Dictionary<string, object>()).Remove(sParam);
            }
        }

        public void SetEffectIconFlashing(uint oCreature, int nIconId, bool bFlashing = true) 
        {
            if (!_flashingIcons.ContainsKey(oCreature))
                _flashingIcons[oCreature] = new HashSet<int>();
            
            if (bFlashing)
                _flashingIcons[oCreature].Add(nIconId);
            else
                _flashingIcons[oCreature].Remove(nIconId);
        }

        public bool ClearObjectVisualTransform(uint oObject, ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Invalid) 
        {
            if (_visualData.ContainsKey(oObject))
            {
                _visualData[oObject].Transforms.Clear();
                return true;
            }
            return false;
        }

        public void SetObjectVisibleDistance(uint oObject, float fDistance = 45.0f) 
        {
            var data = GetOrCreateVisualData(oObject);
            data.VisibleDistance = fDistance;
        }

        public float GetObjectVisibleDistance(uint oObject) => 
            _visualData.GetValueOrDefault(oObject, new VisualData()).VisibleDistance;

        public void ReplaceObjectAnimation(uint oObject, string sOld, string sNew = "") 
        {
            // Mock implementation - no-op for testing
        }

        private VisualData GetOrCreateVisualData(uint oObject)
        {
            if (!_visualData.ContainsKey(oObject))
                _visualData[oObject] = new VisualData();
            return _visualData[oObject];
        }

        // Visual effect methods
        public void BlackScreen(uint oCreature) { }
        public void BootPC(uint oPlayer, string sReason = "") { }
        public void DayToNight(uint oPlayer, float fTransitionTime = 0.0f) { }
        public void FadeFromBlack(uint oCreature, float fSpeed = 1.0f) { }
        public void FadeToBlack(uint oCreature, float fSpeed = 1.0f) { }
        public void NightToDay(uint oPlayer, float fTransitionTime = 0.0f) { }
        public void StopFade(uint oCreature) { }
        public void ReplaceObjectTexture(uint oObject, int nTexture, int nTextureVariation = 0) { }

        // Helper methods for testing
    }
}
