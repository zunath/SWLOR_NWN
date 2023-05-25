using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
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
            VM.StackPush(fValue);
            VM.StackPush((int)nShader);
            VM.StackPush(oPlayer);
            VM.Call(1038);
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
            VM.StackPush(nValue);
            VM.StackPush((int)nShader);
            VM.StackPush(oPlayer);
            VM.Call(1039);
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
            VM.StackPush(fW);
            VM.StackPush(fZ);
            VM.StackPush(fY);
            VM.StackPush(fX);
            VM.StackPush((int)nShader);
            VM.StackPush(oPlayer);
            VM.Call(1040);
        }
    }
}
