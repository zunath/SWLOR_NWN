using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.Service;

namespace SWLOR.NWN.API.NWNX
{
    public class ProfilerPluginService : IProfilerPluginService
    {
        /// <inheritdoc/>
        public void PushPerfScope(
            string name, 
            string tag0_tag = "", 
            string tag0_value = "",
            string tag1_tag = "",
            string tag1_value = "",
            string tag2_tag = "",
            string tag2_value = "")
        {
            // The core plugin only supports one tag pair, so we'll use the first non-empty tag pair
            string primaryTag = "";
            string primaryValue = "";
            
            if (!string.IsNullOrWhiteSpace(tag0_value) && !string.IsNullOrWhiteSpace(tag0_tag))
            {
                primaryTag = tag0_tag;
                primaryValue = tag0_value;
            }
            else if (!string.IsNullOrWhiteSpace(tag1_value) && !string.IsNullOrWhiteSpace(tag1_tag))
            {
                primaryTag = tag1_tag;
                primaryValue = tag1_value;
            }
            else if (!string.IsNullOrWhiteSpace(tag2_value) && !string.IsNullOrWhiteSpace(tag2_tag))
            {
                primaryTag = tag2_tag;
                primaryValue = tag2_value;
            }
            
            global::NWN.Core.NWNX.ProfilerPlugin.PushPerfScope(name, primaryTag, primaryValue);
        }

        /// <inheritdoc/>
        public void PushPerfScope(uint target, string scriptName)
        {
            var internalObjectType = GetIsObjectValid(target)
                ? ObjectPlugin.GetInternalObjectType(target) 
                : InternalObjectType.Invalid;
            var objectTypeName = internalObjectType == InternalObjectType.Invalid 
                ? "(unknown)" 
                : internalObjectType.ToString();
            string areaResref;

            if (internalObjectType == InternalObjectType.Module)
                areaResref = "--MODULE--";
            else if (internalObjectType == InternalObjectType.Invalid)
                areaResref = "(unknown)";
            else 
                areaResref = GetResRef(GetArea(target));

            if (string.IsNullOrWhiteSpace(areaResref))
                areaResref = "(unknown)";

            PushPerfScope("RunScript", 
                "Script", scriptName,
                "Area", areaResref,
                "ObjectType", objectTypeName);
        }

        /// <inheritdoc/>
        public void PopPerfScope()
        {
            global::NWN.Core.NWNX.ProfilerPlugin.PopPerfScope();
        }
    }
}
