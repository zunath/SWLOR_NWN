using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    public abstract class LegacyMigrationBase
    {
        protected void CleanItemName(uint item)
        {
            SetName(item, string.Empty);
        }

        protected void WipeItemProperties(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                RemoveItemProperty(item, ip);
            }
        }

        protected void WipeDescription(uint item)
        {
            SetDescription(item, string.Empty);
            SetDescription(item, string.Empty, false);
        }

        protected void WipeVariables(uint item)
        {
            var variableCount = ObjectPlugin.GetLocalVariableCount(item);
            for (var variableIndex = 0; variableIndex < variableCount - 1; variableIndex++)
            {
                var stCurVar = ObjectPlugin.GetLocalVariable(item, variableIndex);

                switch (stCurVar.Type)
                {
                    case LocalVariableType.Int:
                        DeleteLocalInt(item, stCurVar.Key);
                        break;
                    case LocalVariableType.Float:
                        DeleteLocalFloat(item, stCurVar.Key);
                        break;
                    case LocalVariableType.String:
                        DeleteLocalString(item, stCurVar.Key);
                        break;
                    case LocalVariableType.Object:
                        DeleteLocalObject(item, stCurVar.Key);
                        break;
                    case LocalVariableType.Location:
                        DeleteLocalLocation(item, stCurVar.Key);
                        break;
                }
            }
        }

    }
}
