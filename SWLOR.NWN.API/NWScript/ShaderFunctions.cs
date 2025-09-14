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
        /// - nShader: SHADER_UNIFORM_*
        /// </summary>
        public static void SetShaderUniformFloat(uint oPlayer, ShaderUniformType nShader, float fValue)
        {
            global::NWN.Core.NWScript.SetShaderUniformFloat(oPlayer, (int)nShader, fValue);
        }

        /// <summary>
        /// Sets the global shader uniform for the player to the specified integer.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// - nShader: SHADER_UNIFORM_*
        /// </summary>
        public static void SetShaderUniformInt(uint oPlayer, ShaderUniformType nShader, int nValue)
        {
            global::NWN.Core.NWScript.SetShaderUniformInt(oPlayer, (int)nShader, nValue);
        }

        /// <summary>
        /// Sets the global shader uniform for the player to the specified vec4.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// - nShader: SHADER_UNIFORM_*
        /// </summary>
        public static void SetShaderUniformVec(uint oPlayer, ShaderUniformType nShader, float fX, float fY, float fZ, float fW)
        {
            global::NWN.Core.NWScript.SetShaderUniformVec(oPlayer, (int)nShader, fX, fY, fZ, fW);
        }
    }
}
