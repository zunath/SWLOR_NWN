using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for shader uniforms
        private readonly Dictionary<uint, Dictionary<ShaderUniformType, ShaderUniformData>> _shaderUniforms = new();

        private class ShaderUniformData
        {
            public float FloatValue { get; set; } = 0.0f;
            public int IntValue { get; set; } = 0;
            public float X { get; set; } = 0.0f;
            public float Y { get; set; } = 0.0f;
            public float Z { get; set; } = 0.0f;
            public float W { get; set; } = 0.0f;
        }

        public void SetShaderUniformFloat(uint oPlayer, ShaderUniformType nShader, float fValue) 
        {
            var data = GetOrCreateShaderUniformData(oPlayer, nShader);
            data.FloatValue = fValue;
        }

        public void SetShaderUniformInt(uint oPlayer, ShaderUniformType nShader, int nValue) 
        {
            var data = GetOrCreateShaderUniformData(oPlayer, nShader);
            data.IntValue = nValue;
        }

        public void SetShaderUniformVec(uint oPlayer, ShaderUniformType nShader, float fX, float fY, float fZ, float fW) 
        {
            var data = GetOrCreateShaderUniformData(oPlayer, nShader);
            data.X = fX;
            data.Y = fY;
            data.Z = fZ;
            data.W = fW;
        }

        private ShaderUniformData GetOrCreateShaderUniformData(uint oPlayer, ShaderUniformType nShader)
        {
            if (!_shaderUniforms.ContainsKey(oPlayer))
                _shaderUniforms[oPlayer] = new Dictionary<ShaderUniformType, ShaderUniformData>();
            if (!_shaderUniforms[oPlayer].ContainsKey(nShader))
                _shaderUniforms[oPlayer][nShader] = new ShaderUniformData();
            return _shaderUniforms[oPlayer][nShader];
        }

        // Helper methods for testing
    }
}
