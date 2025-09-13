using SWLOR.NWN.API.Core;

// ReSharper disable once CheckNamespace
namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Destroy oObject (irrevocably).
        ///   This will not work on modules and areas.
        /// </summary>
        public static void DestroyObject(uint oDestroy, float fDelay = 0.0f)
        {
            VM.StackPush(fDelay);
            VM.StackPush(oDestroy);
            VM.Call(241);

            ExecuteScript("object_destroyed", oDestroy);
        }
    }
}