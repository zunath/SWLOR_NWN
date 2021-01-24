using SWLOR.Game.Server.Core.NWNX.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Object = SWLOR.Game.Server.Core.NWNX.Object;

namespace SWLOR.Game.Server.Service
{
    public static class Variable
    {
        /// <summary>
        /// Copies all local variables from source to target.
        /// </summary>
        /// <param name="source">The source to read variables from.</param>
        /// <param name="target">The target to apply variables to.</param>
        public static void CopyAll(uint source, uint target)
        {
            var variableCount = Object.GetLocalVariableCount(source);
            for (var variableIndex = 0; variableIndex < variableCount - 1; variableIndex++)
            {
                var stCurVar = Object.GetLocalVariable(source, variableIndex);

                switch (stCurVar.Type)
                {
                    case LocalVariableType.Int:
                        SetLocalInt(target, stCurVar.Key, GetLocalInt(source, stCurVar.Key));
                        break;
                    case LocalVariableType.Float:
                        SetLocalFloat(target, stCurVar.Key, GetLocalFloat(source, stCurVar.Key));
                        break;
                    case LocalVariableType.String:
                        SetLocalString(target, stCurVar.Key, GetLocalString(source, stCurVar.Key));
                        break;
                    case LocalVariableType.Object:
                        SetLocalObject(target, stCurVar.Key, GetLocalObject(source, stCurVar.Key));
                        break;
                    case LocalVariableType.Location:
                        SetLocalLocation(target, stCurVar.Key, GetLocalLocation(source, stCurVar.Key));
                        break;
                }
            }
        }
    }
}
