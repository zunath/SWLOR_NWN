using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Sets the global shader uniform for the player to the specified float.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the shader uniform for</param>
        /// <param name="nShader">SHADER_UNIFORM_* constant</param>
        /// <param name="fValue">The float value to set</param>
        public static void SetShaderUniformFloat(uint oPlayer, ShaderUniformType nShader, float fValue)
        {
            global::NWN.Core.NWScript.SetShaderUniformFloat(oPlayer, (int)nShader, fValue);
        }

        /// <summary>
        /// Sets the global shader uniform for the player to the specified integer.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the shader uniform for</param>
        /// <param name="nShader">SHADER_UNIFORM_* constant</param>
        /// <param name="nValue">The integer value to set</param>
        public static void SetShaderUniformInt(uint oPlayer, ShaderUniformType nShader, int nValue)
        {
            global::NWN.Core.NWScript.SetShaderUniformInt(oPlayer, (int)nShader, nValue);
        }

        /// <summary>
        /// Sets the global shader uniform for the player to the specified vec4.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the shader uniform for</param>
        /// <param name="nShader">SHADER_UNIFORM_* constant</param>
        /// <param name="fX">The X component of the vec4</param>
        /// <param name="fY">The Y component of the vec4</param>
        /// <param name="fZ">The Z component of the vec4</param>
        /// <param name="fW">The W component of the vec4</param>
        public static void SetShaderUniformVec(uint oPlayer, ShaderUniformType nShader, float fX, float fY, float fZ, float fW)
        {
            global::NWN.Core.NWScript.SetShaderUniformVec(oPlayer, (int)nShader, fX, fY, fZ, fW);
        }
    }
}
